using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Market;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShoppingCartController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly ICartService _cartService;

        public ShoppingCartController(FriendlyFoodDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        // GET /api/ShoppingCart — 取得購物車（依賣家分組）
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            int userId = User.GetUserId();

            var items = await _context.TMarketShoppingCarts
                .Where(c => c.FUserId == userId)
                .Select(c => new
                {
                    c.FCartItemId,
                    c.FProductId,
                    c.FQuantity,
                    c.FSellerId,
                    SellerName = c.FSeller.FSellerName,
                    ProductName = c.FProduct.FProductName,
                    Price = c.FProduct.FPrice,
                    Stock = c.FProduct.FStock,
                    ImageUrl = c.FProduct.TMarketProductImages
                        .OrderBy(img => img.FSortOrder)
                        .Select(img => img.FImageUrl)
                        .FirstOrDefault(),
                    IsFavorite = _context.TMarketProductFavorites
                        .Any(f => f.FProductId == c.FProductId && f.FUserId == userId)
                })
                .ToListAsync();

            // 依賣家分組
            var grouped = items
                .GroupBy(i => new { i.FSellerId, i.SellerName })
                .Select(g => new CartSellerGroupDto
                {
                    SellerId = g.Key.FSellerId,
                    SellerName = g.Key.SellerName,
                    Items = g.Select(i => new CartItemDto
                    {
                        CartItemId = i.FCartItemId,
                        ProductId = i.FProductId,
                        ProductName = i.ProductName,
                        ImageUrl = i.ImageUrl,
                        Price = i.Price,
                        Stock = i.Stock,
                        Quantity = i.FQuantity,
                        Subtotal = i.Price * i.FQuantity,
                        IsFavorite = i.IsFavorite
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }

        // GET /api/ShoppingCart/count — Header 購物車徽章用（購物車有幾項商品）
        [HttpGet("count")]
        public async Task<IActionResult> GetCartCount()
        {
            int userId = User.GetUserId();
            var count = await _context.TMarketShoppingCarts.CountAsync(c => c.FUserId == userId);
            return Ok(new { count });
        }

        // POST /api/ShoppingCart/add — 加入購物車（規則統一由 CartService 判斷）
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var result = await _cartService.AddAsync(User.GetUserId(), dto.ProductId, dto.Quantity);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            await _context.SaveChangesAsync();
            return Ok(new { message = "已加入購物車" });
        }

        // PUT /api/ShoppingCart/{cartItemId} — 修改數量
        [HttpPut("{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            int userId = User.GetUserId();

            if (dto.Quantity <= 0)
                return BadRequest(new { message = "數量必須大於 0" });

            var item = await _context.TMarketShoppingCarts
                .Include(c => c.FProduct)
                .FirstOrDefaultAsync(c => c.FCartItemId == cartItemId && c.FUserId == userId);

            if (item == null)
                return NotFound(new { message = "購物車項目不存在" });

            if (dto.Quantity > item.FProduct.FStock)
                return BadRequest(new { message = $"數量超過庫存上限(目前庫存:{item.FProduct.FStock})" });

            item.FQuantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "數量已更新",
                quantity = item.FQuantity,
                subtotal = item.FProduct.FPrice * item.FQuantity
            });
        }

        // DELETE /api/ShoppingCart/{cartItemId} — 刪除單筆
        [HttpDelete("{cartItemId}")]
        public async Task<IActionResult> DeleteCartItem(int cartItemId)
        {
            int userId = User.GetUserId();

            var item = await _context.TMarketShoppingCarts
                .FirstOrDefaultAsync(c => c.FCartItemId == cartItemId && c.FUserId == userId);

            if (item == null)
                return NotFound(new { message = "購物車項目不存在" });

            _context.TMarketShoppingCarts.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "已從購物車移除" });
        }

        // DELETE /api/ShoppingCart/all — 清空購物車
        [HttpDelete("all")]
        public async Task<IActionResult> ClearCart()
        {
            int userId = User.GetUserId();

            var items = await _context.TMarketShoppingCarts
                .Where(c => c.FUserId == userId)
                .ToListAsync();

            if (!items.Any())
                return Ok(new { message = "購物車已是空的" });

            _context.TMarketShoppingCarts.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "購物車已清空" });
        }
    }
}