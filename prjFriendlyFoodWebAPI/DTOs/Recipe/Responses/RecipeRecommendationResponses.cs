namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record RecipeRecommendationDto(
    int RecipeId,
    string Title,
    string? CoverImageUrl,
    string CategoryName,
    int CookingMinutes,
    decimal MatchPercentage,
    int MatchedIngredientCount,
    int RequiredIngredientCount,
    bool IsReadyToCook,
    IReadOnlyCollection<RecipeMatchIngredientDto> AvailableIngredients,
    IReadOnlyCollection<RecipeMatchIngredientDto> ExpiringIngredients,
    IReadOnlyCollection<RecipeMatchIngredientDto> MissingIngredients,
    string Explanation);

public sealed record RecipeMatchIngredientDto(
    int IngredientId,
    string Name,
    int? DaysUntilExpiration);

public sealed record TrendingRecipeDto(
    RecipeSummaryDto Recipe,
    decimal TimeDecayScore);

public sealed record RecipeViewDto(
    int RecipeId,
    int ViewCount);
