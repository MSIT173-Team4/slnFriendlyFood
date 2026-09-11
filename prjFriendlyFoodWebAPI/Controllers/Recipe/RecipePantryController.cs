using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/pantry")]
public sealed class RecipePantryController(IRecipePantryService pantryService) : BaseController
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PantryItemDto>>>> GetItems(
        int userId,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.GetItemsAsync(userId, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> CreateItem(
        [FromBody] CreatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.CreateItemAsync(request, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPut("{pantryId:int}")]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> UpdateItem(
        int pantryId,
        [FromBody] UpdatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.UpdateItemAsync(pantryId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpDelete("{pantryId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteItem(
        int pantryId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.DeleteItemAsync(pantryId, userId, cancellationToken);
        return FromServiceResult(result);
    }
}
