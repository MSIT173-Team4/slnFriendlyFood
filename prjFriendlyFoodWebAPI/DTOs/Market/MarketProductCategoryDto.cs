namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketProductCategoryDto
    {
        public long FCategoryId { get; set; }
        public string FCategoryNo { get; set; }
        public string FCategoryName { get; set; }
        public long ParentCategoryId { get; set; }

    }
}
