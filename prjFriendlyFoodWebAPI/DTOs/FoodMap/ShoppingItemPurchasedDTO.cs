namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // 採買模式打勾後回傳的結果
    public class ShoppingItemPurchasedDTO
    {
        public int FShoppingListItemId { get; set; }
        public bool FIsPurchased { get; set; }
    }
}
