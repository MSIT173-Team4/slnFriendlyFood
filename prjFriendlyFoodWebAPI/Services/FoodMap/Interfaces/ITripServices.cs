using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripServices
    {
        Task<List<TripDTO>> GetTripsAsync();

        Task<TripDTO?> GetTripByIdAsync(int tripId);

        Task<TripDTO> CreatTripAsync(TripDTO tripDTO);
    }
}
