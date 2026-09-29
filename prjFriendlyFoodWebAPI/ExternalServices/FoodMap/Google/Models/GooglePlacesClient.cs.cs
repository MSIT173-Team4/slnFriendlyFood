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

        // 文字搜尋不拿電話，少一個欄位可以壓低計費等級
        private const string TextSearchFieldMask =
            "places.id," +
            "places.displayName," +
            "places.formattedAddress," +
            "places.location," +
            "places.rating," +
            "places.userRatingCount," +
            "places.businessStatus," +
            "places.primaryType";

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

        public async Task<List<GooglePlace>> SearchTextAsync(
            string textQuery,
            double? latitude = null,
            double? longitude = null,
            double? radiusMeters = null,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            // 用 Dictionary 組 body：沒有座標時整個 locationBias 就不送，
            // 避免送出 "locationBias": null 給 Google
            var body = new Dictionary<string, object>
            {
                ["textQuery"] = textQuery,
                ["languageCode"] = "zh-TW",
                ["regionCode"] = "TW",
                ["pageSize"] = Math.Clamp(pageSize, 1, 20)
            };

            if (latitude.HasValue && longitude.HasValue)
            {
                body["locationBias"] = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = latitude.Value,
                            longitude = longitude.Value
                        },
                        radius = Math.Clamp(radiusMeters ?? 5000, 1, 50000)
                    }
                };
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post, "/v1/places:searchText");

            httpRequest.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
            httpRequest.Headers.Add("X-Goog-FieldMask", TextSearchFieldMask);
            httpRequest.Content = JsonContent.Create(body);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            response.EnsureSuccessStatusCode();

            // Text Search 回傳格式跟 Nearby Search 一樣是 { "places": [...] }
            var result = await response.Content
                .ReadFromJsonAsync<GoogleNearbySearchResponseModel>(cancellationToken: cancellationToken);

            return result?.Places ?? [];
        }
    }
}
