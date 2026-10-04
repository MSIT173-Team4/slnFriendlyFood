using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;
using prjFriendlyFoodWebAPI.Services.ImageUpload;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeAssetService(
    ICloudinaryService cloudinaryService,
    ILogger<RecipeAssetService> logger) : IRecipeAssetService
{
    private const int RecommendedWidth = 1200;
    private const int RecommendedHeight = 900;

    public async Task<ServiceResult<RecipeAssetResponseDto>> SaveCoverAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return ServiceResult<RecipeAssetResponseDto>.Validation("請選擇食譜圖片。");
        }

        var validationMessage = cloudinaryService.ValidateImage(file);
        if (validationMessage is not null)
        {
            return ServiceResult<RecipeAssetResponseDto>.Validation(validationMessage);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var uploadedImage = await cloudinaryService.UploadImageAsync(
                file,
                CloudinaryFolders.Recipes);

            return ServiceResult<RecipeAssetResponseDto>.Created(
                new RecipeAssetResponseDto(
                    uploadedImage.Url,
                    uploadedImage.PublicId,
                    file.FileName,
                    file.Length,
                    file.ContentType,
                    RecommendedWidth,
                    RecommendedHeight),
                "食譜圖片已上傳至雲端。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "上傳 Recipe 圖片至 Cloudinary 失敗。FileName: {FileName}",
                file.FileName);
            return ServiceResult<RecipeAssetResponseDto>.Unexpected(
                "圖片上傳失敗，請稍後再試。");
        }
    }
}
