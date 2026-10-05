namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 對應 appsettings 的 "Market" 區塊
    public class MarketOptions
    {
        public int PaymentTimeoutMinutes { get; set; } = 2;
        public bool EnableExpiryWorker { get; set; } = false;

        // 某個批次的付款期限（P-3 的 Pay 檢查、P-4 的畫面顯示也會用到）
        public DateTime PaymentDeadlineOf(DateTime createdDate) =>
            createdDate.AddMinutes(PaymentTimeoutMinutes);
    }
}