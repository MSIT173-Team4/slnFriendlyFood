namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class ShoppingListDTO
    {
        public int FShoppingListId { get; set; }
        public string FListName { get; set; } = null!;

        public string FStatus { get; set; } = null!;

        public DateTime FCreatedTime { get; set; }

        public DateTime? FUpdatedTime { get; set; }
    }
}
