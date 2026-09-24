namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class PlanTripResultDTO
    {
        public TripDTO Trip { get; set; } = null!; 
        public double FinalCoveragePercentage { get; set; }
        public List<string> UncoveredItemNames { get; set; } = [];
    }
}
