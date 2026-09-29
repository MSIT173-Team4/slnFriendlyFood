using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    // 在 Google Places 呼叫外面包一層快取，減少重複呼叫的費用。
    internal static class GooglePlacesClientCacheExtensions
    {
        private static readonly TimeSpan NearbyTimeToLive = TimeSpan.FromMinutes(15);

        // Nearby Search：座標四捨五入到小數 3 位（約 100 公尺網格），
        // 搜尋也用四捨五入後的座標，讓「差不多位置」的請求共用同一份結果。
        public static async Task<List<GooglePlace>> SearchNearbyCachedAsync(
            this IGooglePlacesClient client,
            double latitude,
            double longitude,
            double radiusMeters,
            string? googlePlaceType,
            CancellationToken cancellationToken = default)
        {
            var gridLatitude = Math.Round(latitude, 3);
            var gridLongitude = Math.Round(longitude, 3);
            var radius = Math.Round(radiusMeters);

            var cacheKey = FormattableString.Invariant(
                $"foodmap:nearby:{gridLatitude}:{gridLongitude}:{radius}:{googlePlaceType ?? "*"}");

            if (FoodMapMemoryCache.TryGet<List<GooglePlace>>(cacheKey, out var cached))
            {
                return cached;
            }

            var result = await client.SearchNearbyAsync(
                gridLatitude, gridLongitude, radius, googlePlaceType, cancellationToken);

            FoodMapMemoryCache.Set(cacheKey, result, NearbyTimeToLive);
            return result;
        }

        // Text Search：同樣的關鍵字 + 大致相同的位置，在 timeToLive 內直接吃快取。
        public static async Task<List<GooglePlace>> SearchTextCachedAsync(
            this IGooglePlacesClient client,
            string textQuery,
            double? latitude,
            double? longitude,
            double? radiusMeters,
            int pageSize,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
        {
            var normalizedQuery = textQuery.Trim();
            double? gridLatitude = latitude.HasValue ? Math.Round(latitude.Value, 2) : null;
            double? gridLongitude = longitude.HasValue ? Math.Round(longitude.Value, 2) : null;

            var cacheKey = FormattableString.Invariant(
                $"foodmap:text:{normalizedQuery}:{gridLatitude}:{gridLongitude}:{radiusMeters}:{pageSize}");

            if (FoodMapMemoryCache.TryGet<List<GooglePlace>>(cacheKey, out var cached))
            {
                return cached;
            }

            var result = await client.SearchTextAsync(
                normalizedQuery, gridLatitude, gridLongitude, radiusMeters, pageSize, cancellationToken);

            FoodMapMemoryCache.Set(cacheKey, result, timeToLive);
            return result;
        }
    }
}
