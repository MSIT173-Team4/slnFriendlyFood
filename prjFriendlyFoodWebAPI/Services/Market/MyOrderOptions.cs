namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 我的訂單 API 可接受的參數值；新增分頁時，這裡與 OrderQueryService.TabFilter 要一起改
    public static class MyOrderOptions
    {
        public static readonly IReadOnlySet<string> Tabs = new HashSet<string>
        {
            "all", "pending-payment", "pending-ship", "to-receive",
            "completed", "to-review", "cancelled"
        };

        public static readonly IReadOnlySet<string> Ranges = new HashSet<string>
        {
            "6m", "1y", "all"
        };
    }
}