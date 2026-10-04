using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Services.Market;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketCouponController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public MarketCouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        // POST /api/MarketCoupon/validate — 購物車頁試算用，不扣名額
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponDto dto)
        {
            var result = await _couponService.ValidateByCodeAsync(dto.Code, dto.OrderAmount, dto.SellerId);
            if (!result.IsValid)
                return BadRequest(new { message = result.ErrorMessage });

            var coupon = result.Coupon!;
            return Ok(new ValidateCouponResultDto
            {
                CouponId = coupon.FCouponId,
                CouponName = coupon.FName,
                ScopeType = coupon.FScopeType,
                DiscountType = coupon.FDiscountType,
                DiscountValue = coupon.FDiscountValue,
                AppliedAmount = result.AppliedAmount,
                Message = coupon.FScopeType == "Shipping"
                    ? $"已套用運費券「{coupon.FName}」運費折抵 NT$ {result.AppliedAmount:0}"
                    : $"已套用「{coupon.FName}」省 NT$ {result.AppliedAmount:0}"
            });
        }
    }
}