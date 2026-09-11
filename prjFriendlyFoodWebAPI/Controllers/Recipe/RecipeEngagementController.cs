using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/{recipeId:int}")]
public sealed class RecipeEngagementController(
    IRecipeEngagementService engagementService) : BaseController
{
    [HttpPost("like")]
    public async Task<ActionResult<ApiResponse<RecipeEngagementDto>>> ToggleLike(
        int recipeId,
        [FromBody] UserRecipeActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await engagementService.ToggleLikeAsync(
            recipeId,
            request.UserId,
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("favorite")]
    public async Task<ActionResult<ApiResponse<RecipeEngagementDto>>> ToggleFavorite(
        int recipeId,
        [FromBody] UserRecipeActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await engagementService.ToggleFavoriteAsync(
            recipeId,
            request.UserId,
            cancellationToken);
        return FromServiceResult(result);
    }
}
