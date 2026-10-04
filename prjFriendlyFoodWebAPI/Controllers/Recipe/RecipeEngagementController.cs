using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/{recipeId:int}")]
[Authorize]
public sealed class RecipeEngagementController(
    IRecipeEngagementService engagementService) : BaseController
{
    [HttpPost("view")]
    public async Task<ActionResult<ApiResponse<RecipeViewDto>>> RecordView(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var result = await engagementService.RecordViewAsync(recipeId, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("like")]
    public async Task<ActionResult<ApiResponse<RecipeEngagementDto>>> ToggleLike(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var result = await engagementService.ToggleLikeAsync(
            recipeId,
            User.GetUserId(),
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("favorite")]
    public async Task<ActionResult<ApiResponse<RecipeEngagementDto>>> ToggleFavorite(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var result = await engagementService.ToggleFavoriteAsync(
            recipeId,
            User.GetUserId(),
            cancellationToken);
        return FromServiceResult(result);
    }
}
