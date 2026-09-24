using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Member;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // 收藏需要登入
    public class MarketFavoriteController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly TokenServices _ts;

        public MarketFavoriteController(FriendlyFoodDbContext context, TokenServices tokenServices)
        {
            _context = context;
            _ts = tokenServices;
        }

        // POST /api/MarketFavorite/toggle/{productId}
        // 加入或取消收藏（Toggle）
        [HttpPost("toggle/{productId}")]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            // 從 Token 取出 userId
            var tokenData = await _ts.GetTokenData(User);
            if (!int.TryParse(tokenData.UserId, out int userId))
                return Unauthorized();

            // 確認商品存在
            var product = await _context.TMarketProducts
                .AnyAsync(p => p.FProductId == productId);
            if (!product)
                return NotFound(new { message = "商品不存在" });

            // 查有沒有已收藏的記錄
            var existing = await _context.TMarketProductFavorites
                .FirstOrDefaultAsync(f => f.FUserId == userId && f.FProductId == productId);

            if (existing != null)
            {
                // 已收藏 → 取消
                _context.TMarketProductFavorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = false, message = "已取消收藏" });
            }
            else
            {
                // 未收藏 → 加入
                _context.TMarketProductFavorites.Add(new TMarketProductFavorite
                {
                    FUserId = userId,
                    FProductId = productId
                });
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = true, message = "已加入收藏" });
            }
        }

        // GET /api/MarketFavorite/check/{productId}
        // 確認這個商品有沒有被目前登入的使用者收藏
        [HttpGet("check/{productId}")]
        public async Task<IActionResult> CheckFavorite(int productId)
        {
            var tokenData = await _ts.GetTokenData(User);
            if (!int.TryParse(tokenData.UserId, out int userId))
                return Unauthorized();

            var isFavorite = await _context.TMarketProductFavorites
                .AnyAsync(f => f.FUserId == userId && f.FProductId == productId);

            return Ok(new { isFavorite });
        }
    }
}