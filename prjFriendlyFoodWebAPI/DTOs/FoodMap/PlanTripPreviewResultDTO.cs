using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // POST /api/trips/plan/preview 的回傳：只有計算結果，還沒有寫進 tFoodMapTrip
    public class PlanTripPreviewResultDTO
    {
        public int ShoppingListId { get; set; }
        public string ListName { get; set; } = string.Empty;

        public decimal OriginLatitude { get; set; }
        public decimal OriginLongitude { get; set; }
        public GoogleTravelMode TravelMode { get; set; }
        public int SearchRadiusMeters { get; set; }

        // 清單中「還沒買」的品項（覆蓋率的分母）
        public int TotalItemCount { get; set; }
        public List<ShoppingListItemDTO> Items { get; set; } = [];

        // 附近所有候選店家（地圖標記），建議的店家排在最前面、依造訪順序
        public List<TripCandidateDTO> Candidates { get; set; } = [];

        public double FinalCoveragePercentage { get; set; }
        public List<string> UncoveredItemNames { get; set; } = [];
    }
}
