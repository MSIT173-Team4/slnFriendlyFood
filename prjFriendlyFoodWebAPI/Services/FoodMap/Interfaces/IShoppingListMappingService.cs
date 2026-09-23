namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IShoppingListMappingService
    {
        // 輸入一份採買清單，回傳「這份清單需要用到哪些店家分類」，
        // 以及每個店家分類底下對應到哪些 ShoppingListItem（供後面 Coverage 計算用）。
        Task<ShoppingListMappingResult> GetPlaceCategoriesForShoppingListAsync(
            int shoppingListId,
            CancellationToken cancellationToken = default);
    }
}
