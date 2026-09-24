namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models
{
    public class GooglePlacesOptions
    {
        public string ApiKey { get; set; } = string.Empty;

        public string PlacesBaseUrl { get; set; }
            = "https://places.googleapis.com";
    }
}
