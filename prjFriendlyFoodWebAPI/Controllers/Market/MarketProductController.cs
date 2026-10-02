using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using prjFriendlyFoodWebAPI.Services.ImageUpload;
using prjFriendlyFoodWebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using prjFriendlyFoodWebAPI.Services.Market;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketProductController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ISellerIdentityService _sellerIdentity;

        private const int MaxImageCount = 5;
        private const int PublicPageSize = 12;   // 商城商品列表一頁幾筆（前端 pageSize 要一致）

        // 注入 IWebHostEnvironment 才能拿到 wwwroot 的實際路徑
        public MarketProductController(
                FriendlyFoodDbContext context,
                IWebHostEnvironment env,
                ICloudinaryService cloudinaryService,
                ISellerIdentityService sellerIdentity)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _sellerIdentity = sellerIdentity;
        }

        // 賣家後台 API 共用：取得目前登入者「生效中」的賣家 Id，不是有效賣家回傳 null
        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var seller = await _sellerIdentity.GetActiveSellerAsync(User.GetUserId());
            return seller?.FId;
        }

        private IActionResult NotSeller() =>
            StatusCode(403, new { message = "您尚未開通賣場，或賣場已停權" });

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
             .Select(img => img.FImageUrl)
             .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)] // 30MB(5 張 × 5MB + 餘裕)
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromForm] MarketProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();

            // ===== Step 1：驗證產品分類跟圖片，全部通過才往下走 =====
            var categoryExists = await _context.TMarketProductCategories
                .AnyAsync(c => c.FCategoryNo == dto.ProductsCategoryNo);
            if (!categoryExists)
                return BadRequest(new { message = "商品分類不存在" });

            var files = dto.Images?.ToList() ?? new List<IFormFile>();

            if (files.Count > MaxImageCount)
                return BadRequest(new { message = $"商品圖片最多 {MaxImageCount} 張" });

            foreach (var file in files)
            {
                var error = _cloudinaryService.ValidateImage(file);
                if (error != null)
                    return BadRequest(new { message = error });
            }

            // ===== Step 2：上傳到 Cloudinary =====
            // 用 List 記住「已經傳上去的」，失敗時才知道要清掉哪些

            var uploadTasks = files
                .Select(file => _cloudinaryService.UploadImageAsync(file, CloudinaryFolders.Products))
                .ToList();

            try
            {
                // 一次等全部完成
                await Task.WhenAll(uploadTasks);
            }
            catch (Exception ex)
            {
                var succeededIds = uploadTasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .Select(t => t.Result.PublicId);

                await _cloudinaryService.DeleteImagesSafelyAsync(succeededIds);
                return StatusCode(502, new { message = $"圖片上傳失敗：{ex.Message}" });
            }

            var uploaded = uploadTasks.Select(t => t.Result).ToList();

            // ===== Step 3：寫入 DB（商品 + 圖片，一次 SaveChanges）=====
            var product = new TMarketProduct
            {
                FSellerId = sellerId.Value,
                FProductNo = $"P{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
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
                FProductDate = DateTime.Now
            };

            // 掛在導覽屬性底下，EF 把新商品的 FProductId 填進每張圖片
            for (int i = 0; i < uploaded.Count; i++)
            {
                product.TMarketProductImages.Add(new TMarketProductImage
                {
                    FImageUrl = uploaded[i].Url,
                    FPublicId = uploaded[i].PublicId,
                    FSortOrder = (short)i
                });
            }

            ProductStockRules.SyncStatusWithStock(product);
            _context.TMarketProducts.Add(product);

            try
            {
                // 一次 SaveChanges = 一個 transaction，商品和圖片要嘛都成功、要嘛都 rollback
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // DB 已自動 rollback，但 Cloudinary 不歸 DB 管 → 補償：刪掉這次上傳的圖
                await _cloudinaryService.DeleteImagesSafelyAsync(uploaded.Select(u => u.PublicId));

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

            int? userId = User.TryGetUserId();
            // 分頁 + mapping 到 DTO
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((Math.Max(dto.Page, 1) - 1) * PublicPageSize)
                .Take(PublicPageSize)
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
                                 .Select(img => img.FImageUrl)
                                 .ToList(),
                    IsFavorite = userId != null && _context.TMarketProductFavorites
                    .Any(f => f.FProductId == p.FProductId && f.FUserId == userId),
                    IsOwnProduct = userId != null && p.FSeller.FUserId == userId
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
            int? userId = User.TryGetUserId();
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
                        .Select(img => img.FImageUrl)
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
                        .Count(sp => sp.FSellerId == p.FSellerId && sp.FProductStatus == 1),
                    IsFavorite = userId != null && _context.TMarketProductFavorites
                        .Any(f => f.FProductId == p.FProductId && f.FUserId == userId),
                    IsOwnProduct = userId != null && p.FSeller.FUserId == userId
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
        [Authorize]
        public async Task<IActionResult> GetSellerProducts(
            [FromQuery] byte? status,
            [FromQuery] bool lowStock = false,
            [FromQuery] int page = 1,
            [FromQuery] string? keyword = null)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();
            // 之後換成從 Token 拿 sellerId
            var query = _context.TMarketProducts
                .Where(p => p.FSellerId == sellerId.Value)
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
                        .Select(img => img.FImageUrl)
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
        [Authorize]
        public async Task<IActionResult> UpdateProductStatus(
            int id, [FromBody] UpdateProductStatusDto dto)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();
            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == sellerId.Value); // 之後改 Token

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            product.FProductStatus = (byte)dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "狀態更新成功" });
        }

        // 賣家取得單一商品（含所有狀態、含圖片 id）
        [HttpGet("seller/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetSellerProductDetail(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();
            var product = await _context.TMarketProducts
                .Where(p => p.FProductId == id && p.FSellerId == sellerId.Value) 
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
                            ImageUrl = img.FImageUrl,
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
        [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)] // 30MB(5 張 × 5MB + 餘裕)
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] MarketProductUpdateDto dto)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();
            var product = await _context.TMarketProducts
                .Include(p => p.TMarketProductImages)
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == sellerId.Value); 

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            // ===== Step 1：驗證（全部通過才開始上傳）=====
            var categoryExists = await _context.TMarketProductCategories
                .AnyAsync(c => c.FCategoryNo == dto.ProductsCategoryNo);
            if (!categoryExists)
                return BadRequest(new { message = "商品分類不存在" });

            var deleteIds = dto.DeleteImageIds?.ToList() ?? new List<int>();

            // 傳入不屬於此商品的圖片 Id → 明確回報，而不是安靜地忽略
            var ownedImageIds = product.TMarketProductImages
                .Select(img => img.FProductImageId)
                .ToHashSet();
            var invalidIds = deleteIds.Where(did => !ownedImageIds.Contains(did)).ToList();
            if (invalidIds.Any())
                return BadRequest(new { message = $"以下圖片不屬於此商品：{string.Join(", ", invalidIds)}" });

            var newFiles = dto.NewImages?.ToList() ?? new List<IFormFile>();

            // 要看「更新後」的總張數：保留的舊圖 + 新增的圖
            var remainingCount = product.TMarketProductImages
                .Count(img => !deleteIds.Contains(img.FProductImageId));
            if (remainingCount + newFiles.Count > MaxImageCount)
                return BadRequest(new { message = $"商品圖片最多 {MaxImageCount} 張" });

            foreach (var file in newFiles)
            {
                var error = _cloudinaryService.ValidateImage(file);
                if (error != null)
                    return BadRequest(new { message = error });
            }

            // ===== Step 2：同時上傳新圖 =====
            var uploadTasks = newFiles
                .Select(file => _cloudinaryService.UploadImageAsync(file, CloudinaryFolders.Products))
                .ToList();

            try
            {
                await Task.WhenAll(uploadTasks);
            }
            catch (Exception ex)
            {
                var succeededIds = uploadTasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .Select(t => t.Result.PublicId);

                await _cloudinaryService.DeleteImagesSafelyAsync(succeededIds);
                return StatusCode(502, new { message = $"圖片上傳失敗：{ex.Message}" });
            }

            var uploaded = uploadTasks.Select(t => t.Result).ToList();

            // ===== Step 3：更新 DB（一次 SaveChanges）=====
            // 更新主表
            product.FProductName = dto.ProductName;
            product.FProductsCategoryNo = dto.ProductsCategoryNo;
            product.FPrice = dto.Price;
            product.FStock = dto.Stock;
            product.FBrandOrOrigin = dto.BrandOrOrigin;
            product.FDescription = dto.Description;
            product.FManufacturingDate = dto.ManufacturingDate;
            product.FExpirationDate = dto.ExpirationDate;

            if (dto.ProductStatus.HasValue)
                product.FProductStatus = dto.ProductStatus.Value;

            ProductStockRules.SyncStatusWithStock(product);

            // 移除圖片「資料」，實體檔案先不刪：DB 失敗會 rollback，檔案刪了卻救不回來
            // 用 product 底下的圖片去篩選，別人商品的圖片 Id 傳進來也刪不到
            var toDelete = product.TMarketProductImages
                .Where(img => deleteIds.Contains(img.FProductImageId))
                .ToList();

            // 先記下要刪的實體位置，Remove 之後這些物件就不在 product 底下了
            var cloudinaryIdsToDelete = toDelete
                .Where(img => img.FPublicId != null)
                .Select(img => img.FPublicId!)
                .ToList();

            foreach (var img in toDelete)
                _context.TMarketProductImages.Remove(img);

            // 新圖接在保留圖片的最後面
            var maxSort = product.TMarketProductImages
                .Where(img => !deleteIds.Contains(img.FProductImageId))
                .Select(img => (int)img.FSortOrder)
                .DefaultIfEmpty(-1)
                .Max();

            for (int i = 0; i < uploaded.Count; i++)
            {
                product.TMarketProductImages.Add(new TMarketProductImage
                {
                    FImageUrl = uploaded[i].Url,
                    FPublicId = uploaded[i].PublicId,
                    FSortOrder = (short)(maxSort + 1 + i)
                });
            }

            // 更新既有圖片的排序
            if (dto.ImageOrder != null && dto.ImageOrder.Any())
            {
                var imageMap = product.TMarketProductImages
                    .Where(img => img.FProductImageId != 0) // 新圖還沒有 Id，排除
                    .ToDictionary(img => img.FProductImageId);

                for (int i = 0; i < dto.ImageOrder.Count; i++)
                {
                    if (imageMap.TryGetValue(dto.ImageOrder[i], out var img))
                        img.FSortOrder = (short)i;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // DB 已 rollback，舊圖都還在；只要清掉這次新上傳的圖
                await _cloudinaryService.DeleteImagesSafelyAsync(uploaded.Select(u => u.PublicId));

                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"更新商品失敗：{message}");
            }

            // ===== Step 4：刪除圖片 =====
            await _cloudinaryService.DeleteImagesSafelyAsync(cloudinaryIdsToDelete);
            return Ok(new { message = "更新成功" });
        }

        // PATCH /api/MarketProduct/seller/{id}/stock
        [HttpPatch("seller/{id:int}/stock")]
        [Authorize]
        public async Task<IActionResult> UpdateProductStock(int id, [FromBody] MarketProductStockUpdateDto dto)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return NotSeller();
            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == id && p.FSellerId == sellerId.Value); 

            if (product == null)
                return NotFound(new { message = "商品不存在或無權限" });

            if (dto.Stock < 0)
                return BadRequest(new { message = "庫存不能為負數" });

            product.FStock = dto.Stock;

            ProductStockRules.SyncStatusWithStock(product);

            await _context.SaveChangesAsync();

            return Ok(new { message = "庫存更新成功", stock = product.FStock, productStatus = product.FProductStatus });
        }
    }

}
