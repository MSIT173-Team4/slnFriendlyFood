using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public interface IOrderQueryService
    {
        // userId 有值：只查該會員自己的批次（完成頁 API 用）
        // userId 為 null：不檢查擁有者（系統內部寄信用，呼叫端須自行確保安全）
        Task<OrderCompleteDto?> GetOrderCompleteAsync(long batchId, int? userId);
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
    }
}