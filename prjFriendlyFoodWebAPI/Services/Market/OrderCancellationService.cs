using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using Microsoft.Extensions.Options;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public enum CancelOutcome
    {
        Cancelled,    // 取消成功
        NotFound,     // 找不到（或不是自己的訂單）
        NotAllowed    // 狀態不允許取消
    }

    public record CancelBatchResult(CancelOutcome Outcome, string Message);

    public interface IOrderCancellationService
    {
        // 買家取消：取消「這張子訂單所屬的整個批次」
        Task<CancelBatchResult> CancelByBuyerAsync(int userId, long orderId);

        // 取消所有逾期未付款的批次，回傳取消了幾筆（被動觸發與背景排程共用）
        Task<int> CancelExpiredBatchesAsync();
    }

    public class OrderCancellationService : IOrderCancellationService
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly ILogger<OrderCancellationService> _logger;
        private readonly MarketOptions _options;

        public OrderCancellationService(FriendlyFoodDbContext context, ILogger<OrderCancellationService> logger, IOptions<MarketOptions> options)
        {
            _context = context;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<CancelBatchResult> CancelByBuyerAsync(int userId, long orderId)
        {
            var order = await _context.TMarketOrders
                .AsNoTracking()
                .Where(o => o.FOrderId == orderId && o.FUserId == userId)
                .Select(o => new { o.FBatchId, o.FOrderStatus, o.FPaymentStatus })
                .FirstOrDefaultAsync();

            if (order == null)
                return new(CancelOutcome.NotFound, "找不到訂單");

            if (order.FOrderStatus == MarketStatus.Order.Cancelled)
                return new(CancelOutcome.NotAllowed, "此訂單已取消");

            if (order.FPaymentStatus != MarketStatus.Payment.Unpaid)
                return new(CancelOutcome.NotAllowed, "已付款的訂單無法直接取消");

            var cancelled = await CancelBatchCoreAsync(order.FBatchId);
            return cancelled
                ? new(CancelOutcome.Cancelled, "訂單已取消")
                : new(CancelOutcome.NotAllowed, "訂單狀態已變更，請重新整理後再試");
        }

        public async Task<int> CancelExpiredBatchesAsync()
        {
            // 建立時間早於這個時間點的未付款批次，就是逾期了
            var cutoff = DateTime.Now.AddMinutes(-_options.PaymentTimeoutMinutes);

            var expiredIds = await _context.TMarketCheckoutBatches
                .AsNoTracking()
                .Where(b => b.FPaymentStatus == MarketStatus.BatchPayment.Unpaid && b.FCreatedDate < cutoff)
                .OrderBy(b => b.FBatchId)
                .Select(b => b.FBatchId)
                .Take(100)   // 一次最多處理 100 筆，避免累積太多時單次執行過久
                .ToListAsync();

            int cancelledCount = 0;
            foreach (var batchId in expiredIds)
            {
                try
                {
                    if (await CancelBatchCoreAsync(batchId))
                        cancelledCount++;
                }
                catch (Exception ex)
                {
                    // 單一批次失敗不影響其他批次，下次執行時會再處理
                    _logger.LogError(ex, "逾期取消失敗，BatchId={BatchId}", batchId);
                }
            }

            if (cancelledCount > 0)
                _logger.LogInformation("逾期自動取消 {Count} 筆結帳批次", cancelledCount);

            return cancelledCount;
        }

        // 取消一個「仍未付款」的批次：狀態、庫存、優惠券名額在同一個 transaction 裡一起處理
        // 買家取消與逾期自動取消（P-2）共用；回傳 false 代表批次已付款或已被取消
        private async Task<bool> CancelBatchCoreAsync(long batchId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            // 1. 搶下取消權：只有仍未付款的批次能改成「失敗」，同時處理時只有一方會成功
            var claimed = await _context.TMarketCheckoutBatches
                .Where(b => b.FBatchId == batchId && b.FPaymentStatus == MarketStatus.BatchPayment.Unpaid)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.FPaymentStatus, MarketStatus.BatchPayment.Failed));

            if (claimed == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            // 2. 批次底下的子訂單全部改為已取消
            await _context.TMarketOrders
                .Where(o => o.FBatchId == batchId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.FOrderStatus, MarketStatus.Order.Cancelled));

            // 3. 還原庫存（同一商品合併數量）
            var lines = await _context.TMarketOrderDetails
                .Where(d => d.FOrder.FBatchId == batchId)
                .GroupBy(d => d.FProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(d => d.FQuantity) })
                .ToListAsync();

            foreach (var line in lines)
            {
                await _context.TMarketProducts
                    .Where(p => p.FProductId == line.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.FStock, p => p.FStock + line.Quantity));
            }
            await ProductStockRules.RestoreOnSaleAsync(_context, lines.Select(l => l.ProductId));

            // 4. 還原優惠券名額：Distinct 讓分攤到多張子訂單的全站券只減一次
            var couponIds = await _context.TMarketOrderDiscounts
                .Where(od => od.FOrder.FBatchId == batchId)
                .Select(od => od.FCouponId)
                .Distinct()
                .ToListAsync();

            if (couponIds.Count > 0)
            {
                await _context.TMarketCoupons
                    .Where(c => couponIds.Contains(c.FCouponId) && c.FUsedCount > 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.FUsedCount, c => c.FUsedCount - 1));
            }

            await tx.CommitAsync();
            _logger.LogInformation("已取消結帳批次 {BatchId}", batchId);
            return true;
        }
    }
}