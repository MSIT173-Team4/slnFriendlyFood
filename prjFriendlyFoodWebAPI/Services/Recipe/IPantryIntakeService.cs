using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IPantryIntakeService
{
    Task<ServiceResult<PantryItemDto>> AddItemAsync(
        AddPantryItemRequestDto request,
        CancellationToken cancellationToken);
}
