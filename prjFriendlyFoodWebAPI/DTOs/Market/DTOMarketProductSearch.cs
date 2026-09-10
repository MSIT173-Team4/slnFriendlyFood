namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class DTOMarketProductSearch
    {
        /// <summary>
        /// 關鍵字（商品名稱模糊搜尋）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 分類編號（例如 F01、S02，不傳就不篩選）
        /// </summary>
        public string? CategoryNo { get; set; }

        /// <summary>
        /// 最低價格
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// 最高價格
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// 排序方式：price_asc / price_desc / newest
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// 頁碼，預設第 1 頁
        /// </summary>
        public int Page { get; set; } = 1;
    }
}
