using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipeRecommendationService
{
    Task<ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>> GetZeroWasteRecommendationsAsync(
        int userId,
        int limit,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<TrendingRecipeDto>>> GetTrendingRecipesAsync(
        int limit,
        CancellationToken cancellationToken);
}
