using prjFriendlyFoodWebAPI.DTOs.FoodMap;

namespace prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces
{
    public interface ITripOptimizationService
    {
        Task<OptimizedTripResult> OptimizeTripAsync(
            int shoppingListId,
            decimal originLatitude,
            decimal originLongitude,
            CancellationToken cancellationToken = default);
    }

    public class OptimizedTripResult
    {
        public List<PlaceCoverageDTO> PlaceCoverages { get; set; } = new List<PlaceCoverageDTO>();
        public double FinalCoveragePercentage { get; set; }
        public List<string> UncoveredItemNames { get; set; } = [];
    }
}
