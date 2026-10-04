using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public enum OrderActionOutcome
    {
        Success,
        NotFound,
        NotAllowed
    }

    public record OrderActionResult(OrderActionOutcome Outcome, string Message);

    public interface IOrderFulfillmentService
    {
        // 賣家出貨：待出貨 → 運送中
        Task<OrderActionResult> ShipAsync(int sellerId, long orderId);

        // 買家確認收貨：運送中 / 已送達 → 已完成
        Task<OrderActionResult> ConfirmReceiptAsync(int userId, long orderId);
    }

    public class OrderFulfillmentService : IOrderFulfillmentService
    {
        private readonly FriendlyFoodDbContext _context;

        public OrderFulfillmentService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<OrderActionResult> ShipAsync(int sellerId, long orderId)
        {
            // 原子性更新：只有「自己賣場、已付款、未取消、未完成、待出貨」的訂單能出貨
            var affected = await _context.TMarketOrders
                .Where(o => o.FOrderId == orderId
                         && o.FSellerId == sellerId
                         && o.FPaymentStatus == MarketStatus.Payment.Paid
                         && o.FOrderStatus != MarketStatus.Order.Cancelled
                         && o.FOrderStatus != MarketStatus.Order.Completed
                         && o.FShippingStatus == MarketStatus.Shipping.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.FShippingStatus, MarketStatus.Shipping.InTransit)
                    .SetProperty(o => o.FIsShippingConfirmed, true));

            if (affected == 1)
                return new(OrderActionOutcome.Success, "已出貨");

            // 沒更新到：分辨是「不是自己的訂單」還是「狀態不允許」
            var exists = await _context.TMarketOrders
                .AnyAsync(o => o.FOrderId == orderId && o.FSellerId == sellerId);

            return exists
                ? new(OrderActionOutcome.NotAllowed, "此訂單目前無法出貨（可能已出貨或尚未付款）")
                : new(OrderActionOutcome.NotFound, "找不到訂單");
        }

        public async Task<OrderActionResult> ConfirmReceiptAsync(int userId, long orderId)
        {
            // 原子性更新：只有「自己的、已付款、未取消、未完成、運送中或已送達」的訂單能確認收貨
            var affected = await _context.TMarketOrders
                .Where(o => o.FOrderId == orderId
                         && o.FUserId == userId
                         && o.FPaymentStatus == MarketStatus.Payment.Paid
                         && o.FOrderStatus != MarketStatus.Order.Cancelled
                         && o.FOrderStatus != MarketStatus.Order.Completed
                         && (o.FShippingStatus == MarketStatus.Shipping.InTransit
                             || o.FShippingStatus == MarketStatus.Shipping.Delivered))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.FOrderStatus, MarketStatus.Order.Completed)
                    .SetProperty(o => o.FShippingStatus, MarketStatus.Shipping.Delivered));

            if (affected == 1)
                return new(OrderActionOutcome.Success, "已確認收貨，訂單完成");

            var exists = await _context.TMarketOrders
                .AnyAsync(o => o.FOrderId == orderId && o.FUserId == userId);

            return exists
                ? new(OrderActionOutcome.NotAllowed, "此訂單目前無法確認收貨（可能尚未出貨或已完成）")
                : new(OrderActionOutcome.NotFound, "找不到訂單");
        }
    }
}