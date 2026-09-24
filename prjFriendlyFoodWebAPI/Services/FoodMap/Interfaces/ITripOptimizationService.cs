using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripOptimizationService
    {
        Task<OptimizedTripResult> OptimizeAsync(
            int shoppingListId,
            decimal originLatitude,
            decimal originLongitude,
            List<PlaceCandidateWithLocationDto> candidatePlaces,
            CancellationToken cancellationToken = default);
    }

    public class OptimizedTripResult
    {
        public List<PlaceCoverageDTO> PlaceCoverages { get; set; } = new List<PlaceCoverageDTO>();
        public double FinalCoveragePercentage { get; set; }
        public List<string> UncoveredItemNames { get; set; } = [];
    }

   
}
