namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models
{
    public class GoogleNearbySearchRequestModel
    {
        public string[]? IncludedTypes { get; set; }

        public int MaxResultCount { get; set; } = 20;

        public GoogleLocationRestriction LocationRestriction { get; set; }
            = new();

        public string RankPreference { get; set; } = "DISTANCE";

        public string LanguageCode { get; set; } = "zh-TW";

        public string RegionCode { get; set; } = "TW";
    }

    public class GoogleLocationRestriction
    {
        public GoogleCircle Circle { get; set; } = new();
    }

    public class GoogleCircle
    {
        public GoogleCenter Center { get; set; } = new();

        public double Radius { get; set; }
    }

    public class GoogleCenter
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
