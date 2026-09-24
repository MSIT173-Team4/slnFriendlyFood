namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketProductDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? Description { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string? BrandOrOrigin { get; set; }
        public DateOnly? ManufacturingDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public byte ProductStatus { get; set; }
        public List<string> ImageUrls { get; set; } = new();

        // 評論統計
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        // 賣家資訊
        public int SellerId { get; set; }
        public string SellerName { get; set; } = "";
        public string? SellerDescription { get; set; }
        public int SellerProductCount { get; set; }
    }

    public class MarketReviewDto
    {
        public int ReviewId { get; set; }
        public string ReviewerName { get; set; } = "";  // 遮蔽格式：王＊芬
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class MarketReviewPagedDto
    {
        public List<MarketReviewDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class MarketRelatedRecipeDto
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int CookingTime { get; set; }  // 分鐘
    }
}
