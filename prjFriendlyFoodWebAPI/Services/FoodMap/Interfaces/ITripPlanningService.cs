using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripPlanningService
    {
        // trip-builder 進頁面時的初始資料：要規劃哪一份採買清單、會員地址的座標
        Task<PlanningContextDTO> GetPlanningContextAsync(
            int userId,
            int? shoppingListId,
            CancellationToken cancellationToken = default);

        // 第一段：只計算（找附近店家 → 最佳化 → 推薦標記），不寫 tFoodMapTrip
        Task<PlanTripPreviewResultDTO> PreviewTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default);

        // 第二段：使用者選好店家、排好順序、取好名字，按下確認才建立行程並計算路線
        Task<PlanTripResultDTO> ConfirmTripAsync(
            int userId,
            ConfirmTripRequestDTO request,
            CancellationToken cancellationToken = default);

        // 舊版相容：一次做完（預覽 → 直接用建議的店家確認）
        Task<PlanTripResultDTO> PlanTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}
