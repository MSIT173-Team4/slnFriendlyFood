using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        protected int CurrentUserId => 1;
        private readonly ITripServices _tripServices;
        private readonly ITripPlanningService _tripPlanningService;

        public TripsController(ITripServices tripServices, ITripPlanningService tripPlanningService)
        {
            _tripServices = tripServices;

            _tripPlanningService = tripPlanningService;
        }

       

        [HttpGet("{id:long}")]
        public async Task<ActionResult<TripDTO>>
        GetTrip(int id)
        {
            var trip =
                await _tripServices
                    .GetTripByIdAsync(id);

            if (trip is null)
            {
                return NotFound(new
                {
                    message = "找不到指定行程"
                });
            }

            return Ok(trip);
        }
        [HttpPost]
        public async Task<ActionResult<TripDTO>> CreateTrip([FromBody] CreateTripRequestDTO tripDTO)
        {
            try
            {
                var trip = await _tripServices.CreateTripAsync(tripDTO);
                return Ok(trip);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("plan")]
        public async Task<ActionResult<PlanTripResultDTO>> PlanTrip(
           [FromBody] PlanTripApiRequest request,
           CancellationToken cancellationToken)
        {
            try
            {
                var result = await _tripPlanningService.PlanTripAsync(
                    CurrentUserId,
                    new PlanTripRequestDTO
                    {
                        ShoppingListId = request.ShoppingListId,
                        OriginLatitude = request.OriginLatitude,
                        OriginLongitude = request.OriginLongitude,
                        TravelMode = request.TravelMode,
                        SearchRadiusMeters = request.SearchRadiusMeters
                    },
                    cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // 「清單是空的」「附近沒有符合的店家」這類可預期的業務錯誤，回 400
                return BadRequest(new { message = ex.Message });
            }
            // 其餘未預期的例外（DbUpdateException、GoogleRoutesUnavailableException
            // 沒被 Service 內部接住的部分…）交給 H.1 的 Global Exception Handler 統一處理，
            // 這裡不用再另外 catch 一次，避免每支 Controller 都重複寫一樣的錯誤處理邏輯
        }

        // 假設你專案裡已經有取得目前登入使用者 ID 的邏輯（例如 JWT Claim），
        // 如果既有 TripsController 裡已經有 CurrentUserId 這個屬性，
        // 這段就不用重複加，直接刪掉，沿用原本的就好
        //protected int CurrentUserId =>
        //    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        //        ?? throw new UnauthorizedAccessException("無法解析使用者身分"));
    }

    // ---------- Request DTO：輸入驗證（對應 H.2）----------

    public class PlanTripApiRequest
    {
        [Required]
        public int ShoppingListId { get; set; }

        [Required, Range(-90, 90, ErrorMessage = "緯度必須介於 -90 到 90 之間")]
        public decimal OriginLatitude { get; set; }

        [Required, Range(-180, 180, ErrorMessage = "經度必須介於 -180 到 180 之間")]
        public decimal OriginLongitude { get; set; }

        public GoogleTravelMode TravelMode { get; set; } = GoogleTravelMode.Drive;

        [Range(100, 20000, ErrorMessage = "搜尋半徑必須介於 100 公尺到 20 公里之間")]
        public int SearchRadiusMeters { get; set; } = 3000;
    }
}


    
