namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryNo { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int? ParentCategoryId { get; set; }
        public int ProductCount { get; set; }
        public List<MarketCategoryDto> Children { get; set; } = new();

    }
}
