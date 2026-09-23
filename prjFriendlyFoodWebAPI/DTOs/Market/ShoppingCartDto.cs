namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    // GET /api/ShoppingCart 回傳格式
    public class CartSellerGroupDto
    {
        public int SellerId { get; set; }
        public string SellerName { get; set; } = "";
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    // PUT /api/ShoppingCart/{cartItemId}
    public class UpdateCartItemDto
    {
        public int Quantity { get; set; }
    }

    // POST /api/MarketCoupon/validate
    public class ValidateCouponDto
    {
        public string Code { get; set; } = "";
        public decimal OrderAmount { get; set; }
        public int? SellerId { get; set; }  // Store 券需要，Platform/Shipping 不需要
    }

    public class ValidateCouponResultDto
    {
        public int CouponId { get; set; }
        public string CouponName { get; set; } = "";
        public string ScopeType { get; set; } = "";    // Shipping/Platform/Store
        public string DiscountType { get; set; } = ""; // Fixed/Percentage
        public decimal DiscountValue { get; set; }
        public decimal AppliedAmount { get; set; }     // 實際折抵金額
        public string Message { get; set; } = "";
    }
}