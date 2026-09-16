using Microsoft.AspNetCore.Authorization;
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
        private readonly IConfiguration _config;
        public CheckoutController(FriendlyFoodDbContext context,IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("Pay/{batchId}")]
        public async Task<IActionResult> Pay(long batchId)
        {
            var batch = await _context.TMarketCheckoutBatches
                .FirstOrDefaultAsync(b => b.FBatchId == batchId);
            if (batch == null)
                return NotFound("找不到此結帳批次");

            if (batch.FPaymentStatus != 0)
                return BadRequest("此批次已付款或已取消，無法重複付款");

            var merchantId = _config["ECPay:MerchantID"];
            var hashKey = _config["ECPay:HashKey"];
            var hashIV = _config["ECPay:HashIV"];
            var returnUrl = _config["ECPay:ReturnURL"];
            var orderResultUrl = _config["ECPay:OrderResultURL"];

            var param = new Dictionary<string, string>
            {
                { "MerchantID", merchantId },
                { "MerchantTradeNo", batch.FBatchNo },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", ((int)batch.FTotalAmount).ToString() },
                { "TradeDesc", "FriendlyFood商城結帳" },
                { "ItemName", "商城訂單" },
                { "ReturnURL", returnUrl },
                { "OrderResultURL", orderResultUrl },
                { "ChoosePayment", "Credit" },
                { "EncryptType", "1" },
            };

            var checkMacValue = GetCheckMacValue(param, hashKey, hashIV);
            param.Add("CheckMacValue", checkMacValue);

            var html = BuildECPayForm(param);

            return Content(html, "text/html");
        }

        private string GetCheckMacValue(Dictionary<string, string>param, string hashKey, string hashIV)
        {
            var sorted = param.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}");

            var raw = $"HashKey={hashKey}&" +
                string.Join("&", sorted) +
                $"&HashIV={hashIV}";

            var encoded = Uri.EscapeDataString(raw)
                .Replace("%20", "+")
                .ToLower();

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(encoded));
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }

        private string BuildECPayForm(Dictionary<string, string> param)
        {
            var actionUrl = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";
            var inputs = string.Join("\n", param.Select(p => $"<input type='hidden' name='{p.Key}' value='{p.Value}' />"));

            return $@"
                <html>
                <body>
                    <form id='ecpayForm' method='post' action='{actionUrl}'>
                        {inputs}
                    </form>
                    <script>document.getElementById('ecpayForm').submit();</script>
                </body>
                </html>";
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

        // =====================================================
        // POST api/Checkout/ECPayCallback
        // 綠界付款完成後，綠界伺服器背景通知（Server to Server）
        // 注意：這支不能加 JWT 驗證，綠界的機器不會帶 token
        // =====================================================
        [AllowAnonymous]
        [HttpPost("ECPayCallback")]
        public async Task<IActionResult> ECPayCallback([FromForm] IFormCollection form)
        {
            // --- 1. 讀取綠界傳來的參數 ---
            var param = form.ToDictionary
                (
                    k => k.Key,
                    v => v.Value.ToString()
                );

            // --- 2. 取出 CheckMacValue 並從參數裡移除 ---
            if (!param.TryGetValue("CheckMacValue", out var receivedMac))
                return Content("0|Error", "text/plain");

            param.Remove("CheckMacValue");

            // --- 3. 再重新算一次 CheckMacValue ---
            var hashKey = _config["ECPay:HashKey"];
            var hashIV = _config["ECPay:HashIV"];
            var calculateMac = GetCheckMacValue(param, hashKey, hashIV);

            // --- 4. 比對是否為綠界打來的參數 ---
            if (!string.Equals(calculateMac, receivedMac, StringComparison.OrdinalIgnoreCase))
                return Content("0|CheckMacValue Error", "text/plain");

            // --- 5. 確認付款結果 ---
            // RtnCode = 1 代表付款成功，其他都是失敗
            var rtnCode = param.GetValueOrDefault("RtnCode", "");
            var merchantTradeNo = param.GetValueOrDefault("MerchantTradeNo", "");
            var tradeNo = param.GetValueOrDefault("TradeNo", ""); //緣異的交易序號

            if(rtnCode != "1")
            {
                return Content("1|OK", "text/plain");//通知綠界收到了
            }

            // --- 6. 用 MerchantTradeNo 找到當初fBatchNo對應的批次 ---
            var batch = await _context.TMarketCheckoutBatches
                .Include(b => b.TMarketOrders)
                .FirstOrDefaultAsync(b => b.FBatchNo == merchantTradeNo);

            if (batch == null)
                return Content("1|OK", "text/plain");

            if (batch.FPaymentStatus == 1)
                return Content("1|OK", "text/plain");
            // --- 7. 更新批次付款狀態 ---
            batch.FPaymentStatus = 1;
            batch.FPaymentTradeNo = tradeNo;
            batch.FPaidAt = DateTime.Now;

            // --- 8. 把所有子訂單也更新成已付款 ---
            foreach(var order in batch.TMarketOrders)
            {
                order.FPaymentStatus = 1;
                order.FOrderStatus = 1;
            }

            await _context.SaveChangesAsync();

            // --- 9. 回傳 1|OK 給綠界 ---
            return Content("1|OK","text/plain");

        }

        // =====================================================
        // GET api/Checkout/PaymentResult
        // 買家付款完成後，瀏覽器跳轉回來的結果頁
        // 給使用者看的，不是給綠界的伺服器打的
        // =====================================================
        [HttpGet("PaymentResult")]
        public IActionResult PaymentResult([FromQuery] string rtnCode, [FromQuery] string merchantTradeNo)
        {
            // rtnCode = 1 代表成功
            if (rtnCode == "1")
            {
                return Content($@"
            <html>
            <body>
                <h2>付款成功！</h2>
                <p>訂單編號：{merchantTradeNo}</p>
                <p>感謝您的購買。</p>
            </body>
            </html>", "text/html");
            }

            return Content($@"
        <html>
        <body>
            <h2>付款失敗或已取消</h2>
            <p>訂單編號：{merchantTradeNo}</p>
            <p>請重新嘗試付款。</p>
        </body>
        </html>", "text/html");
        }
    }


}
