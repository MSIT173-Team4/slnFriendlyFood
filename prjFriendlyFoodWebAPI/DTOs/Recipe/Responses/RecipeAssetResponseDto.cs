namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record RecipeAssetResponseDto(
    string Url,
    string FileName,
    long FileSize,
    string ContentType,
    int RecommendedWidth,
    int RecommendedHeight);
