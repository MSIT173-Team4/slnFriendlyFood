namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record RecipeSummaryDto(
    int RecipeId,
    string Title,
    string Description,
    string? CoverImageUrl,
    int CookingMinutes,
    decimal TotalCalories,
    int DefaultServings,
    int Views,
    int Likes,
    int Favorites,
    bool IsAiGenerated,
    string CategoryName,
    string AuthorName,
    IReadOnlyCollection<string> Tags);

public sealed record RecipeDetailDto(
    int RecipeId,
    int UserId,
    int CategoryId,
    string Title,
    string Description,
    string? CoverImageUrl,
    string? YouTubeVideoId,
    string? AiPrepTips,
    bool IsAiGenerated,
    int DefaultServings,
    int CookingMinutes,
    decimal TotalCalories,
    int Views,
    int Likes,
    int Favorites,
    string CategoryName,
    string AuthorName,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<RecipeIngredientDto> Ingredients,
    IReadOnlyCollection<RecipeStepDto> Steps);

public sealed record RecipeIngredientDto(
    int IngredientId,
    string Name,
    string DisplayAmount,
    decimal? BaseAmount,
    string? Unit,
    bool IsMain,
    short SortOrder);

public sealed record RecipeStepDto(
    short StepNumber,
    string Instruction,
    string? ImageUrl,
    int TimerSeconds);

public sealed record CookingDeductionResultDto(
    int IngredientId,
    string IngredientName,
    decimal RequiredAmount,
    decimal ConsumedAmount,
    decimal RemainingAmount,
    string Unit,
    bool IsExhausted,
    bool IsInsufficient);

public sealed record RecipeMetadataDto(
    IReadOnlyCollection<RecipeCategoryDto> Categories,
    IReadOnlyCollection<RecipeTagDto> Tags);

public sealed record RecipeCategoryDto(int CategoryId, string Name, short DisplayOrder);

public sealed record RecipeTagDto(int TagId, string Type, string Name);

public sealed record RecipeEngagementDto(
    int RecipeId,
    int UserId,
    bool IsLiked,
    bool IsFavorite,
    int LikeCount,
    int FavoriteCount);
