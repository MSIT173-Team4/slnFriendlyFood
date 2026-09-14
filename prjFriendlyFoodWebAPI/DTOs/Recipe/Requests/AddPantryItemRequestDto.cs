namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed record AddPantryItemRequestDto(
    int UserId,
    string IngredientName,
    decimal Amount,
    string Unit,
    string StorageLocation,
    DateOnly ExpirationDate,
    string? Note);
