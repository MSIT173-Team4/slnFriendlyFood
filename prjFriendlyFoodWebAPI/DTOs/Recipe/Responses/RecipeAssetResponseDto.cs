namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record RecipeAssetResponseDto(
    string Url,
    string PublicId,
    string FileName,
    long FileSize,
    string ContentType,
    int RecommendedWidth,
    int RecommendedHeight);
