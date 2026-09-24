using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class ShoppingListMappingResult
    {
        public Dictionary<int, List<ShoppingListItemDTO>> ItemsByPlaceCategory { get; set; } = new();

        public int TotalItemCount { get; set; }
    }
}
