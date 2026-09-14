using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/ingredients")]
public sealed class RecipeIngredientController(
    IRecipeIngredientNormalizationService normalizationService) : BaseController
{
    [HttpPost("normalize")]
    public async Task<ActionResult<ApiResponse<IngredientNormalizationResponseDto>>> Normalize(
        [FromBody] IngredientNormalizationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await normalizationService.NormalizeAsync(request, cancellationToken);
        return FromServiceResult(result);
    }
}
