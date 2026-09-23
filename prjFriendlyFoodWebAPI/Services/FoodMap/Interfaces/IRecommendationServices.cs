using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using System.Runtime.CompilerServices;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface IRecommendationServices
    {
        Task<List<PlaceCoverageDTO>> GetRecommendationsAsync(
            int shoppingListId,
            List<PlaceCandidateDTO> candidatePlaces,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetActiveRecommendedPlaceIdsAsync(
                   CancellationToken cancellationToken = default);
    }
}
