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
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> GetRecipeAsync(
        int recipeId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> CreateRecipeAsync(
        CreateRecipeRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeDetailDto>> UpdateRecipeAsync(
        int recipeId,
        UpdateRecipeRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteRecipeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>> CompleteCookingAsync(
        CompleteCookingRequestDto request,
        CancellationToken cancellationToken);

    Task<ServiceResult<RecipeMetadataDto>> GetMetadataAsync(CancellationToken cancellationToken);
}
