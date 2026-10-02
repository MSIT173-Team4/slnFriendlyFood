using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using System.Linq.Expressions;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public interface IOrderQueryService
    {
        // userId 有值：只查該會員自己的批次（完成頁 API 用）
        // userId 為 null：不檢查擁有者（系統內部寄信用，呼叫端須自行確保安全）
        Task<OrderCompleteDto?> GetOrderCompleteAsync(long batchId, int? userId);
        Task<MyOrderListResultDto> GetMyOrdersAsync(int userId, string tab, string range, string? keyword, int page);
    }

    public class OrderQueryService : IOrderQueryService
    {
        private readonly FriendlyFoodDbContext _context;

        public OrderQueryService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<OrderCompleteDto?> GetOrderCompleteAsync(long batchId, int? userId)
        {
            var query = _context.TMarketCheckoutBatches
                .AsNoTracking()
                .Include(b => b.TMarketOrders)
                    .ThenInclude(o => o.FSeller)
                .Include(b => b.TMarketOrders)
                    .ThenInclude(o => o.TMarketOrderDetails)
                        .ThenInclude(d => d.FProduct)
                            .ThenInclude(p => p.TMarketProductImages)
                .Where(b => b.FBatchId == batchId);

            if (userId.HasValue)
                query = query.Where(b => b.FUserId == userId.Value);

            var batch = await query.FirstOrDefaultAsync();
            if (batch == null) return null;

            var groups = batch.TMarketOrders
                .OrderBy(o => o.FOrderId)
                .Select(order => new OrderGroupDto
                {
                    OrderId = order.FOrderId,
                    OrderNo = order.FOrderNo,
                    SellerName = order.FSeller?.FSellerName ?? $"賣家 {order.FSellerId}",

                    RecipientName = order.FRecipientName ?? string.Empty,
                    RecipientPhone = order.FRecipientPhone ?? string.Empty,
                    ShippingAddress = order.FShippingAddress ?? string.Empty,
                    ShippingMethod = order.FShippingMethod ?? string.Empty,

                    PaymentStatus = order.FPaymentStatus,
                    SubTotal = order.TMarketOrderDetails.Sum(d => d.FUnitPrice * d.FQuantity),
                    ProductDiscount = order.FProductDiscount,
                    ShippingFee = order.FShippingFee,
                    ShippingDiscount = order.FShippingDiscount,
                    OrderAmount = order.FTotalAmount,

                    Items = order.TMarketOrderDetails.Select(d => new OrderItemDto
                    {
                        ProductId = d.FProductId,
                        ProductName = d.FProduct?.FProductName ?? string.Empty,
                        ImageUrl = d.FProduct?.TMarketProductImages
                                    .OrderBy(img => img.FSortOrder)
                                    .FirstOrDefault()?.FImageUrl,
                        Quantity = d.FQuantity,
                        UnitPrice = d.FUnitPrice,
                        LineTotal = d.FUnitPrice * d.FQuantity
                    }).ToList()
                })
                .ToList();

            return new OrderCompleteDto
            {
                BatchId = batch.FBatchId,
                BatchNo = batch.FBatchNo,
                PaidAt = batch.FPaidAt ?? batch.FCreatedDate,
                PaymentMethod = batch.FPaymentMethod ?? "信用卡",
                PaymentStatus = batch.FPaymentStatus,

                SubTotal = groups.Sum(g => g.SubTotal),
                ProductDiscount = groups.Sum(g => g.ProductDiscount),
                ShippingFee = groups.Sum(g => g.ShippingFee),
                ShippingDiscount = groups.Sum(g => g.ShippingDiscount),
                TotalAmount = batch.FTotalAmount,

                OrderGroups = groups
            };
        }

        // ── 我的訂單 ───────────────────────────────────────────

        private const int PageSize = 10;

        // 訂單狀態值（對照資料表欄位說明）
        private const int OrderCompleted = 2;
        private const int OrderCancelled = 3;
        private const int Unpaid = 0;
        private const int Paid = 1;
        private const int ShipPending = 0;
        private const int Shipping = 1;
        private const int Delivered = 2;

        public async Task<MyOrderListResultDto> GetMyOrdersAsync(
            int userId, string tab, string range, string? keyword, int page)
        {
            if (page < 1) page = 1;

            // 基礎條件：自己的訂單 + 時間範圍（分頁數量也套用時間範圍）
            var baseQuery = _context.TMarketOrders
                .AsNoTracking()
                .Where(o => o.FUserId == userId);

            var since = GetSinceDate(range);
            if (since.HasValue)
                baseQuery = baseQuery.Where(o => o.FOrderDate >= since.Value);

            // 各分頁數量（不受搜尋關鍵字影響）
            var counts = new MyOrderCountsDto
            {
                PendingPayment = await baseQuery.CountAsync(TabFilter("pending-payment")),
                PendingShip = await baseQuery.CountAsync(TabFilter("pending-ship")),
                ToReceive = await baseQuery.CountAsync(TabFilter("to-receive")),
                Completed = await baseQuery.CountAsync(TabFilter("completed")),
                ToReview = await baseQuery.CountAsync(TabFilter("to-review")),
                Cancelled = await baseQuery.CountAsync(TabFilter("cancelled")),
                All = await baseQuery.CountAsync()
            };

            // 目前分頁 + 搜尋
            var query = baseQuery.Where(TabFilter(tab));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(o =>
                    o.FOrderNo.Contains(k) ||
                    o.FSeller.FSellerName.Contains(k) ||
                    o.TMarketOrderDetails.Any(d => d.FProduct.FProductName.Contains(k)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.FOrderDate)
                .ThenByDescending(o => o.FOrderId)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(o => new MyOrderDto
                {
                    OrderId = o.FOrderId,
                    OrderNo = o.FOrderNo,
                    BatchId = o.FBatchId,
                    OrderDate = o.FOrderDate,
                    SellerId = o.FSellerId,
                    SellerName = o.FSeller.FSellerName,

                    OrderStatus = o.FOrderStatus,
                    PaymentStatus = o.FPaymentStatus,
                    ShippingStatus = o.FShippingStatus,

                    SubTotal = o.TMarketOrderDetails.Sum(d => d.FUnitPrice * d.FQuantity),
                    ProductDiscount = o.FProductDiscount,
                    ShippingFee = o.FShippingFee,
                    ShippingDiscount = o.FShippingDiscount,
                    OrderAmount = o.FTotalAmount,

                    CanReview = o.FOrderStatus == OrderCompleted &&
                                o.TMarketOrderDetails.Any(d =>
                                    !_context.TMarketProductReviews.Any(r => r.FOrderDetailsId == d.FOrderDetailsId)),

                    Items = o.TMarketOrderDetails.Select(d => new OrderItemDto
                    {
                        ProductId = d.FProductId,
                        ProductName = d.FProduct.FProductName,
                        ImageUrl = d.FProduct.TMarketProductImages
                                    .OrderBy(img => img.FSortOrder)
                                    .Select(img => img.FImageUrl)
                                    .FirstOrDefault(),
                        Quantity = d.FQuantity,
                        UnitPrice = d.FUnitPrice,
                        LineTotal = d.FUnitPrice * d.FQuantity
                    }).ToList()
                })
                .ToListAsync();

            // 狀態代碼在記憶體中判斷（規則與 TabFilter 一致）
            foreach (var item in items)
                item.StatusKey = GetStatusKey(item.OrderStatus, item.PaymentStatus, item.ShippingStatus);

            return new MyOrderListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                Counts = counts
            };
        }

        // 時間範圍：6m 近半年（預設）／1y 一年內／all 全部
        private static DateTime? GetSinceDate(string range) => range switch
        {
            "1y" => DateTime.Now.AddYears(-1),
            "all" => null,
            _ => DateTime.Now.AddMonths(-6)
        };

        // 各分頁的篩選條件；寫成 Expression，EF 才能翻譯成 SQL 的 WHERE
        private Expression<Func<TMarketOrder, bool>> TabFilter(string tab) => tab switch
        {
            "pending-payment" => o => o.FOrderStatus != OrderCancelled
                                   && o.FPaymentStatus == Unpaid,

            "pending-ship" => o => o.FOrderStatus != OrderCancelled
                                && o.FPaymentStatus == Paid
                                && o.FShippingStatus == ShipPending,

            "to-receive" => o => o.FOrderStatus != OrderCancelled
                              && o.FOrderStatus != OrderCompleted
                              && o.FPaymentStatus == Paid
                              && (o.FShippingStatus == Shipping || o.FShippingStatus == Delivered),

            "completed" => o => o.FOrderStatus == OrderCompleted,

            "to-review" => o => o.FOrderStatus == OrderCompleted
                             && o.TMarketOrderDetails.Any(d =>
                                    !_context.TMarketProductReviews.Any(r => r.FOrderDetailsId == d.FOrderDetailsId)),

            "cancelled" => o => o.FOrderStatus == OrderCancelled,

            _ => o => true   // all
        };

        // 單張訂單的狀態代碼（給前端顯示標籤用）
        private static string GetStatusKey(int orderStatus, int paymentStatus, int shippingStatus)
        {
            if (orderStatus == OrderCancelled) return "cancelled";
            if (orderStatus == OrderCompleted) return "completed";
            if (paymentStatus == Unpaid) return "pending-payment";
            if (shippingStatus == ShipPending) return "pending-ship";
            if (shippingStatus == Shipping) return "shipping";
            if (shippingStatus == Delivered) return "delivered";
            return "other";
        }
    }
}