namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketSellerProductDetailDto
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
        public byte ProductStatus { get; set; }
        public string ProductsCategoryNo { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
    }
    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public short SortOrder { get; set; }
    }
}
