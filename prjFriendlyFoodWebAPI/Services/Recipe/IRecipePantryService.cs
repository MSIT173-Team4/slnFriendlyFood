using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipePantryService
{
    Task<ServiceResult<IReadOnlyCollection<PantryItemDto>>> GetItemsAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<PantryItemDto>> CreateItemAsync(
        int userId,
        CreatePantryItemRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PantryItemDto>> UpdateItemAsync(
        int pantryId,
        int userId,
        UpdatePantryItemRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteItemAsync(
        int pantryId,
        int userId,
        CancellationToken cancellationToken);
}
