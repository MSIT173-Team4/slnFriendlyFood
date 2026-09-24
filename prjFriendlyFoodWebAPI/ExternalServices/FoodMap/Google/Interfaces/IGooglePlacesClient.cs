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
    }
}
