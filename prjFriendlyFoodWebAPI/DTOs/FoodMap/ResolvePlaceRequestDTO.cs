namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class ResolvePlaceRequestDTO
    {
        public required string FGooglePlaceId { get; set; }
        public int FPlaceCategoryId { get; set; }
    }
}
