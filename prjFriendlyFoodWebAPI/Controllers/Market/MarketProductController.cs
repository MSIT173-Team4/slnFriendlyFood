using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        // 注入 IWebHostEnvironment 才能拿到 wwwroot 的實際路徑
        public MarketProductController(FriendlyFoodDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 消費者端：固定只拿架上商品（status = 1）
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicProducts([FromQuery] int page = 1)
        {
            var products = await _context.TMarketProducts
                .Where(p => p.FProductStatus == 1)
                .Skip((page - 1) * 10)
                .Take(10)
                .Select(p => new DTOMarketPublicProductList
                {
                    ProductId = p.FProductId,
                    ProductName = p.FProductName,
                    Description = p.FDescription,
                    Stock = p.FStock,
                    Price = p.FPrice,
                    BrandOrOrigin = p.FBrandOrOrigin,
                    ManufacturingDate = p.FManufacturingDate,
                    ExpirationDate = p.FExpirationDate,
                    ProductStatus = p.FProductStatus,
                    ImageUrls = p.TMarketProductImages
                                 .OrderBy(img => img.FSortOrder)
                                 .Select(img => img.FImageUrl)
                                 .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        // 賣家後台：依 status 篩選，不傳就拿自己所有商品
        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerProducts([FromQuery] byte? status, [FromQuery] int page = 1)
        {
            // 之後換成從 Token 拿 sellerId
            var query = _context.TMarketProducts
                .Where(p => p.FSellerId == 3)
                .AsQueryable();

            // 0=審核中 / 1=架上商品 / 2=已售完 / 3=未上架 / 4=已違規
            if (status.HasValue)
                query = query.Where(p => p.FProductStatus == status.Value);

            var products = await query
                .Skip((page - 1) * 10)
                .Take(10)
                .Select(p => new DTOMarketPublicProductList
                {
                    ProductId = p.FProductId,
                    ProductName = p.FProductName,
                    Description = p.FDescription,
                    Stock = p.FStock,
                    Price = p.FPrice,
                    BrandOrOrigin = p.FBrandOrOrigin,
                    ManufacturingDate = p.FManufacturingDate,
                    ExpirationDate = p.FExpirationDate,
                    ProductStatus = p.FProductStatus,
                    ImageUrls = p.TMarketProductImages
                                 .OrderBy(img => img.FSortOrder)
                                 .Select(img => img.FImageUrl)
                                 .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
        public async Task<IActionResult> CreateProduct([FromForm] DTOMarketProductCreate dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 之後換成從 Token 拿 sellerId
            int sellerId = 7;

            var product = new TMarketProduct
            {
                FSellerId = sellerId,
                FProductNo = $"P{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}", // 先用這個暫時產生，之後再改規則
                FProductsCategoryNo = dto.ProductsCategoryNo,
                FProductName = dto.ProductName,
                FPrice = dto.Price,
                FStock = dto.Stock,
                FBrandOrOrigin = dto.BrandOrOrigin,
                FDescription = dto.Description,
                FManufacturingDate = dto.ManufacturingDate,
                FExpirationDate = dto.ExpirationDate,
                FProductStatus = 1,
                FReportCount = 0,
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1：存商品主表
                _context.TMarketProducts.Add(product);
                await _context.SaveChangesAsync();

                // Step 2：處理圖片檔案
                if (dto.Images != null && dto.Images.Any())
                {
                    // 確認 wwwroot/ProductImageUploads 資料夾存在
                    var uploadFolder = Path.Combine(_env.WebRootPath, "ProductImageUploads");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    var images = new List<TMarketProductImage>();
                    for (int i = 0; i < dto.Images.Count; i++)
                    {
                        var file = dto.Images[i];

                        // 用 GUID 當檔名，避免重複；保留原始副檔名
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadFolder, fileName);

                        // 把檔案存到 wwwroot/ProductImageUploads/
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        images.Add(new TMarketProductImage
                        {
                            FProductId = product.FProductId,
                            // DB 存相對路徑，之後換 Cloudinary 只改這裡
                            FImageUrl = $"/ProductImageUploads/{fileName}",
                            FSortOrder = (short)i
                        });
                    }

                    _context.TMarketProductImages.AddRange(images);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // 把 inner exception 也一起回傳，除錯完再改回去
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"新增商品失敗：{message}");
            }

            return Ok(new { product.FProductId });
        }
    }
}
