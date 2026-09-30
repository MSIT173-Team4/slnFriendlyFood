namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class CheckoutDto
    {
        public class CreateOrderRequestDto
        {
            public List<int> CartItemIds { get; set; } = new();
            public List<SellerShippingDto> SellerShippings { get; set; } = new();
            public List<SellerCouponDto> SellerCoupons { get; set; } = new();
            public int? PlatformCouponId { get; set; }
        }

        public class SellerCouponDto
        {
            public int SellerId { get; set; }
            public int CouponId { get; set; }
        }

        // 每個賣家各自的收件資料（前端已把「套用全域預設 / 個別指定」解析好再送來）
        public class SellerShippingDto
        {
            public int SellerId { get; set; }
            public string RecipientName { get; set; } = string.Empty;
            public string RecipientPhone { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
        }

        public class CreateOrderResponseDto
        {
            public long BatchId { get; set; }
            public string BathNo { get; set; }
            public decimal TotalAmount { get; set; }
            public List<SubOrderDto> Orders { get; set; } = new();
        }

        public class SubOrderDto
        {
            public long OrderId { get; set; }
            public string OrderNo { get; set; }
            public int SellerId { get; set; }
            public decimal OrderAmount { get; set; }
        }
    }

    
}
