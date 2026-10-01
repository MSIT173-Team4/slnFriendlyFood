using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces
{
    public interface IGooglePlacesClient
    {
        Task<List<GooglePlace>> SearchNearbyAsync(
            double latitude,
            double longitude,
            double radius,
            string? googleplaceType = null,
            CancellationToken cancellationToken = default
            );

        Task<GooglePlace?> GetPlaceDetailsAsync(
            string googlePlaceId,
            CancellationToken cancellationToken = default
            );

        // 用文字搜尋地點（店名、地名、地址）。有帶座標時會優先回傳附近的結果。
        // 也拿來把會員地址轉成座標（取第一筆結果的 location）。
        Task<List<GooglePlace>> SearchTextAsync(
            string textQuery,
            double? latitude = null,
            double? longitude = null,
            double? radiusMeters = null,
            int pageSize = 10,
            CancellationToken cancellationToken = default
            );
    }
}
