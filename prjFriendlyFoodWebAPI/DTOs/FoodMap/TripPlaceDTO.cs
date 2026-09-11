namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class TripPlaceDTO
    {
        public int FTripPlaceId { get; set; }

        public string FPlaceName { get; set; } = null!;

        public string FAddress { get; set; } = null!;
        public decimal FLatitude { get; set; }
        public decimal Flongitude { get; set; }

        public int FSortOrder { get; set; }
        public int FPlaceId { get; internal set; }
    }
}
