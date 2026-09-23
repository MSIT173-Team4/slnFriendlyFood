using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripServices
    {
        Task<TripDTO> CreateTripAsync(CreateTripRequestDTO request, CancellationToken cancellationToken = default);

        Task<TripDTO> GetTripByIdAsync(int tripId, CancellationToken cancellationToken = default);

        Task<TripDTO> FinalizeTripAsync(int tripId, GoogleTravelMode travelMode, CancellationToken cancellationToken = default);
    }
}
