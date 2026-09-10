using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IFoodMapService
    {
        Task<List<PlaceDTO>> GetPlacesAsync();
        Task<PlaceDTO?> GetPlaceByIdAsync(long id);

        Task<List<PlaceDTO>>GetNearbyPlacesAsync(PlacesDTO request);

        Task<List<PlaceDTO>> GetNearbyPlacesWithFallbackAsync(decimal latitude, decimal longitude);
    }



}
