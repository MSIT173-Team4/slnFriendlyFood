using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripServices
    {
        // 目前使用者自己的行程（新的在前）
        Task<List<TripDTO>> GetTripsAsync(int userId, CancellationToken cancellationToken = default);

        // userId 有帶時只回傳屬於這個使用者的行程；找不到回 null
        Task<TripDTO?> GetTripByIdAsync(int tripId, int? userId = null, CancellationToken cancellationToken = default);

        Task<TripDTO> CreateTripAsync(
            CreateTripRequestDTO request,
            int? userId = null,
            string? description = null,
            CancellationToken cancellationToken = default);

        Task<TripDTO> FinalizeTripAsync(int tripId, GoogleTravelMode travelMode, CancellationToken cancellationToken = default);
    }
}
