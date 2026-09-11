using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipeIngredientNormalizationService
{
    Task<ServiceResult<IngredientNormalizationResponseDto>> NormalizeAsync(
        IngredientNormalizationRequestDto request,
        CancellationToken cancellationToken);
}
