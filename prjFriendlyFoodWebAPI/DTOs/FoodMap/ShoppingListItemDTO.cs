namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class ShoppingListItemDTO
    {
        public int FShoppingListItemId { get; set; }
        public int FIngredientId { get; set; }
        public string FIngredientName { get; set; } = string.Empty;
        public decimal? FQuantity { get; set; }
        public bool FIsPurchased { get; set; }


    }
}
