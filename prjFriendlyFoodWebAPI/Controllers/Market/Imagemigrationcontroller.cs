using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.ImageUpload;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{

    // ============================================================
    // ⚠ 暫時性 Controller：舊商品圖片從 wwwroot 搬到 Cloudinary
    //   搬移完成、前台確認正常後，整個檔案刪除
    // ============================================================
    [Route("api/[controller]")]
    [ApiController]
    public class ImageMigrationController : ControllerBase
    {
        private const string LocalPrefix = "/ProductImageUploads/";

        private readonly FriendlyFoodDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICloudinaryService _cloudinaryService;

        public ImageMigrationController(
            FriendlyFoodDbContext context,
            IWebHostEnvironment env,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _env = env;
            _cloudinaryService = cloudinaryService;
        }

        // POST /api/ImageMigration/products                 → 試跑（預設），只列清單不上傳
        // POST /api/ImageMigration/products?dryRun=false    → 正式執行
        [HttpPost("products")]
        public async Task<IActionResult> MigrateProductImages([FromQuery] bool dryRun = true)
        {
            // 只允許開發環境：萬一這個檔案忘記刪就被部署，正式環境也觸發不了
            if (!_env.IsDevelopment())
                return NotFound();

            // 只挑「還沒搬」的：fPublicId 是 NULL 且是本機路徑
            // 所以中途失敗重跑，已搬過的不會重複上傳
            var images = await _context.TMarketProductImages
                .Where(img => img.FPublicId == null && img.FImageUrl.StartsWith(LocalPrefix))
                .OrderBy(img => img.FProductImageId)
                .ToListAsync();

            var ready = new List<TMarketProductImage>();
            var missing = new List<object>();

            foreach (var img in images)
            {
                if (System.IO.File.Exists(ToPhysicalPath(img.FImageUrl)))
                    ready.Add(img);
                else
                    missing.Add(new { img.FProductImageId, img.FProductId, img.FImageUrl });
            }

            if (dryRun)
            {
                return Ok(new
                {
                    dryRun = true,
                    total = images.Count,
                    readyCount = ready.Count,
                    missingCount = missing.Count,
                    ready = ready.Select(img => new { img.FProductImageId, img.FProductId, img.FImageUrl }),
                    missing
                });
            }

            // ===== 正式執行：依序一張一張搬 =====
            var succeeded = new List<object>();
            var failed = new List<object>();

            foreach (var img in ready)
            {
                var physicalPath = ToPhysicalPath(img.FImageUrl);
                UploadedImageDto? uploaded = null;

                try
                {
                    await using (var stream = System.IO.File.OpenRead(physicalPath))
                    {
                        uploaded = await _cloudinaryService.UploadImageAsync(
                            stream, Path.GetFileName(physicalPath), CloudinaryFolders.Products);
                    }

                    img.FImageUrl = uploaded.Url;
                    img.FPublicId = uploaded.PublicId;

                    // 每張各自存檔：中途失敗時，前面搬好的都已經確實寫進 DB
                    await _context.SaveChangesAsync();

                    succeeded.Add(new { img.FProductImageId, img.FProductId });
                }
                catch (Exception ex)
                {
                    // 上傳成功但存 DB 失敗 → 補償刪除這張
                    if (uploaded != null)
                        await _cloudinaryService.DeleteImagesSafelyAsync(new[] { uploaded.PublicId });

                    // 關鍵：EF 還記得這筆的修改，不還原的話，下一張 SaveChanges 會把它一起送出，
                    // 變成 DB 指向一張剛被刪掉的圖
                    var entry = _context.Entry(img);
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;

                    failed.Add(new
                    {
                        img.FProductImageId,
                        img.FProductId,
                        error = ex.InnerException?.Message ?? ex.Message
                    });
                }
            }

            return Ok(new
            {
                dryRun = false,
                total = images.Count,
                succeededCount = succeeded.Count,
                failedCount = failed.Count,
                missingCount = missing.Count,
                failed,
                missing
            });
        }

        private string ToPhysicalPath(string relativeUrl)
            => Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/'));
    }
}
