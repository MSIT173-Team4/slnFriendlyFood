using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IFoodMapService
    {
        Task<List<PlaceDTO>> GetPlacesAsync();
        Task<PlaceDTO?> GetPlaceByIdAsync(long id);

        Task<NearbyResponseDTO> GetNearbyPlacesAsync(NearbyRequestDTO request, CancellationToken cancellationToken = default);

        Task<int> ResolvePlaceAsync(ResolvePlaceRequestDTO request, CancellationToken cancellationToken = default);

        // 解析 Google 地點並回傳完整店家資料（地圖上點選 POI、搜尋結果加入行程時用）
        Task<PlaceDTO> ResolvePlaceDetailAsync(ResolvePlaceRequestDTO request, CancellationToken cancellationToken = default);

        // 文字搜尋地點（店名、地名）
        Task<List<PlaceDTO>> SearchPlacesAsync(PlaceSearchRequestDTO request, CancellationToken cancellationToken = default);

        // 把 Nearby Search 拿到的 Google 地點批次寫入/更新 tFoodMapPlace，回傳 GooglePlaceId → FPlaceId。
        // 不會再打 Place Details（Nearby Search 已經帶回一樣的欄位），
        // 而且本地資料 30 天內同步過的直接沿用，不寫 DB。
        Task<Dictionary<string, int>> UpsertGooglePlacesAsync(
            IReadOnlyCollection<(GooglePlace Place, int PlaceCategoryId)> places,
            CancellationToken cancellationToken = default);
    }
}
