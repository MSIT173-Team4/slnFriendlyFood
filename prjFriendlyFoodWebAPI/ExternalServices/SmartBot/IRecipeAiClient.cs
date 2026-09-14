using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.ExternalServices.SmartBot;

public interface IRecipeAiClient
{
    Task<ServiceResult<IngredientLocalizationDto>> LocalizeIngredientAsync(
        string ingredientName,
        CancellationToken cancellationToken);

    Task<ServiceResult<ParsedRecipeDto>> ParseRecipeAsync(
        string content,
        CancellationToken cancellationToken);

    Task<ServiceResult<ChefRecommendationDto>> RecommendRecipeAsync(
        IReadOnlyCollection<string> ingredientNames,
        CancellationToken cancellationToken);
}
