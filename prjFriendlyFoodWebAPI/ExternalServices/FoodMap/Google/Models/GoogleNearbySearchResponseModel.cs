namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models
{
    public class GoogleNearbySearchResponseModel
    {
        public List<GooglePlace> Places { get; set; } = [];
    }

    public class GooglePlace
    {
        public string Id { get; set; } = string.Empty;

        public GoogleDisplayName? DisplayName { get; set; }

        public string? FormattedAddress { get; set; }

        public GoogleLocation? Location { get; set; }

        public double? Rating { get; set; }

        public int? UserRatingCount { get; set; }

        public string? BusinessStatus { get; set; }

        public string? NationalPhoneNumber { get; set; }

        public string? PrimaryType { get; set; }
    }

    public class GoogleDisplayName
    {
        public string? Text { get; set; }

        public string? LanguageCode { get; set; }
    }

    public class GoogleLocation
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
