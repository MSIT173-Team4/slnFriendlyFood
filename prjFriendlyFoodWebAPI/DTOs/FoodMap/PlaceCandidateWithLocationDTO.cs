namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlaceCandidateWithLocationDto
    {
        public int FPlaceId { get; set; }
        public string FName { get; set; } = string.Empty;
        public int FPlaceCategoryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // 生鮮類店家（肉舖、傳統市場…）：排行程順序時放在一般店家之後
        public bool IsFresh { get; set; }
    }
}
