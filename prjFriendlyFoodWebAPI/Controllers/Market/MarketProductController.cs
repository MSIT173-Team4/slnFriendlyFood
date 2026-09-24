using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketProductController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly IWebHostEnvironment _env;

        //連線字串的變數，demo時要改成demo主機的位置
        private const string ImageBaseUrl = "https://localhost:7164";

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
                .Select(p => new MarketPublicProductListDto
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
             .Select(img => ImageBaseUrl + img.FImageUrl)
             .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
        public async Task<IActionResult> CreateProduct([FromForm] MarketProductCreateDto dto)
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
                FProductStatus = dto.ProductStatus,
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

        //消費者端搜尋商品
        // GET /api/MarketProduct/search?keyword=糯米&categoryNo=F01&minPrice=50&maxPrice=200&sortBy=price_asc&page=1
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] MarketProductSearchDto dto)
        {
            // 基礎條件：只拿架上商品
            var query = _context.TMarketProducts
                .Where(p => p.FProductStatus == 1)
                .AsQueryable();

            // 關鍵字篩選：商品名稱包含關鍵字
            if (!string.IsNullOrEmpty(dto.Keyword))
                query = query.Where(p => p.FProductName.Contains(dto.Keyword));

            // 分類篩選
            // 子分類精確篩選（點穀物與豆類、肉類等）
            if (!string.IsNullOrEmpty(dto.CategoryNo))
                query = query.Where(p => p.FProductsCategoryNo == dto.CategoryNo);

            // 頂層分類篩選（點全部商品，用 parentCategoryId 找底下所有子分類）
            if (dto.ParentCategoryId.HasValue)
            {
                var childNos = await _context.TMarketProductCategories
                    .Where(c => c.FParentCategoryId == dto.ParentCategoryId.Value)
                    .Select(c => c.FCategoryNo)
                    .ToListAsync();

                query = query.Where(p => childNos.Contains(p.FProductsCategoryNo));
            }

            // 最低價格
            if (dto.MinPrice.HasValue)
                query = query.Where(p => p.FPrice >= dto.MinPrice.Value);

            // 最高價格
            if (dto.MaxPrice.HasValue)
                query = query.Where(p => p.FPrice <= dto.MaxPrice.Value);

            // 排序
            query = dto.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.FPrice),
                "price_desc" => query.OrderByDescending(p => p.FPrice),
                "newest" => query.OrderByDescending(p => p.FProductId),
                _ => query.OrderByDescending(p => p.FProductId) // 預設最新
            };

            // 分頁 + mapping 到 DTO
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((dto.Page - 1) * 10)
                .Take(10)
                .Select(p => new MarketPublicProductListDto
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
                                 .Select(img => ImageBaseUrl + img.FImageUrl)
                                 .ToList()
                })
                .ToListAsync();

            return Ok(new MarketProductPagedResultDto
            {
                Items = items,
                TotalCount = totalCount
            });
        }

        // GET /api/MarketProduct/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var product = await _context.TMarketProducts
                .Where(p => p.FProductId == id && p.FProductStatus == 1)
                .Select(p => new MarketProductDetailDto
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
                        .Select(img => ImageBaseUrl + img.FImageUrl)
                        .ToList(),

                    // 評論統計：從 tMarketProductReview 計算
                    AverageRating = p.TMarketProductReviews.Any()
                        ? Math.Round(p.TMarketProductReviews.Average(r => (double)r.FRating), 1)
                        : 0,
                    ReviewCount = p.TMarketProductReviews.Count(),

                    // 賣家資訊：JOIN tSeller
                    SellerId = p.FSellerId,
                    SellerName = p.FSeller.FSellerName,
                    SellerDescription = p.FSeller.FDescription,
                    SellerProductCount = _context.TMarketProducts
                        .Count(sp => sp.FSellerId == p.FSellerId && sp.FProductStatus == 1)
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "商品不存在" });

            return Ok(product);
        }

        // GET /api/MarketProduct/{id}/reviews?page=1&pageSize=3
        [HttpGet("{id:int}/reviews")]
        public async Task<IActionResult> GetProductReviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 3)
        {
            var query = _context.TMarketProductReviews
                .Where(r => r.FProductId == id)
                .OrderByDescending(r => r.FCreatedDate);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new MarketReviewDto
                {
                    ReviewId = r.FReviewId,
                    // 遮蔽名稱：取第一個字 + ＊ + 最後一個字
                    // 例如「王小芬」→「王＊芬」，「王芬」→「王＊」
                    ReviewerName = r.FUser.FUsername.Length >= 2
                        ? r.FUser.FUsername.Substring(0, 1) + "＊" + r.FUser.FUsername.Substring(r.FUser.FUsername.Length - 1)
                        : r.FUser.FUsername.Substring(0, 1) + "＊",
                    Rating = r.FRating,
                    Comment = r.FComment,
                    CreatedDate = r.FCreatedDate
                })
                .ToListAsync();

            return Ok(new MarketReviewPagedDto
            {
                Items = items,
                TotalCount = totalCount
            });
        }

        // GET /api/MarketProduct/{id}/recipes
        [HttpGet("{id:int}/recipes")]
        public async Task<IActionResult> GetRelatedRecipes(int id)
        {
            var product = await _context.TMarketProducts
                .Where(p => p.FProductId == id)
                .Select(p => new { p.FIngredientId })
                .FirstOrDefaultAsync();

            if (product == null || product.FIngredientId == null)
                return Ok(new List<MarketRelatedRecipeDto>());

            // 用 Join 取代導覽屬性
            var recipes = await _context.TRecipeIngredients
                .Where(ri => ri.FIngredientId == product.FIngredientId)
                .Join(
                    _context.TRecipes,
                    ri => ri.FRecipeId,
                    r => r.FRecipeId,
                    (ri, r) => new MarketRelatedRecipeDto
                    {
                        RecipeId = r.FRecipeId,
                        RecipeName = r.FTitle,
                        ImageUrl = r.FCoverImageUrl,
                        CookingTime = r.FCookingMinutes
                    }
                )
                .Take(4)
                .ToListAsync();

            return Ok(recipes);
        }

        // 賣家後台商品列表（含近30天銷量）
        [HttpGet("sellcenter")]
        public async Task<IActionResult> GetSellerProducts(
            [FromQuery] byte? status,
            [FromQuery] bool lowStock = false,
            [FromQuery] int page = 1,
            [FromQuery] string? keyword = null)
        {
            // 之後換成從 Token 拿 sellerId
            var query = _context.TMarketProducts
                .Where(p => p.FSellerId == 7)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.FProductStatus == status.Value);

            if (lowStock)
                query = query.Where(p => p.FStock < 10);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p =>
                    p.FProductName.Contains(keyword) ||
                    p.FDescription.Contains(keyword) ||
                    p.FBrandOrOrigin.Contains(keyword));

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.FProductId)
                .Skip((page - 1) * 10)
                .Take(10)
                .Select(p => new MarketSellerProductListDto
                {
                    ProductId = p.FProductId,
                    ProductNo = p.FProductNo,
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
                        .Select(img => ImageBaseUrl + img.FImageUrl)
                        .ToList(),
                    SalesLast30Days = 0
                })
                .ToListAsync();

            // 一支 SQL 算完這頁所有商品的近30天銷量
            var since = DateTime.Now.AddDays(-30);
            var productIds = products.Select(p => p.ProductId).ToList();

            var salesMap = await _context.TMarketOrderDetails
                .Where(od =>
                    productIds.Contains(od.FProductId) &&
                    od.FOrder.FOrderDate >= since)
                .GroupBy(od => od.FProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(od => od.FQuantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            foreach (var p in products)
                p.SalesLast30Days = salesMap.TryGetValue(p.ProductId, out var qty) ? qty : 0;

            return Ok(new { items = products, totalCount });
        }

        // 上下架靜默切換
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(
            int id, [FromBody] UpdateProductStatusDto dto)
        {
            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == 7); // 之後改 Token

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            product.FProductStatus = (byte)dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "狀態更新成功" });
        }

        // 賣家取得單一商品（含所有狀態、含圖片 id）
        [HttpGet("seller/{id:int}")]
        public async Task<IActionResult> GetSellerProductDetail(int id)
        {
            var product = await _context.TMarketProducts
                .Where(p => p.FProductId == id && p.FSellerId == 7) // 之後換 Token
                .Select(p => new MarketSellerProductDetailDto
                {
                    ProductId = p.FProductId,
                    ProductNo = p.FProductNo,
                    ProductName = p.FProductName,
                    Description = p.FDescription,
                    Stock = p.FStock,
                    Price = p.FPrice,
                    BrandOrOrigin = p.FBrandOrOrigin,
                    ManufacturingDate = p.FManufacturingDate,
                    ExpirationDate = p.FExpirationDate,
                    ProductStatus = p.FProductStatus,
                    ProductsCategoryNo = p.FProductsCategoryNo,
                    Images = p.TMarketProductImages
                        .OrderBy(img => img.FSortOrder)
                        .Select(img => new ProductImageDto
                        {
                            ImageId = img.FProductImageId,
                            ImageUrl = ImageBaseUrl + img.FImageUrl,
                            SortOrder = img.FSortOrder
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            return Ok(product);
        }

        // 賣家更新商品（精細圖片處理）
        [HttpPut("seller/{id:int}")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] MarketProductUpdateDto dto)
        {
            var product = await _context.TMarketProducts
                .Include(p => p.TMarketProductImages)
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == 7); // 之後換 Token

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 更新主表
                product.FProductName = dto.ProductName;
                product.FProductsCategoryNo = dto.ProductsCategoryNo;
                product.FPrice = dto.Price;
                product.FStock = dto.Stock;
                product.FBrandOrOrigin = dto.BrandOrOrigin;
                product.FDescription = dto.Description;
                product.FManufacturingDate = dto.ManufacturingDate;
                product.FExpirationDate = dto.ExpirationDate;

                //更新狀態
                if (dto.ProductStatus.HasValue)
                    product.FProductStatus = dto.ProductStatus.Value;

                // 刪除指定圖片
                if (dto.DeleteImageIds != null && dto.DeleteImageIds.Any())
                {
                    var toDelete = product.TMarketProductImages
                        .Where(img => dto.DeleteImageIds.Contains(img.FProductImageId))
                        .ToList();

                    foreach (var img in toDelete)
                    {
                        // 刪除實體檔案
                        var filePath = Path.Combine(_env.WebRootPath,
                            img.FImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);

                        _context.TMarketProductImages.Remove(img);
                    }
                }

                // 新增圖片
                if (dto.NewImages != null && dto.NewImages.Any())
                {
                    var uploadFolder = Path.Combine(_env.WebRootPath, "ProductImageUploads");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // 現有最大 sortOrder
                    var maxSort = product.TMarketProductImages
                        .Where(img => !(dto.DeleteImageIds != null &&
                                         dto.DeleteImageIds.Contains(img.FProductImageId)))
                        .Select(img => (int)img.FSortOrder)
                        .DefaultIfEmpty(-1)
                        .Max();

                    for (int i = 0; i < dto.NewImages.Count; i++)
                    {
                        var file = dto.NewImages[i];
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadFolder, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.TMarketProductImages.Add(new TMarketProductImage
                        {
                            FProductId = product.FProductId,
                            FImageUrl = $"/ProductImageUploads/{fileName}",
                            FSortOrder = (short)(maxSort + 1 + i)
                        });
                    }
                }

                // 更新圖片排序
                if (dto.ImageOrder != null && dto.ImageOrder.Any())
                {
                    var imageMap = product.TMarketProductImages
                        .ToDictionary(img => img.FProductImageId);

                    for (int i = 0; i < dto.ImageOrder.Count; i++)
                    {
                        if (imageMap.TryGetValue(dto.ImageOrder[i], out var img))
                            img.FSortOrder = (short)i;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"更新商品失敗：{message}");
            }

            return Ok(new { message = "更新成功" });
        }

        // PATCH /api/MarketProduct/seller/{id}/stock
        [HttpPatch("seller/{id:int}/stock")]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] MarketProductStockUpdateDto dto)
        {
            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == 7); // 之後換 Token

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            if (dto.Stock < 0)
                return BadRequest(new { message = "庫存不能為負數" });

            product.FStock = dto.Stock;

            // 庫存大於 0 且目前是已售完狀態，自動改回販售中
            if (dto.Stock > 0 && product.FProductStatus == 2)
                product.FProductStatus = 1;

            // 庫存為 0 且目前是販售中，自動改成已售完
            if (dto.Stock == 0 && product.FProductStatus == 1)
                product.FProductStatus = 2;

            await _context.SaveChangesAsync();

            return Ok(new { message = "庫存更新成功", stock = product.FStock, productStatus = product.FProductStatus });
        }
    }

}
