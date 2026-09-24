using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketCouponController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;

        public MarketCouponController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // POST /api/MarketCoupon/validate — 驗證優惠券
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponDto dto)
        {
            // 1. 找這張券
            var coupon = await _context.TMarketCoupons
                .FirstOrDefaultAsync(c => c.FCode == dto.Code && c.FIsActive == true);

            if (coupon == null)
                return BadRequest(new { message = "優惠代碼不存在或已停用" });

            // 2. 檢查有效期限
            var now = DateTime.Now;
            if (now < coupon.FStartDate)
                return BadRequest(new { message = "此優惠券尚未開始" });
            if (coupon.FEndDate.HasValue && now > coupon.FEndDate.Value)
                return BadRequest(new { message = "此優惠券已過期" });

            // 3. 檢查總量上限
            if (coupon.FTotalLimit.HasValue && coupon.FUsedCount >= coupon.FTotalLimit.Value)
                return BadRequest(new { message = "此優惠券已被兌換完畢" });

            // 4. 檢查賣場券的賣家是否符合
            if (coupon.FScopeType == "Store")
            {
                if (dto.SellerId == null || coupon.FSellerId != dto.SellerId)
                    return BadRequest(new { message = "此優惠券不適用於此賣場" });
            }

            // 5. 檢查最低消費門檻
            if (coupon.FMinPurchaseAmount.HasValue && dto.OrderAmount < coupon.FMinPurchaseAmount.Value)
                return BadRequest(new { message = $"未達最低消費門檻 NT$ {coupon.FMinPurchaseAmount:0}" });

            // 6. 計算折抵金額
            decimal appliedAmount = 0;

            if (coupon.FScopeType == "Shipping")
            {
                // 運費券：固定折抵運費 80 元
                appliedAmount = 80;
            }
            else if (coupon.FDiscountType == "Fixed")
            {
                appliedAmount = coupon.FDiscountValue;
            }
            else if (coupon.FDiscountType == "Percentage")
            {
                appliedAmount = Math.Round(dto.OrderAmount * (coupon.FDiscountValue / 100), 0);

                // 7. 檢查最高折抵上限
                if (coupon.FMaxDiscountAmount.HasValue && appliedAmount > coupon.FMaxDiscountAmount.Value)
                    appliedAmount = coupon.FMaxDiscountAmount.Value;
            }

            // 8. 折抵金額不能超過訂單金額
            if (appliedAmount > dto.OrderAmount)
                appliedAmount = dto.OrderAmount;

            return Ok(new ValidateCouponResultDto
            {
                CouponId = coupon.FCouponId,
                CouponName = coupon.FName,
                ScopeType = coupon.FScopeType,
                DiscountType = coupon.FDiscountType,
                DiscountValue = coupon.FDiscountValue,
                AppliedAmount = appliedAmount,
                Message = coupon.FScopeType == "Shipping"
                    ? $"已套用運費券「{coupon.FName}」運費折抵 NT$ {appliedAmount:0}"
                    : $"已套用「{coupon.FName}」省 NT$ {appliedAmount:0}"
            });
        }
    }
}