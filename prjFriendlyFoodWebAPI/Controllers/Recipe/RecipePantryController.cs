using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/pantry")]
[Authorize]
public sealed class RecipePantryController(IRecipePantryService pantryService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PantryItemDto>>>> GetItems(
        CancellationToken cancellationToken)
    {
        var result = await pantryService.GetItemsAsync(User.GetUserId(), cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> CreateItem(
        [FromBody] CreatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.CreateItemAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPut("{pantryId:int}")]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> UpdateItem(
        int pantryId,
        [FromBody] UpdatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.UpdateItemAsync(
            pantryId,
            User.GetUserId(),
            request,
            cancellationToken);
        return FromServiceResult(result);
    }

    [HttpDelete("{pantryId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteItem(
        int pantryId,
        CancellationToken cancellationToken)
    {
        var result = await pantryService.DeleteItemAsync(
            pantryId,
            User.GetUserId(),
            cancellationToken);
        return FromServiceResult(result);
    }
}
