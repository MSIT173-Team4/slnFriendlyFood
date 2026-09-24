using Polly;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Exceptions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;

namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models
{
    public class GoogleRoutesClient : IGoogleRoutesClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleRoutesClient> _logger;
        private readonly string _apiKey;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

        private const string Endpoint = "https://routes.googleapis.com/directions/v2:computeRoutes";

        private const string FieldMask =
            "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline," +
            "routes.legs.distanceMeters,routes.legs.duration,routes.legs.polyline.encodedPolyline";

        public GoogleRoutesClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleRoutesClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // 比照 GooglePlacesClient：Key 從 User Secrets 讀取，不寫進 appsettings.json
            _apiKey = configuration["GoogleMaps:ApiKey"]
                ?? throw new InvalidOperationException("GoogleMaps:ApiKey 未設定，請用 User Secrets 設定");

            // Retry：遇到 5xx 或連線例外，重試 3 次，指數退避（2s, 4s, 8s）
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

            // Circuit Breaker：連續失敗 5 次後斷開 30 秒，避免一直打已經掛掉的服務、浪費額度
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            _resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        public async Task<GoogleRouteResult> ComputeRouteAsync(
            List<(double Latitude, double Longitude)> waypoints,
            GoogleTravelMode travelMode = GoogleTravelMode.Drive,
            CancellationToken cancellationToken = default)
        {
            if (waypoints.Count < 2)
            {
                // 少於 2 個點，不需要規劃路線
                return null;
            }

            var origin = waypoints[0];
            var destination = waypoints[^1];
            var intermediates = waypoints
                .Skip(1)
                .Take(waypoints.Count - 2)
                .ToList();

            // routingPreference（即時路況）只有 DRIVE 支援，
            // 所以非 DRIVE 的情況下 requestBody 不能帶這個欄位（這裡本來就沒有帶，維持原樣即可）
            var requestBody = new
            {
                origin = BuildWaypoint(origin),
                destination = BuildWaypoint(destination),
                intermediates = intermediates.Select(BuildWaypoint).ToList(),
                travelMode = ToGoogleTravelModeString(travelMode)
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
                {
                    Content = JsonContent.Create(requestBody)
                };
                request.Headers.Add("X-Goog-Api-Key", _apiKey);
                request.Headers.Add("X-Goog-FieldMask", FieldMask);

                var response = await _resiliencePolicy.ExecuteAsync(
                    ct => _httpClient.SendAsync(request, ct),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Google Routes API 回傳非成功狀態碼 {StatusCode}: {Body}",
                        response.StatusCode, errorBody);
                    throw new GoogleRoutesUnavailableException(
                        $"Google Routes API 回傳 {response.StatusCode}");
                }

                var payload = await response.Content
                    .ReadFromJsonAsync<GoogleRoutesApiResponse>(cancellationToken: cancellationToken);

                return MapToRouteResult(payload);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "呼叫 Google Routes API 發生連線錯誤");
                throw new GoogleRoutesUnavailableException("無法連線到 Google Routes API", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "呼叫 Google Routes API 逾時");
                throw new GoogleRoutesUnavailableException("Google Routes API 呼叫逾時", ex);
            }
        }

        // enum 轉成 Google Routes API 要求的大寫底線格式字串
        // 注意：TWO_WHEELER 目前只在部分地區（如印度、部分東南亞國家）支援，
        // 用在台灣等未支援地區時請自行實測 Google 是否正常回傳結果
        private static string ToGoogleTravelModeString(GoogleTravelMode travelMode) => travelMode switch
        {
            GoogleTravelMode.Drive => "DRIVE",
            GoogleTravelMode.Walk => "WALK",
            GoogleTravelMode.Bicycle => "BICYCLE",
            GoogleTravelMode.Two_Wheeler => "TWO_WHEELER",
            GoogleTravelMode.Transit => "TRANSIT",
            _ => throw new ArgumentOutOfRangeException(nameof(travelMode), travelMode, "未支援的交通方式")
        };

        private static object BuildWaypoint((double Latitude, double Longitude) point) => new
        {
            location = new
            {
                latLng = new
                {
                    latitude = point.Latitude,
                    longitude = point.Longitude
                }
            }
        };

        private static GoogleRouteResult? MapToRouteResult(GoogleRoutesApiResponse? payload)
        {
            var route = payload?.Routes?.FirstOrDefault();
            if (route is null)
            {
                return null;
            }

            return new GoogleRouteResult
            {
                TotalDistanceMeters = route.DistanceMeters,
                TotalDurationSeconds = ParseDurationSeconds(route.Duration),
                EncodedPolyline = route.Polyline?.EncodedPolyline ?? string.Empty,
                Legs = route.Legs?.Select(leg => new GoogleRouteLeg
                {
                    DistanceMeters = leg.DistanceMeters,
                    DurationSeconds = ParseDurationSeconds(leg.Duration),
                    EncodedPolyline = leg.Polyline?.EncodedPolyline ?? string.Empty
                }).ToList() ?? []
            };
        }

        // Google Routes API 的 duration 是 "1234s" 這種字串格式，去掉結尾的 s 轉成整數秒
        private static int ParseDurationSeconds(string? duration)
        {
            if (string.IsNullOrEmpty(duration))
            {
                return 0;
            }

            var numericPart = duration.TrimEnd('s');
            return int.TryParse(numericPart, out var seconds) ? seconds : 0;
        }

        // ---------- 對應 Google Routes API 回傳的 JSON 結構 ----------

        private class GoogleRoutesApiResponse
        {
            public List<GoogleRoutesApiRoute>? Routes { get; set; }
        }

        private class GoogleRoutesApiRoute
        {
            public int DistanceMeters { get; set; }
            public string? Duration { get; set; }
            public GoogleRoutesApiPolyline? Polyline { get; set; }
            public List<GoogleRoutesApiLeg>? Legs { get; set; }
        }

        private class GoogleRoutesApiLeg
        {
            public int DistanceMeters { get; set; }
            public string? Duration { get; set; }
            public GoogleRoutesApiPolyline? Polyline { get; set; }
        }

        private class GoogleRoutesApiPolyline
        {
            public string? EncodedPolyline { get; set; }
        }
    }

}

