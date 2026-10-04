namespace prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;

public sealed record AddPantryItemRequestDto(
    string IngredientName,
    decimal Amount,
    string Unit,
    string StorageLocation,
    DateOnly ExpirationDate,
    string? Note);
