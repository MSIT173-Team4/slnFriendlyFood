using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace prjFriendlyFoodWebAPI.Services.ImageUpload
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

        public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public string? ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "檔案是空的";

            if (file.Length > MaxFileSizeBytes)
                return $"「{file.FileName}」超過 5MB";

            // 副檔名和 ContentType 都檢查：只看副檔名的話，把 .exe 改名成 .jpg 就能騙過
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) ||
                !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return $"「{file.FileName}」格式不支援，只接受 jpg / png / webp";

            return null;
        }

        public async Task<UploadedImageDto> UploadImageAsync(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();
            return await UploadImageAsync(stream, file.FileName, folder);
        }

        public async Task<UploadedImageDto> UploadImageAsync(Stream stream, string fileName, string folder)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                AssetFolder = folder,
                // 上傳時就把寬度壓到 1200px 以內（小圖不會被放大），省儲存空間跟流量
                Transformation = new Transformation().Width(1200).Crop("limit")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            // SDK 不會因為 Cloudinary 回傳錯誤就丟例外，要自己檢查 Error
            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary 上傳失敗：{result.Error.Message}");

            return new UploadedImageDto(result.SecureUrl.AbsoluteUri, result.PublicId);
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

            // "not found" 也視為成功：目標是「這張圖不存在」，已經不存在就達成了
            if (result.Result != "ok" && result.Result != "not found")
                throw new InvalidOperationException($"Cloudinary 刪除失敗：{publicId}，{result.Error?.Message}");
        }

        public async Task DeleteImagesSafelyAsync(IEnumerable<string> publicIds)
        {
            foreach (var publicId in publicIds)
            {
                try
                {
                    await DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    // 留下 PublicId，之後可以到 Cloudinary Console 手動清掉孤兒圖片
                    _logger.LogError(ex, "補償刪除失敗，孤兒圖片 PublicId：{PublicId}", publicId);
                }
            }
        }
    }
}