using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripPlanningService
    {
        Task<PlanTripResultDTO> PlanTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}
