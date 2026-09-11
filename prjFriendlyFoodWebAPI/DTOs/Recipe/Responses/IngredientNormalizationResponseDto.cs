namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record IngredientNormalizationResponseDto(
    string RawIngredientName,
    string StandardIngredientName,
    int? IngredientId,
    decimal OriginalAmount,
    string OriginalUnit,
    decimal StandardAmount,
    string StandardUnit,
    string DisplayAmount,
    bool ConversionApplied);
