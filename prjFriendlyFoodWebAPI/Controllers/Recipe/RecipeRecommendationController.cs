using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe")]
[Authorize]
public sealed class RecipeRecommendationController(
    IRecipeRecommendationService recommendationService) : BaseController
{
    [HttpGet("recommendations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RecipeRecommendationDto>>>>
        GetZeroWasteRecommendations(
            [FromQuery] int limit = 12,
            CancellationToken cancellationToken = default)
    {
        var result = await recommendationService.GetZeroWasteRecommendationsAsync(
            User.GetUserId(),
            limit,
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpGet("trending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TrendingRecipeDto>>>>
        GetTrendingRecipes(
            [FromQuery] int limit = 8,
            CancellationToken cancellationToken = default)
    {
        var result = await recommendationService.GetTrendingRecipesAsync(limit, cancellationToken);
        return FromServiceResult(result);
    }
}
