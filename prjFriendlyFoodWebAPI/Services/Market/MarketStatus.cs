namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 商城各狀態欄位的數值定義（對照資料表欄位說明），新程式碼一律用這裡的常數
    public static class MarketStatus
    {
        // tMarketOrder.fOrderStatus
        public static class Order
        {
            public const int Pending = 0;       // 待處理
            public const int Established = 1;   // 已成立
            public const int Completed = 2;     // 已完成
            public const int Cancelled = 3;     // 已取消
        }

        // tMarketOrder.fPaymentStatus（子訂單）
        public static class Payment
        {
            public const int Unpaid = 0;
            public const int Paid = 1;
            public const int RefundPending = 2;
            public const int Refunded = 3;
        }

        // tMarketCheckoutBatch.fPaymentStatus（整筆金流）
        public static class BatchPayment
        {
            public const int Unpaid = 0;
            public const int Paid = 1;
            public const int PartialRefund = 2;
            public const int Refunded = 3;
            public const int Failed = 4;        // 逾期或買家取消
        }

        // tMarketOrder.fShippingStatus
        public static class Shipping
        {
            public const int Pending = 0;       // 待出貨
            public const int InTransit = 1;     // 運送中
            public const int Delivered = 2;     // 已送達
            public const int Failed = 3;        // 運送失敗
            public const int Returning = 4;     // 退回包裹運送中
            public const int Returned = 5;      // 賣家已取回退件
        }
    }
}