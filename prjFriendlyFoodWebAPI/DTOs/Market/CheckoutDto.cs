namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class CheckoutDto
    {
        public class CreateOrderRequestDto
        {
            public List<int> CartItemIds { get; set; } = new();
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
