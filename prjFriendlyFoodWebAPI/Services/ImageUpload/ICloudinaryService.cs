namespace prjFriendlyFoodWebAPI.Services.ImageUpload
{
    // 上傳成功後需要存進 DB 的兩個值
    public record UploadedImageDto(string Url, string PublicId);

    public interface ICloudinaryService
    {
        // 回傳 null 代表通過；有字串代表錯誤訊息。
        // 驗證獨立成一個方法，呼叫端才能「全部驗證完再開始上傳」，避免傳到一半才發現第 4 張不合格
        string? ValidateImage(IFormFile file);

        Task<UploadedImageDto> UploadImageAsync(IFormFile file, string folder);

        // 給搬移舊圖片用：本機檔案沒有 IFormFile，只能用 Stream
        Task<UploadedImageDto> UploadImageAsync(Stream stream, string fileName, string folder);

        Task DeleteImageAsync(string publicId);

        // 補償用：刪除失敗只記 log 不丟例外，避免蓋掉真正造成失敗的原始錯誤
        Task DeleteImagesSafelyAsync(IEnumerable<string> publicIds);
    }
}
