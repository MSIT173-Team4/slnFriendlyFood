using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed class LocalizeIngredientAiRequestDto
{
    [Required, StringLength(50)]
    public string IngredientName { get; init; } = string.Empty;
}

public sealed class ParseRecipeAiRequestDto
{
    [Required, StringLength(10000)]
    public string Content { get; init; } = string.Empty;
}

public sealed class ChefRecommendationAiRequestDto
{
    [MinLength(1)]
    public IReadOnlyCollection<string> IngredientNames { get; init; } = [];
}
