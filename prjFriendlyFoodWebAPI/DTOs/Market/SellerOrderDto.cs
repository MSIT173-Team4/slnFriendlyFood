namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    // 賣家後台：一張子訂單一張卡片（出貨需要完整收件資訊）
    public class SellerOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? PaidAt { get; set; }

        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingMethod { get; set; } = string.Empty;

        public int OrderStatus { get; set; }
        public int ShippingStatus { get; set; }
        public string StatusKey { get; set; } = string.Empty;   // pending-ship / shipping / delivered / completed

        public decimal SubTotal { get; set; }
        public decimal ProductDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal OrderAmount { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class SellerOrderCountsDto
    {
        public int PendingShip { get; set; }
        public int Shipping { get; set; }
        public int Completed { get; set; }
        public int All { get; set; }
    }

    public class SellerOrderListResultDto
    {
        public List<SellerOrderDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public SellerOrderCountsDto Counts { get; set; } = new();
    }
}