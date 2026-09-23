using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private const string ImageBaseUrl = "https://localhost:7164";

        public ShoppingCartController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // GET /api/ShoppingCart — 取得購物車（依賣家分組）
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            // TODO: 之後換成從 JWT 拿 userId
            int userId = 1;

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
                        .Select(img => ImageBaseUrl + img.FImageUrl)
                        .FirstOrDefault()
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
                        Subtotal = i.Price * i.FQuantity
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }

        // POST /api/ShoppingCart/add — 加入購物車（原本的，維持不動）
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            int userId = 1;

            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == dto.ProductId
                                     && p.FProductStatus == (byte)1);

            if (product == null)
                return NotFound(new { message = "商品不存在或已下架" });

            if (product.FStock <= 0)
                return BadRequest(new { message = "商品已售完" });

            var existing = await _context.TMarketShoppingCarts
                .FirstOrDefaultAsync(c => c.FUserId == userId
                                     && c.FProductId == dto.ProductId);

            if (existing != null)
            {
                int newQty = existing.FQuantity + dto.Quantity;
                if (newQty > product.FStock)
                    return BadRequest(new { message = $"數量超過庫存上限(目前庫存:{product.FStock})" });
                existing.FQuantity = newQty;
            }
            else
            {
                if (dto.Quantity > product.FStock)
                    return BadRequest(new { message = $"數量超過庫存上限(目前庫存:{product.FStock})" });

                _context.TMarketShoppingCarts.Add(new TMarketShoppingCart
                {
                    FUserId = userId,
                    FSellerId = product.FSellerId,
                    FProductId = dto.ProductId,
                    FQuantity = dto.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "已加入購物車" });
        }

        // PUT /api/ShoppingCart/{cartItemId} — 修改數量
        [HttpPut("{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            int userId = 1;

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
            int userId = 1;

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
            int userId = 1;

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