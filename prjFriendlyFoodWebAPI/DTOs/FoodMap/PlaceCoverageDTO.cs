namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlaceCoverageDTO
    {
        public int FPlaceId { get; set; }
        public string FName { get; set; } = string.Empty;
        public double CoveragePercentage { get; set; }
        public List<string> MatchedItemNames { get; set; } = [];
        public bool IsRecommend { get; set; }
    }
}
