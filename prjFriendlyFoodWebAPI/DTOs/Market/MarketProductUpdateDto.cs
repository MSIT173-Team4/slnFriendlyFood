namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketProductUpdateDto
    {
        public string ProductName { get; set; }
        public string ProductsCategoryNo { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string BrandOrOrigin { get; set; }
        public string Description { get; set; }
        public DateOnly? ManufacturingDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }

        /// <summary>要刪除的圖片 id 清單</summary>
        public List<int>? DeleteImageIds { get; set; }

        /// <summary>新增的圖片檔案</summary>
        public List<IFormFile>? NewImages { get; set; }

        /// <summary>最終排序（保留的舊圖 id，依新順序排列）</summary>
        public List<int>? ImageOrder { get; set; }
        public byte? ProductStatus { get; set; }
    }
}
