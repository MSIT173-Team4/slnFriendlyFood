using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using static prjFriendlyFoodWebAPI.DTOs.Market.CheckoutDto;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        public CheckoutController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto requset)
        {
            int userId = 1;
            if (requset.CartItemIds == null || !requset.CartItemIds.Any())
                return BadRequest("請選擇至少一件商品");

            var cartItems = await _context.TMarketShoppingCarts
                .Include(c => c.FProduct)
                .Where(c => requset.CartItemIds.Contains(c.FCartItemId)
                && c.FUserId == userId)
                .ToListAsync();

            if (cartItems.Count != requset.CartItemIds.Count)
                return BadRequest("部分購物車項目不存在或不屬於此帳號");

            foreach (var item in cartItems)
            {
                if (item.FProduct.FStock < item.FQuantity)
                    return BadRequest($"商品「{item.FProduct.FProductName}」庫存不足");
            }

            var groupedBySeller = cartItems
                .GroupBy(c => c.FSellerId)
                .ToList();

            decimal totalAmount = cartItems.Sum(c => c.FProduct.FPrice * c.FQuantity);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var batch = new TMarketCheckoutBatch
                {
                    FBatchNo = GenerateBatchNo(),
                    FUserId = userId,
                    FTotalAmount = totalAmount,
                    FPaymentStatus = 0,
                    FPaymentMethod = "Credit",
                    FCreatedDate = DateTime.Now,
                };

                _context.TMarketCheckoutBatches.Add(batch);
                await _context.SaveChangesAsync();

                var subOrders = new List<TMarketOrder>();

                foreach (var sellerGroup in groupedBySeller)
                {
                    decimal orderAmount = sellerGroup.Sum(c => c.FProduct.FPrice * c.FQuantity);

                    var order = new TMarketOrder
                    {
                        FBatchId = batch.FBatchId,
                        FOrderNo = GenerateOrderNo(),
                        FUserId = userId,
                        FSellerId = sellerGroup.Key,
                        FTotalAmount = orderAmount,
                        FOrderDate = DateTime.Now,
                        FShippingMethod = "Home",
                        FShippingFee = 0,
                        FShippingDiscount = 0,
                        FProductDiscount = 0,
                        FRecipientName = "測試收件人",
                        FRecipientPhone = "0912345678",
                        FShippingAddress = "台北市測試地址",
                        FOrderStatus = 0,
                        FPaymentStatus = 0,
                        FShippingStatus = 0,
                        FIsShippingConfirmed = false,
                        FCancellationStatus = 0,
                        FReturnStatus = 0,
                    };

                    _context.TMarketOrders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in sellerGroup)
                    {
                        var detail = new TMarketOrderDetail
                        {
                            FOrderId = order.FOrderId,
                            FProductId = item.FProductId,
                            FQuantity = item.FQuantity,
                            FUnitPrice = item.FProduct.FPrice,
                        };
                        _context.TMarketOrderDetails.Add(detail);
                    }
                    await _context.SaveChangesAsync();

                    subOrders.Add(order);
                }

                await transaction.CommitAsync();

                var response = new CreateOrderResponseDto
                {
                    BatchId = batch.FBatchId,
                    BathNo = batch.FBatchNo,
                    TotalAmount = totalAmount,
                    Orders = subOrders.Select(o => new SubOrderDto
                    {
                        OrderId = o.FOrderId,
                        OrderNo = o.FOrderNo,
                        SellerId = o.FSellerId,
                        OrderAmount = o.FTotalAmount,
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"建立訂單失敗:{ex.Message}");
            }
        }
        private string GenerateBatchNo()
        {
            return "B" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                   new Random().Next(1000, 9999).ToString();
        }

        private string GenerateOrderNo()
        {
            return "O" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                   new Random().Next(1000, 9999).ToString();
        }
    }
}
