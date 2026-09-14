using Microsoft.AspNetCore.Http;
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
        private readonly FriendlyFoodDbContext _context;//宣告一個唯讀的Context
        public ShoppingCartController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        //POST api/ShoppingCart/add
        [HttpPost("add")]//這個是在明確告訴當Angular使用app且用Post打過來的時候要執行以下的程式
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            //TODO:之後換成JWT取得真實userId
            int userId = 1;


            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == dto.ProductId
                                    && p.FProductStatus==(byte)1);

            if (product == null)
                return NotFound(new { message = "商品不存在或已下架" });

            if (product.FStock <= 0)
                return BadRequest(new { message = "商品已售完" });

            var existing = await _context.TMarketShoppingCarts
                    .FirstOrDefaultAsync(c => c.FUserId == userId
                                        && c.FProductId == dto.ProductId);

            if(existing!=null)
            {
                int newQty = existing.FQuantity + dto.Quantity;
                if (newQty > product.FStock)
                    return BadRequest(new { message = $"數量超過庫存上限(目前庫存:{product.FStock})" });
            }
            else
            {
                if(dto.Quantity>product.FStock)
                    return BadRequest(new {message=$"數量超過庫存上限(目前庫存:{ product.FStock})"});

                var cartItem = new TMarketShoppingCart
                {
                    FUserId = userId,
                    FSellerId=product.FSellerId,
                    FProductId=dto.ProductId,
                    FQuantity=dto.Quantity
                };
                _context.TMarketShoppingCarts.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "已加入購物車" });
        }
    }
}
