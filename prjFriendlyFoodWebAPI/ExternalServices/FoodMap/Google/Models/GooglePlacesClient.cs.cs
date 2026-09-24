using Microsoft.Extensions.Options;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;

namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models
{
    public class GooglePlacesClient : IGooglePlacesClient
    {
        private readonly HttpClient _httpClient;
        private readonly GooglePlacesOptions _options;

        private const string NearbyFieldMask =
            "places.id," +
            "places.displayName," +
            "places.formattedAddress," +
            "places.location," +
            "places.rating," +
            "places.userRatingCount," +
            "places.businessStatus," +
            "places.nationalPhoneNumber," +
            "places.primaryType";

        private const string DetailsFieldMask =
            "id," +
            "displayName," +
            "formattedAddress," +
            "location," +
            "rating," +
            "userRatingCount," +
            "businessStatus," +
            "nationalPhoneNumber," +
            "primaryType";

        public GooglePlacesClient(
            HttpClient httpClient,
            IOptions<GooglePlacesOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<List<GooglePlace>>
            SearchNearbyAsync(
                double latitude,
                double longitude,
                double radiusMeters,
                string? googlePlaceType,
                CancellationToken cancellationToken = default)
        {
            var request = new GoogleNearbySearchRequestModel
            {
                MaxResultCount = 20,

                RankPreference = "DISTANCE",

                LanguageCode = "zh-TW",

                RegionCode = "TW",

                LocationRestriction = new GoogleLocationRestriction
                {
                    Circle = new GoogleCircle
                    {
                        Center = new GoogleCenter
                        {
                            Latitude = latitude,
                            Longitude = longitude
                        },
                        Radius = radiusMeters
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(
                    googlePlaceType))
            {
                request.IncludedTypes =
                    [googlePlaceType];
            }

            using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post, "/v1/places:searchNearby");

            httpRequest.Headers.Add(
                "X-Goog-Api-Key",
                _options.ApiKey);

            httpRequest.Headers.Add(
                "X-Goog-FieldMask",
                NearbyFieldMask);

            httpRequest.Content =
                JsonContent.Create(request);

            using var response =
                await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<GoogleNearbySearchResponseModel>(cancellationToken: cancellationToken);

            return result?.Places ?? [];
        }

        public async Task<GooglePlace?>
            GetPlaceDetailsAsync(
                string googlePlaceId,
                CancellationToken cancellationToken = default)
        {
            var encodedId =
                Uri.EscapeDataString(
                    googlePlaceId);

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/v1/places/{encodedId}");

            httpRequest.Headers.Add(
                "X-Goog-Api-Key",
                _options.ApiKey);

            httpRequest.Headers.Add(
                "X-Goog-FieldMask",
                DetailsFieldMask);

            using var response =
                await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<GooglePlace>(
                    cancellationToken:
                        cancellationToken);
        }
    }
}

