using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.ExternalServices.SmartBot;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/ai")]
public sealed class RecipeAiController(IRecipeAiClient recipeAiClient) : BaseController
{
    [HttpPost("localize-ingredient")]
    public async Task<ActionResult<ApiResponse<IngredientLocalizationDto>>> LocalizeIngredient(
        [FromBody] LocalizeIngredientAiRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeAiClient.LocalizeIngredientAsync(
            request.IngredientName,
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("parse-recipe")]
    public async Task<ActionResult<ApiResponse<ParsedRecipeDto>>> ParseRecipe(
        [FromBody] ParseRecipeAiRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeAiClient.ParseRecipeAsync(request.Content, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("chef-recommend")]
    public async Task<ActionResult<ApiResponse<ChefRecommendationDto>>> RecommendRecipe(
        [FromBody] ChefRecommendationAiRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await recipeAiClient.RecommendRecipeAsync(
            request.IngredientNames,
            cancellationToken);
        return FromServiceResult(result);
    }
}
