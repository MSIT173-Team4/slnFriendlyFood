using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Services.Market;
using prjFriendlyFoodWebAPI.DTOs.Market;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarketOrderController : ControllerBase
    {
        private readonly IOrderQueryService _orderQuery;
        private readonly ICartService _cartService;

        public MarketOrderController(IOrderQueryService orderQuery, ICartService cartService)
        {
            _orderQuery = orderQuery;
            _cartService = cartService;
        }

        // GET /api/MarketOrder/my?tab=all&range=6m&keyword=&page=1 — 我的訂單
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders(
           [FromQuery] string tab = "all",
           [FromQuery] string range = "6m",
           [FromQuery] string? keyword = null,
           [FromQuery] int page = 1)
        {
            if (!MyOrderOptions.Tabs.Contains(tab))
                return BadRequest(new { message = $"不支援的分頁「{tab}」，可用值：{string.Join("、", MyOrderOptions.Tabs)}" });

            if (!MyOrderOptions.Ranges.Contains(range))
                return BadRequest(new { message = $"不支援的時間範圍「{range}」，可用值：{string.Join("、", MyOrderOptions.Ranges)}" });

            if (page < 1)
                return BadRequest(new { message = "頁碼必須從 1 開始" });

            var result = await _orderQuery.GetMyOrdersAsync(User.GetUserId(), tab, range, keyword, page);
            return Ok(result);
        }

        // POST /api/MarketOrder/rebuy — 再買一次（我的訂單傳一張、完成頁傳整個批次）
        [HttpPost("rebuy")]
        public async Task<IActionResult> Rebuy([FromBody] RebuyRequestDto dto)
        {
            if (dto.OrderIds == null || dto.OrderIds.Count == 0)
                return BadRequest(new { message = "請指定要再買一次的訂單" });

            var result = await _cartService.RebuyAsync(User.GetUserId(), dto.OrderIds);

            if (result.AddedCount == 0 && result.Skipped.Count == 0)
                return NotFound(new { message = "找不到訂單" });

            return Ok(result);
        }
    }
}