namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class RecommendationDTO
    {
        public int FPlaceId { get; set; }

        public string FTitle { get; set; } = null!;

        public decimal? FGoogleRating { get; set; }

        public double?  FDistance { get; set; }

        public bool isRecommend { get; set; }
    }
}
