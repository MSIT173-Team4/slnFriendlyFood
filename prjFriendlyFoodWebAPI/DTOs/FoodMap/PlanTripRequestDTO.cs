using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlanTripRequestDTO
    {
        public required int ShoppingListId { get; set; }
        public required decimal OriginLatitude { get; set; }
        public required decimal OriginLongitude { get; set; }
        public GoogleTravelMode TravelMode { get; set; } = GoogleTravelMode.Drive;

        // 搜尋附近店家的半徑，預設 3 公里，可依實際情況調整上限
        public int SearchRadiusMeters { get; set; } = 3000;
    }
}
