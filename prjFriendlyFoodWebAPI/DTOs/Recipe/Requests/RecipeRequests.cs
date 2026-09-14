using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed class CreateRecipeRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(100)]
    public string Title { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? CoverImageUrl { get; init; }

    [StringLength(15)]
    public string? YouTubeVideoId { get; init; }

    public string? AiPrepTips { get; init; }

    public bool IsAiGenerated { get; init; }

    [Range(1, 20)]
    public int DefaultServings { get; init; } = 2;

    [Range(0, 1440)]
    public int CookingMinutes { get; init; }

    [Range(0, 100000)]
    public decimal TotalCalories { get; init; }

    [MinLength(1)]
    public IReadOnlyCollection<RecipeIngredientInputDto> Ingredients { get; init; } = [];

    [MinLength(1)]
    public IReadOnlyCollection<RecipeStepInputDto> Steps { get; init; } = [];

    public IReadOnlyCollection<int> TagIds { get; init; } = [];
}

public sealed class UpdateRecipeRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(100)]
    public string Title { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? CoverImageUrl { get; init; }

    [StringLength(15)]
    public string? YouTubeVideoId { get; init; }

    public string? AiPrepTips { get; init; }

    public bool IsAiGenerated { get; init; }

    [Range(1, 20)]
    public int DefaultServings { get; init; } = 2;

    [Range(0, 1440)]
    public int CookingMinutes { get; init; }

    [Range(0, 100000)]
    public decimal TotalCalories { get; init; }

    [MinLength(1)]
    public IReadOnlyCollection<RecipeIngredientInputDto> Ingredients { get; init; } = [];

    [MinLength(1)]
    public IReadOnlyCollection<RecipeStepInputDto> Steps { get; init; } = [];

    public IReadOnlyCollection<int> TagIds { get; init; } = [];
}

public sealed class RecipeIngredientInputDto
{
    public int? IngredientId { get; init; }

    [Required, StringLength(50)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string DisplayAmount { get; init; } = string.Empty;

    [Range(0, 100000)]
    public decimal? BaseAmount { get; init; }

    [StringLength(20)]
    public string? StandardUnit { get; init; }

    public bool IsMain { get; init; } = true;

    public short SortOrder { get; init; }
}

public sealed class RecipeStepInputDto
{
    [Range(1, short.MaxValue)]
    public short StepNumber { get; init; }

    [Required]
    public string Instruction { get; init; } = string.Empty;

    [StringLength(500)]
    public string? ImageUrl { get; init; }

    [Range(0, 86400)]
    public int TimerSeconds { get; init; }
}

public sealed class CompleteCookingRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }

    [Range(1, int.MaxValue)]
    public int RecipeId { get; init; }

    [Range(1, 20)]
    public int TargetServings { get; init; }
}

public sealed class UserRecipeActionRequestDto
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }
}
