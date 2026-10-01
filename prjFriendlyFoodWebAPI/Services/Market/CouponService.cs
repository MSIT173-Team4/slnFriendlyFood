using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 驗證結果：成功時帶券和折抵金額，失敗時帶錯誤訊息
    public class CouponCheckResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public TMarketCoupon? Coupon { get; set; }
        public decimal AppliedAmount { get; set; }

        public static CouponCheckResult Fail(string message) =>
            new() { IsValid = false, ErrorMessage = message };
    }

    public interface ICouponService
    {
        Task<CouponCheckResult> ValidateByCodeAsync(string code, decimal orderAmount, int? sellerId);
        Task<CouponCheckResult> ValidateByIdAsync(int couponId, decimal orderAmount, int? sellerId);
        Task<bool> TryUseAsync(int couponId);
    }

    // validate API 與 CreateOrder 共用同一套規則，避免兩邊各寫一份、改規則時漏改
    public class CouponService : ICouponService
    {
        private readonly FriendlyFoodDbContext _context;

        public CouponService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // 購物車頁：買家輸入代碼
        public async Task<CouponCheckResult> ValidateByCodeAsync(string code, decimal orderAmount, int? sellerId)
        {
            var coupon = await _context.TMarketCoupons
                .FirstOrDefaultAsync(c => c.FCode == code && c.FIsActive == true);
            return Check(coupon, orderAmount, sellerId);
        }

        // CreateOrder：前端只送 couponId，後端重新驗證
        public async Task<CouponCheckResult> ValidateByIdAsync(int couponId, decimal orderAmount, int? sellerId)
        {
            var coupon = await _context.TMarketCoupons
                .FirstOrDefaultAsync(c => c.FCouponId == couponId && c.FIsActive == true);
            return Check(coupon, orderAmount, sellerId);
        }

        private CouponCheckResult Check(TMarketCoupon? coupon, decimal orderAmount, int? sellerId)
        {
            if (coupon == null)
                return CouponCheckResult.Fail("優惠代碼不存在或已停用");

            // 有效期限
            var now = DateTime.Now;
            if (now < coupon.FStartDate)
                return CouponCheckResult.Fail("此優惠券尚未開始");
            if (coupon.FEndDate.HasValue && now > coupon.FEndDate.Value)
                return CouponCheckResult.Fail("此優惠券已過期");

            // 總量上限（這裡只是預檢；CreateOrder 真正扣名額時還會用原子性 UPDATE 再擋一次）
            if (coupon.FTotalLimit.HasValue && coupon.FUsedCount >= coupon.FTotalLimit.Value)
                return CouponCheckResult.Fail("此優惠券已被兌換完畢");

            // 歸屬：賣家欄位只收該賣家的券，全站欄位只收平台券（FSellerId 為 NULL）
            if (sellerId.HasValue)
            {
                if (coupon.FSellerId != sellerId)
                    return CouponCheckResult.Fail("此優惠券不適用於此賣場");
            }
            else
            {
                if (coupon.FSellerId != null)
                    return CouponCheckResult.Fail("此為賣場專屬優惠券，請在對應賣家欄位輸入");
            }

            // 最低消費門檻
            if (coupon.FMinPurchaseAmount.HasValue && orderAmount < coupon.FMinPurchaseAmount.Value)
                return CouponCheckResult.Fail($"未達最低消費門檻 NT$ {coupon.FMinPurchaseAmount:0}");

            // 折抵金額
            decimal appliedAmount;
            if (coupon.FScopeType == "Shipping")
            {
                // 運費券折的是單一賣家的運費，跟商品金額無關，不受下面「不超過訂單金額」限制
                appliedAmount = MarketConstants.ShippingFeePerSeller;
            }
            else
            {
                if (coupon.FDiscountType == "Percentage")
                {
                    appliedAmount = Math.Round(orderAmount * (coupon.FDiscountValue / 100), 0);
                    if (coupon.FMaxDiscountAmount.HasValue && appliedAmount > coupon.FMaxDiscountAmount.Value)
                        appliedAmount = coupon.FMaxDiscountAmount.Value;
                }
                else
                {
                    appliedAmount = coupon.FDiscountValue;
                }

                if (appliedAmount > orderAmount)
                    appliedAmount = orderAmount;
            }

            return new CouponCheckResult
            {
                IsValid = true,
                Coupon = coupon,
                AppliedAmount = appliedAmount
            };
        }

        public async Task<bool> TryUseAsync(int couponId)
        {
            var affected = await _context.TMarketCoupons
                .Where(c => c.FCouponId == couponId
                         && (c.FTotalLimit == null || c.FUsedCount < c.FTotalLimit))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.FUsedCount, c => c.FUsedCount + 1));
            return affected == 1;
        }
    }
}