namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;

public sealed record PantryItemDto(
    int PantryId,
    int UserId,
    int IngredientId,
    string IngredientName,
    decimal Amount,
    string Unit,
    DateOnly? ExpirationDate,
    int? DaysLeft,
    string StorageLocation,
    string? Note,
    DateTime CreatedAt);
