namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class TripDTO
    {
        public int FTripId { get; set; }

        public string FTripName { get; set; } = null!;

        public string? FStatus { get; set; }

        public string? FDescription { get; set; }

        public DateTime FCreatedTime { get; set; }

        public List<TripPlaceDTO> Places { get; set; } = [];

        // 逐段路線（Google Routes 算出來的），前端用 FPolyline 在地圖上畫線
        public List<TripRouteDTO> Routes { get; set; } = [];

        public int? TotalDistanceMeters { get; set; }

        public int? TotalDurationSeconds { get; set; }
    }
}
