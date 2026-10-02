namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    /// <summary>
    /// GET /api/Checkout/OrderComplete/{batchId} 的回傳資料
    /// </summary>
    public class OrderCompleteDto
    {
        public long BatchId { get; set; }
        public string BatchNo { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int PaymentStatus { get; set; }      // 批次（整筆金流）的付款狀態

        // 結帳金額總覽：所有子訂單加總
        public decimal SubTotal { get; set; }          // 商品原價小計
        public decimal ProductDiscount { get; set; }   // 優惠折抵（商品）
        public decimal ShippingFee { get; set; }       // 運費（折抵前）
        public decimal ShippingDiscount { get; set; }  // 運費折抵
        public decimal TotalAmount { get; set; }       // 實付 = 批次 FTotalAmount

        public List<OrderGroupDto> OrderGroups { get; set; } = new();
    }

    public class OrderGroupDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;

        // 收件配送資訊（每個賣家可能不同）
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingMethod { get; set; } = string.Empty;

        // 付款資訊（這張子訂單自己的）
        public int PaymentStatus { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ProductDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal OrderAmount { get; set; }       // = 子訂單 FTotalAmount
        public int OrderStatus { get; set; }   // 子訂單狀態；3 = 已取消

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }   // UnitPrice * Quantity
    }
}