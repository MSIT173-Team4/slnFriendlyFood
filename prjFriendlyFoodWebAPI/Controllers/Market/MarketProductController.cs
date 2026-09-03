using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketProductController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        public MarketProductController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            bool canConnect = _context.Database.CanConnect();
            return Ok($"DB 連線狀態：{canConnect}");

            //var products = await _context.TMarketProducts.ToListAsync();

            //return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] MarketProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 從 Token 取得賣家 ID（不信任前端傳的）
            //var sellerId = int.Parse(User.FindFirst("SellerId").Value);
            int sellerId = 3;

            // 手動 mapping：把 DTO 的值塞進 EF Entity
            var product = new TMarketProduct
            {
                FSellerId = sellerId,
                FProductsCategoryNo = dto.FProductsCategoryNo,
                FProductname = dto.FProductname,
                FPrice = dto.FPrice,
                FStock = dto.FStock,
                FBrandOrOrigin = dto.FBrandOrOrigin,
                FDescription = dto.FDescription,           // 補上
                FManufacturingDate = dto.FManufacturingDate, // 補上
                FExpirationDate = dto.FExpirationDate,       // 補上
                FProductStatus = 1,   // 直接上架（第二輪決策）
                FReportCount = 0,
                // fProductNo 如果需要自動產生，邏輯也在這邊做
            };

            _context.TMarketProducts.Add(product);
            await _context.SaveChangesAsync();

            // 如果有圖片、規格，接著寫入各自的子表...

            return Ok(new { product.FProductId });
        }
    }
}
