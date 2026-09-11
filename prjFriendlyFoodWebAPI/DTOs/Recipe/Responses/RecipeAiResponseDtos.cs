namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record IngredientLocalizationDto(
    string RawInput,
    string? StandardTaiwaneseName,
    string Category);

public sealed record ParsedRecipeDto(
    string RecipeTitle,
    IReadOnlyCollection<ParsedRecipeIngredientDto> Ingredients,
    IReadOnlyCollection<ParsedRecipeStepDto> Steps);

public sealed record ParsedRecipeIngredientDto(
    string Name,
    decimal Amount,
    string Unit);

public sealed record ParsedRecipeStepDto(
    int StepNumber,
    string Description);

public sealed record ChefRecommendationDto(
    string Recommendation,
    DateTime GeneratedAt);
