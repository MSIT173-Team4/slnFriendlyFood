namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    /// <summary>
    /// GET /api/Checkout/OrderComplete/{batchId} 的回傳資料
    /// </summary>
    public class OrderCompleteDto
    {
        // ── 批次層級 ──────────────────────────────────────────
        public long BatchId { get; set; }
        public string BatchNo { get; set; } = string.Empty;          // 顯示用批次編號
        public DateTime PaidAt { get; set; }                         // 付款完成時間
        public string PaymentMethod { get; set; } = string.Empty;    // 固定：信用卡一次付清
        public int PaymentStatus { get; set; }                       // 0待付款 / 1已付款
        public decimal TotalAmount { get; set; }                     // 批次應付總額

        // ── 子訂單清單（依賣家分組） ───────────────────────────
        public List<OrderGroupDto> OrderGroups { get; set; } = new();

        // ── 收件資訊（從第一筆子訂單取，所有子訂單共用同一地址） ──
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingMethod { get; set; } = string.Empty;   // Home 宅配 / CVS 超商

        // ── 金額明細 ──────────────────────────────────────────
        public decimal SubTotal { get; set; }       // 商品原價加總（折扣前）
        public decimal DiscountAmount { get; set; } // 折扣總額
        public decimal ShippingFee { get; set; }    // 運費（目前固定 0 或 80）
    }

    public class OrderGroupDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
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