namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    // 我的訂單：一張子訂單（一個賣家）一張卡片
    public class MyOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public long BatchId { get; set; }           // 「查看明細」連到完成頁用
        public DateTime OrderDate { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;

        // 原始狀態值 + 後端統一判斷好的狀態代碼（前端只負責把代碼轉成文字）
        public int OrderStatus { get; set; }
        public int PaymentStatus { get; set; }
        public int ShippingStatus { get; set; }
        public string StatusKey { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal ProductDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal OrderAmount { get; set; }

        public bool CanReview { get; set; }        // 已完成且還有未評價的明細

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class MyOrderCountsDto
    {
        public int PendingPayment { get; set; }
        public int PendingShip { get; set; }
        public int ToReceive { get; set; }
        public int Completed { get; set; }
        public int ToReview { get; set; }
        public int Cancelled { get; set; }
        public int All { get; set; }
    }

    public class MyOrderListResultDto
    {
        public List<MyOrderDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public MyOrderCountsDto Counts { get; set; } = new();
    }
}