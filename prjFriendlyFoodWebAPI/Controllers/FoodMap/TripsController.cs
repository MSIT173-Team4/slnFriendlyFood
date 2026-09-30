using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    // 全部需要登入：JWT 由登入時寫入的 token cookie 帶過來（前端要加 withCredentials: true）
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripsController : ControllerBase
    {
        private readonly ITripServices _tripServices;
        private readonly ITripPlanningService _tripPlanningService;

        public TripsController(ITripServices tripServices, ITripPlanningService tripPlanningService)
        {
            _tripServices = tripServices;
            _tripPlanningService = tripPlanningService;
        }

        // 原本寫死 1，改成從 JWT 讀目前登入的使用者（跟 UsersController 一樣用 NameIdentifier）
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : throw new UnauthorizedAccessException("無法解析使用者身分");

        // GET /api/trips：我的行程
        [HttpGet]
        public Task<ActionResult<List<TripDTO>>> GetTrips(CancellationToken cancellationToken)
        {
            return HandleAsync<List<TripDTO>>(async () =>
                await _tripServices.GetTripsAsync(CurrentUserId, cancellationToken));
        }

        // GET /api/trips/5
        [HttpGet("{id:int}")]
        public Task<ActionResult<TripDTO>> GetTrip(int id, CancellationToken cancellationToken)
        {
            return HandleAsync<TripDTO>(async () =>
                await _tripServices.GetTripByIdAsync(id, CurrentUserId, cancellationToken)
                ?? throw new FoodMapNotFoundException("找不到指定行程"));
        }

        // POST /api/trips：直接用指定的店家建立行程（不算路線）
        [HttpPost]
        public Task<ActionResult<TripDTO>> CreateTrip(
            [FromBody] CreateTripRequestDTO tripDTO,
            CancellationToken cancellationToken)
        {
            return HandleAsync<TripDTO>(async () =>
                await _tripServices.CreateTripAsync(tripDTO, CurrentUserId, null, cancellationToken));
        }

        // GET /api/trips/planning-context?shoppingListId=6
        // trip-builder 進頁面時呼叫：要規劃哪一份清單（沒帶就用自己目前的清單）、會員地址座標
        [HttpGet("planning-context")]
        public Task<ActionResult<PlanningContextDTO>> GetPlanningContext(
            [FromQuery] int? shoppingListId,
            CancellationToken cancellationToken)
        {
            return HandleAsync<PlanningContextDTO>(async () =>
                await _tripPlanningService.GetPlanningContextAsync(CurrentUserId, shoppingListId, cancellationToken));
        }

        // POST /api/trips/plan/preview：第一段，只計算、不存檔
        [HttpPost("plan/preview")]
        public Task<ActionResult<PlanTripPreviewResultDTO>> PreviewTrip(
            [FromBody] PlanTripApiRequest request,
            CancellationToken cancellationToken)
        {
            return HandleAsync<PlanTripPreviewResultDTO>(async () =>
                await _tripPlanningService.PreviewTripAsync(CurrentUserId, request.ToServiceRequest(), cancellationToken));
        }

        // POST /api/trips/plan/confirm：第二段，使用者確認後建立行程並算路線
        [HttpPost("plan/confirm")]
        public Task<ActionResult<PlanTripResultDTO>> ConfirmTrip(
            [FromBody] ConfirmTripRequestDTO request,
            CancellationToken cancellationToken)
        {
            return HandleAsync<PlanTripResultDTO>(async () =>
                await _tripPlanningService.ConfirmTripAsync(CurrentUserId, request, cancellationToken));
        }

        // POST /api/trips/plan：舊版相容（一次做完、直接存檔）
        [HttpPost("plan")]
        public Task<ActionResult<PlanTripResultDTO>> PlanTrip(
            [FromBody] PlanTripApiRequest request,
            CancellationToken cancellationToken)
        {
            return HandleAsync<PlanTripResultDTO>(async () =>
                await _tripPlanningService.PlanTripAsync(CurrentUserId, request.ToServiceRequest(), cancellationToken));
        }

        // 把 Service 丟出的可預期錯誤轉成對應的 HTTP 狀態碼
        private async Task<ActionResult<T>> HandleAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (FoodMapNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FoodMapForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Google 地圖服務暫時無法使用，請稍後再試" });
            }
            catch (TaskCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new { message = "Google 地圖服務回應逾時，請稍後再試" });
            }
        }
    }

    // ---------- Request DTO：輸入驗證 ----------

    public class PlanTripApiRequest
    {
        [Required, Range(1, int.MaxValue, ErrorMessage = "缺少採買清單")]
        public int ShoppingListId { get; set; }

        [Required, Range(-90, 90, ErrorMessage = "緯度必須介於 -90 到 90 之間")]
        public decimal OriginLatitude { get; set; }

        [Required, Range(-180, 180, ErrorMessage = "經度必須介於 -180 到 180 之間")]
        public decimal OriginLongitude { get; set; }

        public GoogleTravelMode TravelMode { get; set; } = GoogleTravelMode.Drive;

        [Range(100, 20000, ErrorMessage = "搜尋半徑必須介於 100 公尺到 20 公里之間")]
        public int SearchRadiusMeters { get; set; } = 3000;

        public PlanTripRequestDTO ToServiceRequest() => new()
        {
            ShoppingListId = ShoppingListId,
            OriginLatitude = OriginLatitude,
            OriginLongitude = OriginLongitude,
            TravelMode = TravelMode,
            SearchRadiusMeters = SearchRadiusMeters
        };
    }
}
