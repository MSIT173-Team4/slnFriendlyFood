using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class ShoppingListMappingResult
    {
        // Key：店家分類 ID，Value：這個分類可以買到的品項
        public Dictionary<int, List<ShoppingListItemDTO>> ItemsByPlaceCategory { get; set; } = new();

        // 清單裡「還沒買」的全部品項（覆蓋率的分母）
        public List<ShoppingListItemDTO> AllItems { get; set; } = [];

        // 找不到可購買店家類型的品項（食材沒設定分類對照），一定會列在「附近買不到」
        public List<ShoppingListItemDTO> UnmappedItems { get; set; } = [];

        public int TotalItemCount { get; set; }
    }
}
