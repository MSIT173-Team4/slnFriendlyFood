namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class TripRouteDTO
    {
        public int? FFromTripPlaceId { get; set; }

        public int? FToTripPlaceId { get; set; }

        public int? FDistanceMeters { get; set; }

        public int? FDurationSeconds { get; set; }

        // Google 的 Encoded Polyline 字串
        public string? FPolyline { get; set; }
    }
}
