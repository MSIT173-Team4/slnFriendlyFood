using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeAssetService(
    IWebHostEnvironment environment,
    ILogger<RecipeAssetService> logger) : IRecipeAssetService
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private const int RecommendedWidth = 1200;
    private const int RecommendedHeight = 900;
    private static readonly IReadOnlyDictionary<string, ImageFileType> AllowedImageTypes =
        new Dictionary<string, ImageFileType>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new(".jpg", IsJpeg),
            ["image/png"] = new(".png", IsPng),
            ["image/webp"] = new(".webp", IsWebP)
        };

    public async Task<ServiceResult<RecipeAssetResponseDto>> SaveCoverAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult<RecipeAssetResponseDto>.Validation("請選擇食譜封面圖片。");
        }

        if (file.Length > MaximumFileSize)
        {
            return ServiceResult<RecipeAssetResponseDto>.Validation("封面圖片不可超過 10 MB。");
        }

        if (!AllowedImageTypes.TryGetValue(file.ContentType.Trim(), out var imageType))
        {
            return ServiceResult<RecipeAssetResponseDto>.Validation(
                "封面圖片僅支援 JPEG、PNG 或 WebP 格式。");
        }

        await using (var sourceStream = file.OpenReadStream())
        {
            var header = new byte[12];
            var bytesRead = await sourceStream.ReadAsync(header, cancellationToken);
            if (!imageType.SignatureValidator(header.AsSpan(0, bytesRead)))
            {
                return ServiceResult<RecipeAssetResponseDto>.Validation(
                    "圖片內容與宣告格式不一致，請重新選擇有效圖片。");
            }
        }

        var fileName = $"{Guid.NewGuid():N}{imageType.Extension}";
        var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRootPath, "RecipeUploads");
        var outputPath = Path.Combine(uploadDirectory, fileName);

        try
        {
            Directory.CreateDirectory(uploadDirectory);
            await using var outputStream = new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);
            await file.CopyToAsync(outputStream, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            DeleteIncompleteFile(outputPath);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteIncompleteFile(outputPath);
            logger.LogError(exception, "儲存 Recipe 封面圖片失敗。FileName: {FileName}", fileName);
            return ServiceResult<RecipeAssetResponseDto>.Unexpected("封面圖片儲存失敗，請稍後再試。");
        }

        return ServiceResult<RecipeAssetResponseDto>.Created(
            new RecipeAssetResponseDto(
                $"/RecipeUploads/{fileName}",
                fileName,
                file.Length,
                file.ContentType,
                RecommendedWidth,
                RecommendedHeight),
            "食譜封面已上傳。");
    }

    private static bool IsJpeg(ReadOnlySpan<byte> header) =>
        header.Length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;

    private static bool IsPng(ReadOnlySpan<byte> header) =>
        header.Length >= 8 &&
        header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });

    private static bool IsWebP(ReadOnlySpan<byte> header) =>
        header.Length >= 12 &&
        header[..4].SequenceEqual("RIFF"u8) &&
        header.Slice(8, 4).SequenceEqual("WEBP"u8);

    private static void DeleteIncompleteFile(string outputPath)
    {
        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
        catch
        {
            // Cleanup failure must not hide the original upload error.
        }
    }

    private sealed record ImageFileType(
        string Extension,
        ImageSignatureValidator SignatureValidator);

    private delegate bool ImageSignatureValidator(ReadOnlySpan<byte> header);
}
