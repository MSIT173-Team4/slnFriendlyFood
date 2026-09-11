namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class TripDTO
    {
        public int FTripId { get; set; }

        public string FTripName { get; set; } = null!;

        public List<TripPlaceDTO> Places { get; set; } = [];
    }
}
