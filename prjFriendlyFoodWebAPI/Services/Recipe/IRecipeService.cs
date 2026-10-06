using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public interface IRecipeService
{
    Task<ServiceResult<IReadOnlyCollection<RecipeSummaryDto>>> GetRecipesAsync(
        string? search,
        int? categoryId,
        string? tag,
        int? userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> GetRecipeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> CreateRecipeAsync(
        int userId,
        CreateRecipeRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> UpdateRecipeAsync(
        int recipeId,
        int userId,
        UpdateRecipeRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteRecipeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>> CompleteCookingAsync(
        int userId,
        CompleteCookingRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeMetadataDto>> GetMetadataAsync(CancellationToken cancellationToken);

    Task<ServiceResult<RecipeAvailabilityDto>> GetAvailabilityAsync(
        int recipeId,
        int userId,
        int targetServings,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeShoppingListDto>> GetShoppingListAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeShoppingListDto>> SaveShoppingListAsync(
        int userId,
        SaveRecipeShoppingListRequestDto request,
        CancellationToken cancellationToken);
}
