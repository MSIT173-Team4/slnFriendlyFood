namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class NearbyRequestDTO
    {
        public double FLatitude { get; set; }
        public double FLongitude { get; set; }
        public int? FPlacesCategoryId { get; set; }
        public int  MinimumRequests { get; set; } = 5;



    }
}
