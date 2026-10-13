namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    /*貼文列表的分頁結果，
     * 需要貼文總數、頁碼、
     * 每頁顯示數量和總頁數等資訊*/
    public class PagedResultDto<T>
    {
        //當前頁面有哪些貼文
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        //計算總頁數，使用Math.Ceiling向上取整，確保有餘數時也能顯示最後一頁
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
