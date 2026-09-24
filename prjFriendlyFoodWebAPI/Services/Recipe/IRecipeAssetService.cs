using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipeAssetService
{
    Task<ServiceResult<RecipeAssetResponseDto>> SaveCoverAsync(
        IFormFile? file,
        CancellationToken cancellationToken);
}
