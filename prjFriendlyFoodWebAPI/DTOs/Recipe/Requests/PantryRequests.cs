using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed class CreatePantryItemRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }

    [Range(1, int.MaxValue)]
    public int IngredientId { get; init; }

    [Range(0.01, 100000)]
    public decimal Amount { get; init; }

    [Required, StringLength(20)]
    public string Unit { get; init; } = string.Empty;

    public DateOnly? ExpirationDate { get; init; }

    [Required, StringLength(20)]
    public string StorageLocation { get; init; } = "冷藏";

    [StringLength(150)]
    public string? Note { get; init; }
}

public sealed class UpdatePantryItemRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }

    [Range(0.01, 100000)]
    public decimal Amount { get; init; }

    [Required, StringLength(20)]
    public string Unit { get; init; } = string.Empty;

    public DateOnly? ExpirationDate { get; init; }

    [Required, StringLength(20)]
    public string StorageLocation { get; init; } = "冷藏";

    [StringLength(150)]
    public string? Note { get; init; }
}
