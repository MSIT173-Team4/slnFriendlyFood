namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlaceDTO
    {
        public int FPlaceId { get; set; }

        public required string FName { get; set; }
        public required string FAddress { get; set; }

        public decimal FLatitude { get; set; }
        public decimal FLongitude { get; set; }

        public string? FPhone { get; set; }
        public string? FDescription { get; set; }

        public int? FGoogleReviewCount { get; set; }

        public decimal? FGoogleRating { get; set; }

        public bool? FIsRecommend { get; set; }

    }
}
