namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketProductPagedResultDto
    {
        public List<MarketPublicProductListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
