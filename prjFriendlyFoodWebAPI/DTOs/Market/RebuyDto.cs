namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class RebuyRequestDto
    {
        public List<long> OrderIds { get; set; } = new();
    }

    public class RebuySkippedItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RebuyResultDto
    {
        public int AddedCount { get; set; }
        public List<RebuySkippedItemDto> Skipped { get; set; } = new();
    }
}