namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketSellerProductListDto
    {
        public int ProductId { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string BrandOrOrigin { get; set; }
        public DateOnly? ManufacturingDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        /// <summary>0審核中/1販售中/2已售完/3未上架/4已違規</summary>
        public byte ProductStatus { get; set; }
        public List<string>? ImageUrls { get; set; }
        public int SalesLast30Days { get; set; }
    }
}
