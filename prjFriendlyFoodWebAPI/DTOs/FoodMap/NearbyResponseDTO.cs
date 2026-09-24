namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class NearbyResponseDTO
    {
        public double FLatitude { get; set; }

        public double FLongitude { get; set; }

        public double SearchRadiusKm { get; set; }

        public bool ExpandedSearch { get; set; }

        public int ResultCount { get; set; }

        public List<PlaceDTO> Places { get; set; } = [];
    }
}
