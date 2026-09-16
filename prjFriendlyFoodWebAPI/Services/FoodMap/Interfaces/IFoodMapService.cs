using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IFoodMapService
    {
        Task<List<PlaceDTO>> GetPlacesAsync();
        Task<PlaceDTO?> GetPlaceByIdAsync(long id);

        Task<NearbyResponseDTO> GetNearbyPlacesAsync(NearbyRequestDTO request, CancellationToken cancellationToken = default);

    }



}
