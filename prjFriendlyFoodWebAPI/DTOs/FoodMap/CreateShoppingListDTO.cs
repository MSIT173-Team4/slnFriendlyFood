namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class CreateShoppingListDTO
    {
        public string Name { get; set; } = string.Empty;

        public List<ShoppingListItemDTO> Items { get; set; } = [];
    }
}
