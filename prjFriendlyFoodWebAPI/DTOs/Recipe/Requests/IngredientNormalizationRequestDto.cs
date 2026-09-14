using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed class IngredientNormalizationRequestDto
{
    [Required, StringLength(50)]
    public string IngredientName { get; init; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Amount { get; init; }

    [Required, StringLength(20)]
    public string Unit { get; init; } = string.Empty;
}
