namespace SmartBot.Api.DTOs;

public sealed class PantryAiDiagnosticDto
{
    public string IngredientName { get; set; } = string.Empty;
    public string FreshnessStatus { get; set; } = string.Empty;
    public string RecommendedLocation { get; set; } = "冷藏";
    public string StorageTip { get; set; } = string.Empty;
    public int EstimatedDays { get; set; } = 7;
}
