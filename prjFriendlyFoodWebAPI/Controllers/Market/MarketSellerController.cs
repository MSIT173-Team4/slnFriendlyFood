using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Market;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarketSellerController : ControllerBase
    {
        // 與前端商品列表的庫存警告門檻一致
        private const int LowStockThreshold = 10;

        private readonly FriendlyFoodDbContext _context;
        private readonly ISellerIdentityService _sellerIdentity;
        private readonly IOrderCancellationService _cancellation;

        public MarketSellerController(FriendlyFoodDbContext context, ISellerIdentityService sellerIdentity, IOrderCancellationService cancellation)
        {
            _context = context;
            _sellerIdentity = sellerIdentity;
            _cancellation = cancellation;
        }

        // GET /api/MarketSeller/me — 目前登入者是否為有效賣家，以及賣場與本人資訊
        [HttpGet("me")]
        public async Task<IActionResult> GetMySellerInfo()
        {
            int userId = User.GetUserId();
            var seller = await _sellerIdentity.GetActiveSellerAsync(userId);

            // 被動觸發：賣家看到的庫存與數量才是最新的
            await _cancellation.CancelExpiredBatchesAsync();

            if (seller == null)
                return Ok(new { isSeller = false });

            var owner = await _context.TUsers
                .AsNoTracking()
                .Where(u => u.FId == userId)
                .Select(u => new { u.FLastName, u.FFirstName})
                .FirstOrDefaultAsync();

            return Ok(new
            {
                isSeller = true,
                sellerId = seller.FId,
                sellerName = seller.FSellerName,                              // 賣場名稱（側欄）
                ownerName = $"{owner?.FLastName}{owner?.FFirstName}".Trim()   // 真實姓名（右上角）
            });
        }

        // GET /api/MarketSeller/summary — 側欄與商品分頁的數量
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var seller = await _sellerIdentity.GetActiveSellerAsync(User.GetUserId());
            if (seller == null)
                return StatusCode(403, new { message = "您尚未開通賣場，或賣場已停權" });

            int sellerId = seller.FId;

            // 一次 GROUP BY 算出每個商品狀態的數量
            var statusCounts = await _context.TMarketProducts
                .Where(p => p.FSellerId == sellerId)
                .GroupBy(p => p.FProductStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            int CountOf(byte status) => statusCounts.TryGetValue(status, out var c) ? c : 0;

            var lowStock = await _context.TMarketProducts
                .CountAsync(p => p.FSellerId == sellerId
                              && p.FProductStatus == 1
                              && p.FStock < LowStockThreshold);

            // 待出貨：已付款、尚未出貨的子訂單
            var pendingOrders = await _context.TMarketOrders
                .CountAsync(o => o.FSellerId == sellerId
                              && o.FPaymentStatus == 1
                              && o.FShippingStatus == 0);

            return Ok(new
            {
                total = statusCounts.Values.Sum(),
                onSale = CountOf(1),
                lowStock,
                soldOut = CountOf(2),
                reviewing = CountOf(0),
                violated = CountOf(4),
                unlisted = CountOf(3),
                pendingOrders
            });
        }
    }
}