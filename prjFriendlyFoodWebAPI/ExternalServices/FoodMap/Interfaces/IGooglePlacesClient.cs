using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google;

namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Interfaces
{
    public interface IGooglePlacesClient
    {
        Task<List<GooglePlacesClient>> SearchNearbyAsync(
            decimal latitude,
            decimal longitude,
            double radius,
            string? googleplaceType = null,
            CancellationToken cancellationToken = default
            );
        

    }
}
