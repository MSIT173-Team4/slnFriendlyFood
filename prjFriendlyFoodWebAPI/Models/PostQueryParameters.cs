namespace prjFriendlyFoodWebAPI.Models
{
    //頁籤(預設最新貼文)、關鍵字搜尋、分頁(預設第一頁，一頁五篇貼文)
    public class PostQueryParameters
    {
        public string Tab { get; set; } = "latest";
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
