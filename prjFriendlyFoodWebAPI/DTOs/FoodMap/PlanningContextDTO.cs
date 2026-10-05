namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // GET /api/trips/planning-context：trip-builder 一進頁面要的初始資料
    public class PlanningContextDTO
    {
        public int? ShoppingListId { get; set; }
        public string? ListName { get; set; }
        public int PendingItemCount { get; set; }

        // 會員資料裡的地址轉成的座標；沒有地址或轉換失敗為 null
        public LocationDTO? MemberLocation { get; set; }
    }

    public class LocationDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
    }
}
