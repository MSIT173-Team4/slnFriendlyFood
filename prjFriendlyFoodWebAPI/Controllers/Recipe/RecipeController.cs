using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe")]
public sealed class RecipeController(IRecipeService recipeService) : BaseController
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyCollection<RecipeSummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RecipeSummaryDto>>>> GetRecipes(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? tag,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.GetRecipesAsync(
            search,
            categoryId,
            tag,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpGet("metadata")]
    public async Task<ActionResult<ApiResponse<RecipeMetadataDto>>> GetMetadata(
        CancellationToken cancellationToken)
    {
        var result = await recipeService.GetMetadataAsync(cancellationToken);
        return FromServiceResult(result);
    }

    [HttpGet("{recipeId:int}")]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> GetRecipe(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.GetRecipeAsync(recipeId, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> CreateRecipe(
        [FromBody] CreateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.CreateRecipeAsync(request, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPut("{recipeId:int}")]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> UpdateRecipe(
        int recipeId,
        [FromBody] UpdateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.UpdateRecipeAsync(recipeId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpDelete("{recipeId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRecipe(
        int recipeId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.DeleteRecipeAsync(recipeId, userId, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("complete-cooking")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CookingDeductionResultDto>>>> CompleteCooking(
        [FromBody] CompleteCookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeService.CompleteCookingAsync(request, cancellationToken);
        return FromServiceResult(result);
    }
}
