namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlaceCandidateWithLocationDto
    {
        public int FPlaceId { get; set; }
        public string FName { get; set; } = string.Empty;
        public int FPlaceCategoryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
