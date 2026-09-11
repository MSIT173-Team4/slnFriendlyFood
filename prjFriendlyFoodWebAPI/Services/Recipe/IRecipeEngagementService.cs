using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipeEngagementService
{
    Task<ServiceResult<RecipeEngagementDto>> ToggleLikeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeEngagementDto>> ToggleFavoriteAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken);
}
