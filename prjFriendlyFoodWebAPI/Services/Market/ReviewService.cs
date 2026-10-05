using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public interface IReviewService
    {
        // 為一張已完成訂單裡的商品送出評價（可一次多項）
        Task<OrderActionResult> SubmitAsync(int userId, long orderId, List<ReviewItemDto> items);
    }

    public class ReviewService : IReviewService
    {
        // 資料庫欄位是 nvarchar(max)，長度由後端把關（前端 maxlength 要一致）
        public const int MaxCommentLength = 300;

        private readonly FriendlyFoodDbContext _context;

        public ReviewService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<OrderActionResult> SubmitAsync(int userId, long orderId, List<ReviewItemDto> items)
        {
            // ── 1. 檢查送來的內容本身 ──
            if (items == null || items.Count == 0)
                return new(OrderActionOutcome.NotAllowed, "請至少為一項商品評分");

            if (items.Select(i => i.OrderDetailId).Distinct().Count() != items.Count)
                return new(OrderActionOutcome.NotAllowed, "同一項商品不能重複評價");

            foreach (var item in items)
            {
                if (item.Rating < 1 || item.Rating > 5)
                    return new(OrderActionOutcome.NotAllowed, "評分必須是 1 到 5 顆星");

                item.Comment = item.Comment?.Trim();
                if (item.Comment?.Length > MaxCommentLength)
                    return new(OrderActionOutcome.NotAllowed, $"評論內容最多 {MaxCommentLength} 字");
            }

            // ── 2. 檢查訂單：自己的、已完成 ──
            var order = await _context.TMarketOrders
                .AsNoTracking()
                .Where(o => o.FOrderId == orderId && o.FUserId == userId)
                .Select(o => new
                {
                    o.FOrderStatus,
                    Details = o.TMarketOrderDetails
                        .Select(d => new { d.FOrderDetailsId, d.FProductId })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return new(OrderActionOutcome.NotFound, "找不到訂單");

            if (order.FOrderStatus != MarketStatus.Order.Completed)
                return new(OrderActionOutcome.NotAllowed, "訂單完成後才能評價");

            // ── 3. 每一項都必須是這張訂單的明細，而且還沒評價過 ──
            var detailMap = order.Details.ToDictionary(d => d.FOrderDetailsId, d => d.FProductId);
            if (items.Any(i => !detailMap.ContainsKey(i.OrderDetailId)))
                return new(OrderActionOutcome.NotAllowed, "評價的商品不屬於這張訂單");

            var detailIds = items.Select(i => i.OrderDetailId).ToList();
            var alreadyReviewed = await _context.TMarketProductReviews
                .AnyAsync(r => detailIds.Contains(r.FOrderDetailsId));
            if (alreadyReviewed)
                return new(OrderActionOutcome.NotAllowed, "部分商品已經評價過了，請重新整理");

            // ── 4. 寫入 ──
            var now = DateTime.Now;
            foreach (var item in items)
            {
                _context.TMarketProductReviews.Add(new TMarketProductReview
                {
                    FOrderDetailsId = item.OrderDetailId,
                    FProductId = detailMap[item.OrderDetailId],   // 商品以明細為準，不相信前端
                    FUserId = userId,
                    FRating = item.Rating,
                    FComment = item.Comment ?? string.Empty,
                    FCreatedDate = now
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // 兩個分頁同時送出：上面的檢查都通過了，但 fOrderDetailsID 的唯一約束擋下第二次寫入
                return new(OrderActionOutcome.NotAllowed, "部分商品已經評價過了，請重新整理");
            }

            return new(OrderActionOutcome.Success, $"已送出 {items.Count} 則評價，感謝您的分享！");
        }
    }
}
