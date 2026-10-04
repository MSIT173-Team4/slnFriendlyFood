using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using System.Text;
using System.Web;
using static prjFriendlyFoodWebAPI.DTOs.Market.CheckoutDto;
using prjFriendlyFoodWebAPI.Services.Market;
using prjFriendlyFoodWebAPI.Extensions;
using Microsoft.Extensions.Options;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CheckoutController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICouponService _couponService;
        private readonly IOrderQueryService _orderQuery;
        private readonly IOrderEmailService _orderEmail;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IOrderCancellationService _cancellation;
        private readonly MarketOptions _marketOptions;
        public CheckoutController(
            FriendlyFoodDbContext context,
            IConfiguration config,
            ICouponService couponService,
            IOrderQueryService orderQuery,
            IOrderEmailService orderEmail,
            ILogger<CheckoutController> logger,
            IOrderCancellationService cancellation,
            IOptions<MarketOptions> marketOptions)
        {
            _context = context;
            _config = config;
            _couponService = couponService;
            _orderQuery = orderQuery;
            _orderEmail = orderEmail;
            _logger = logger;
            _cancellation = cancellation;
            _marketOptions = marketOptions.Value;
        }


        [HttpGet("Pay/{batchId}")]
        public async Task<IActionResult> Pay(long batchId)
        {
            int userId = User.GetUserId();

            // 被動觸發：先處理逾期的批次，下面讀到的狀態才是最新的
            await _cancellation.CancelExpiredBatchesAsync();

            var batch = await _context.TMarketCheckoutBatches
                .FirstOrDefaultAsync(b => b.FBatchId == batchId && b.FUserId == userId);

            if (batch == null)
                return PayMessagePage("找不到訂單", "找不到此筆訂單，請回到我的訂單確認。");

            if (batch.FPaymentStatus == MarketStatus.BatchPayment.Paid)
                return PayMessagePage("訂單已付款", "此訂單已完成付款，不需要重複付款。");

            if (batch.FPaymentStatus != MarketStatus.BatchPayment.Unpaid)
                return PayMessagePage("訂單已取消", "此訂單已取消或已逾期，無法付款。如仍需要商品，可使用「再買一次」重新下單。");

            // 保險：就算逾期取消還沒執行到，超過期限也不能付款
            if (DateTime.Now > _marketOptions.PaymentDeadlineOf(batch.FCreatedDate))
                return PayMessagePage("付款期限已過", "此訂單已超過付款期限，系統將自動取消並釋出庫存。");

            // 每次送往綠界都換一個新的交易編號：
            // 綠界不接受重複的 MerchantTradeNo，第二次付款若沿用舊編號會被拒絕
            batch.FBatchNo = GenerateBatchNo();
            await _context.SaveChangesAsync();

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
                { "TradeDesc", "FriendlyFood Checkout" },
                { "ItemName", "Market Order" },
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

        private string GetCheckMacValue(Dictionary<string, string> param, string hashKey, string hashIV)
        {
            var sorted = param
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => $"{p.Key}={p.Value}");

            var raw = $"HashKey={hashKey}&" +
                      string.Join("&", sorted) +
                      $"&HashIV={hashIV}";

            // 綠界正確的編碼方式：
            // 整串 UrlEncode（= 變 %3d，& 變 %26，/ 變 %2f 等）
            // 然後全部轉小寫，不要還原任何字元
            var encoded = HttpUtility.UrlEncode(raw).ToLower();

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

        // Pay 是瀏覽器整頁跳轉過來的，錯誤時回傳一個簡單的說明頁，而不是一行純文字
        private ContentResult PayMessagePage(string title, string message)
        {
            var ordersUrl = $"{_config["AngularBaseUrl"]}/market/orders";
            var html = $@"
                    <!DOCTYPE html>
                    <html lang=""zh-TW"">
                    <head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                    <title>{title}｜友料美食</title></head>
                    <body style=""margin:0;background:#fdf7f4;font-family:'Microsoft JhengHei',Arial,sans-serif;color:#2b1b14;"">
                      <div style=""max-width:480px;margin:80px auto;padding:32px;background:#fff;border-radius:12px;text-align:center;box-shadow:0 2px 8px rgba(0,0,0,.06);"">
                        <h1 style=""margin:0 0 12px;font-size:22px;color:#832600;"">{title}</h1>
                        <p style=""margin:0 0 24px;color:#6b5a52;line-height:1.7;"">{message}</p>
                        <a href=""{ordersUrl}"" style=""display:inline-block;padding:10px 24px;border-radius:8px;background:#832600;color:#fff;text-decoration:none;font-weight:600;"">回到我的訂單</a>
                      </div>
                    </body>
                    </html>";
            return Content(html, "text/html; charset=utf-8");
        }


        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            int userId = User.GetUserId();
            if (request.CartItemIds == null || !request.CartItemIds.Any())
                return BadRequest(new { message = "請選擇至少一件商品" });

            var cartItems = await _context.TMarketShoppingCarts
                .Include(c => c.FProduct)
                .Where(c => request.CartItemIds.Contains(c.FCartItemId)
                         && c.FUserId == userId)
                .ToListAsync();

            if (cartItems.Count != request.CartItemIds.Count)
                return BadRequest(new { message = "部分購物車項目不存在或不屬於此帳號" });

            // 再擋一次不能購買自己的商品
            var mySellerIds = await _context.TSellers
                .Where(s => s.FUserId == userId)
                .Select(s => s.FId)
                .ToListAsync();
            if (cartItems.Any(c => mySellerIds.Contains(c.FProduct.FSellerId)))
                return BadRequest(new { message = "購物車中有您自己上架的商品，請移除後再結帳" });

            foreach (var item in cartItems)
            {
                if (item.FProduct.FStock < item.FQuantity)
                    return BadRequest(new { message = $"商品「{item.FProduct.FProductName}」庫存不足" });
            }

            // 寫入 DB 之前，先把每張子訂單的金額、收件資料全部準備好並檢查完，
            // 有任何問題就在這裡擋下，不用進 transaction 再 rollback
            var orderPlans = new List<OrderPlan>();
            foreach (var g in cartItems.GroupBy(c => c.FSellerId))
            {
                var shipping = request.SellerShippings?.FirstOrDefault(s => s.SellerId == g.Key);
                if (shipping == null
                    || string.IsNullOrWhiteSpace(shipping.RecipientName)
                    || string.IsNullOrWhiteSpace(shipping.RecipientPhone)
                    || string.IsNullOrWhiteSpace(shipping.ShippingAddress))
                {
                    return BadRequest(new { message = "有賣家的收件資料未填寫完整" });
                }

                orderPlans.Add(new OrderPlan
                {
                    SellerId = g.Key,
                    Items = g.ToList(),
                    ItemsAmount = g.Sum(c => c.FProduct.FPrice * c.FQuantity),
                    ShippingFee = MarketConstants.ShippingFeePerSeller,
                    Shipping = shipping
                });
            }

            // ── 套用優惠券：全部在後端重新驗證，不相信購物車頁當時的試算結果 ──
            const string retryHint = "，請回購物車重新確認";
            var couponIdsToUse = new List<int>();

            // 賣家券：每個賣家最多一張，用該賣家的商品金額驗證
            foreach (var sc in request.SellerCoupons ?? new())
            {
                var plan = orderPlans.FirstOrDefault(p => p.SellerId == sc.SellerId);
                if (plan == null)
                    return BadRequest(new { message = "優惠券對應的賣家不在此次結帳中" + retryHint });
                if (plan.Discounts.Any())
                    return BadRequest(new { message = "每個賣家只能使用一張優惠券" });

                var check = await _couponService.ValidateByIdAsync(sc.CouponId, plan.ItemsAmount, plan.SellerId);
                if (!check.IsValid)
                    return BadRequest(new { message = check.ErrorMessage + retryHint });

                var coupon = check.Coupon!;
                if (coupon.FScopeType == "Shipping")
                {
                    plan.ShippingDiscount = plan.ShippingFee;
                    plan.Discounts.Add(new AppliedDiscount { Coupon = coupon, Amount = plan.ShippingFee });
                }
                else
                {
                    plan.ProductDiscount += check.AppliedAmount;
                    plan.Discounts.Add(new AppliedDiscount { Coupon = coupon, Amount = check.AppliedAmount });
                }
                couponIdsToUse.Add(coupon.FCouponId);
            }

            // 全站券：用「扣賣家券前」的商品總額驗證（跟購物車頁算法一致）
            if (request.PlatformCouponId.HasValue)
            {
                decimal itemsTotal = orderPlans.Sum(p => p.ItemsAmount);
                var check = await _couponService.ValidateByIdAsync(request.PlatformCouponId.Value, itemsTotal, null);
                if (!check.IsValid)
                    return BadRequest(new { message = check.ErrorMessage + retryHint });

                var coupon = check.Coupon!;
                if (coupon.FScopeType == "Shipping")
                {
                    // 平台免運券：每個還沒免運的賣家各免 80（已用賣家免運券的不重複折）
                    foreach (var plan in orderPlans.Where(p => p.ShippingDiscount == 0))
                    {
                        plan.ShippingDiscount = plan.ShippingFee;
                        plan.Discounts.Add(new AppliedDiscount { Coupon = coupon, Amount = plan.ShippingFee });
                    }
                }
                else
                {
                    // 全站折扣券：依各子訂單商品金額比例分攤，餘數給最後一張，確保加總剛好等於折抵總額
                    decimal remaining = check.AppliedAmount;
                    for (int i = 0; i < orderPlans.Count; i++)
                    {
                        var plan = orderPlans[i];
                        decimal share = (i == orderPlans.Count - 1)
                            ? remaining
                            : Math.Round(check.AppliedAmount * plan.ItemsAmount / itemsTotal, 0);
                        remaining -= share;

                        if (share > 0)
                        {
                            plan.ProductDiscount += share;
                            plan.Discounts.Add(new AppliedDiscount { Coupon = coupon, Amount = share });
                        }
                    }
                }
                couponIdsToUse.Add(coupon.FCouponId);   // 平台券不管分攤到幾張子訂單，名額只扣 1
            }

            decimal totalAmount = orderPlans.Sum(p => p.PayableAmount);
            if (totalAmount < 1)
                return BadRequest(new { message = "折抵後應付金額為 0，無法進行線上付款" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 先搶優惠券名額；任何一張搶不到就整筆 rollback，名額也會一起還回去
                foreach (var couponId in couponIdsToUse)
                {
                    if (!await _couponService.TryUseAsync(couponId))
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "優惠券已被兌換完畢" + retryHint });
                    }
                }
                // 扣庫存：判斷與扣除在同一句 SQL 完成，兩人同時搶最後一件也不會超賣；
                // 同時要求商品仍為「販售中」，避免購買已下架的商品
                foreach (var item in cartItems)
                {
                    var affected = await _context.TMarketProducts
                        .Where(p => p.FProductId == item.FProductId
                                 && p.FProductStatus == ProductStockRules.OnSale
                                 && p.FStock >= item.FQuantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.FStock, p => p.FStock - item.FQuantity));

                    if (affected == 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"商品「{item.FProduct.FProductName}」庫存不足或已下架{retryHint}" });
                    }
                }

                // 扣到 0 的商品自動改為已售完
                await ProductStockRules.MarkSoldOutAsync(_context, cartItems.Select(c => c.FProductId));
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

                foreach (var plan in orderPlans)
                {
                    var order = new TMarketOrder
                    {
                        FBatchId = batch.FBatchId,
                        FOrderNo = GenerateOrderNo(),
                        FUserId = userId,
                        FSellerId = plan.SellerId,
                        FTotalAmount = plan.PayableAmount,
                        FOrderDate = DateTime.Now,
                        FShippingMethod = "Home",
                        FShippingFee = plan.ShippingFee,
                        FShippingDiscount = plan.ShippingDiscount,
                        FProductDiscount = plan.ProductDiscount,
                        FRecipientName = plan.Shipping.RecipientName.Trim(),
                        FRecipientPhone = plan.Shipping.RecipientPhone.Trim(),
                        FShippingAddress = plan.Shipping.ShippingAddress.Trim(),
                        FOrderStatus = 0,
                        FPaymentStatus = 0,
                        FShippingStatus = 0,
                        FIsShippingConfirmed = false,
                        FCancellationStatus = 0,
                        FReturnStatus = 0,
                    };
                    _context.TMarketOrders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in plan.Items)
                    {
                        _context.TMarketOrderDetails.Add(new TMarketOrderDetail
                        {
                            FOrderId = order.FOrderId,
                            FProductId = item.FProductId,
                            FQuantity = item.FQuantity,
                            FUnitPrice = item.FProduct.FPrice,
                        });
                    }

                    // 折扣快照：券之後被改名或停用，這張訂單的紀錄也不會變
                    foreach (var d in plan.Discounts)
                    {
                        _context.TMarketOrderDiscounts.Add(new TMarketOrderDiscount
                        {
                            FOrderId = order.FOrderId,
                            FCouponId = d.Coupon.FCouponId,
                            FDiscountName = d.Coupon.FName,
                            FDiscountScope = d.Coupon.FScopeType,
                            FDiscountType = d.Coupon.FDiscountType,
                            FAppliedAmount = d.Amount
                        });
                    }

                    await _context.SaveChangesAsync();

                    subOrders.Add(order);
                }

                // 移除已結帳的購物車項目
                _context.TMarketShoppingCarts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new CreateOrderResponseDto
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
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"建立訂單失敗:{ex.Message}" });
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

            // --- 6. 用 MerchantTradeNo 找批次（只讀取需要的欄位） ---
            var batch = await _context.TMarketCheckoutBatches
                .AsNoTracking()
                .Where(b => b.FBatchNo == merchantTradeNo)
                .Select(b => new { b.FBatchId, b.FPaymentStatus })
                .FirstOrDefaultAsync();

            if (batch == null)
            {
                // 例如買家開了兩個綠界頁面、在舊頁面付款，舊的交易編號已被換掉
                _logger.LogError("付款成功但找不到對應的結帳批次，需人工確認退款：MerchantTradeNo={MerchantTradeNo}, TradeNo={TradeNo}",
                    merchantTradeNo, tradeNo);
                return Content("1|OK", "text/plain");
            }

            // 綠界重送同一筆通知：已處理過就直接回覆
            if (batch.FPaymentStatus == MarketStatus.BatchPayment.Paid)
                return Content("1|OK", "text/plain");

            // --- 7. 原子性標記為已付款：只有「仍未付款」的批次能成功，避免與逾期取消同時發生 ---
            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                var paidAt = DateTime.Now;
                var updated = await _context.TMarketCheckoutBatches
                    .Where(b => b.FBatchId == batch.FBatchId && b.FPaymentStatus == MarketStatus.BatchPayment.Unpaid)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.FPaymentStatus, MarketStatus.BatchPayment.Paid)
                        .SetProperty(b => b.FPaymentTradeNo, tradeNo)
                        .SetProperty(b => b.FPaidAt, (DateTime?)paidAt));

                if (updated == 0)
                {
                    await tx.RollbackAsync();

                    // 沒更新到：可能是同一筆通知剛好同時送達（已被另一次處理），或批次已被取消
                    var currentStatus = await _context.TMarketCheckoutBatches
                        .Where(b => b.FBatchId == batch.FBatchId)
                        .Select(b => b.FPaymentStatus)
                        .FirstAsync();

                    if (currentStatus != MarketStatus.BatchPayment.Paid)
                    {
                        _logger.LogError("已付款但結帳批次已取消，需人工退款：BatchId={BatchId}, TradeNo={TradeNo}",
                            batch.FBatchId, tradeNo);
                    }
                    return Content("1|OK", "text/plain");
                }

                // --- 8. 子訂單一起改為已付款、已成立 ---
                await _context.TMarketOrders
                    .Where(o => o.FBatchId == batch.FBatchId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.FPaymentStatus, MarketStatus.Payment.Paid)
                        .SetProperty(o => o.FOrderStatus, MarketStatus.Order.Established));

                await tx.CommitAsync();
            }

            // --- 9. 寄付款成功通知信：失敗只記錄 log，不影響回覆綠界 ---
            try
            {
                await _orderEmail.SendPaymentSuccessAsync(batch.FBatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "付款成功通知信寄送失敗，BatchId={BatchId}", batch.FBatchId);
            }

            // --- 10. 回傳 1|OK 給綠界 ---
            return Content("1|OK", "text/plain");

        }

        // =====================================================
        // GET api/Checkout/PaymentResult
        // 買家付款完成後，瀏覽器跳轉回來的結果頁
        // 給使用者看的，不是給綠界的伺服器打的
        // =====================================================
        [HttpGet("PaymentResult")]
        [HttpPost("PaymentResult")]
        [AllowAnonymous]
        public IActionResult PaymentResult()
        {
            // 從綠界的 Form POST 或 Query String 取 MerchantTradeNo
            var merchantTradeNo = Request.HasFormContentType && Request.Form.ContainsKey("MerchantTradeNo")
                                ? Request.Form["MerchantTradeNo"].ToString()
                                : Request.Query["MerchantTradeNo"].ToString();

            if (!string.IsNullOrEmpty(merchantTradeNo))
            {
                var batch = _context.TMarketCheckoutBatches
                    .FirstOrDefault(b => b.FBatchNo == merchantTradeNo);

                if (batch != null)
                {
                    return Redirect($"{_config["AngularBaseUrl"]}/checkout/complete/{batch.FBatchId}");
                }
            }

            // fallback
            return Redirect($"{_config["AngularBaseUrl"]}/market");
        }

        //=======================================================

        /// <summary>
        /// 訂單完成頁資料
        /// GET /api/Checkout/OrderComplete/{batchId}
        /// 綠界付款完成後，後端 PaymentResult redirect 到 Angular 帶 batchId，
        /// Angular 再來打這支 API 取得完整資料顯示，避免把資料塞在 URL query string 上
        /// </summary>
        /// =====================================================

        [HttpGet("OrderComplete/{batchId}")]
        public async Task<IActionResult> GetOrderComplete(long batchId)
        {
            var dto = await _orderQuery.GetOrderCompleteAsync(batchId, User.GetUserId());
            if (dto == null)
                return NotFound(new { message = "找不到此批次訂單" });

            return Ok(dto);
        }

        // CreateOrder 用：一張子訂單在寫入 DB 前的試算結果
        private class OrderPlan
        {
            public int SellerId { get; set; }
            public List<TMarketShoppingCart> Items { get; set; } = new();
            public decimal ItemsAmount { get; set; }
            public decimal ShippingFee { get; set; }
            public decimal ProductDiscount { get; set; }
            public decimal ShippingDiscount { get; set; }
            public SellerShippingDto Shipping { get; set; } = null!;
            public List<AppliedDiscount> Discounts { get; set; } = new();

            // 子訂單應付金額；賣家券 + 全站券分攤理論上可能超過商品金額，最低保底 0
            public decimal PayableAmount =>
                Math.Max(0, ItemsAmount - ProductDiscount + ShippingFee - ShippingDiscount);
        }

        private class AppliedDiscount
        {
            public TMarketCoupon Coupon { get; set; } = null!;
            public decimal Amount { get; set; }
        }
    }
}
