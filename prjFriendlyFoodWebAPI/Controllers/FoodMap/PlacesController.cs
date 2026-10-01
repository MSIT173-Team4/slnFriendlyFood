using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController : ControllerBase
    {
        private readonly IFoodMapService _placeService;

        public PlacesController(IFoodMapService placeService)
        {
            _placeService = placeService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PlaceDTO>>> GetPlace()
        {
            var place = await _placeService.GetPlacesAsync();
            return Ok(place);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<PlaceDTO>> GetPlaceById(long id)
        {
            var place = await _placeService.GetPlaceByIdAsync(id);
            if (place == null)
            {
                return NotFound();
            }
            return Ok(place);
        }

        // 會呼叫 Google（要付費）的端點都需要登入，避免被匿名大量呼叫

        // POST /api/places/nearby（原本是 GET + Query String）
        [Authorize]
        [HttpPost("nearby")]
        public async Task<ActionResult<NearbyResponseDTO>> GetNearbyPlaces(
            [FromBody] NearbyRequestDTO request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _placeService.GetNearbyPlacesAsync(request, cancellationToken));
            }
            catch (HttpRequestException)
            {
                return GoogleUnavailable();
            }
            catch (TaskCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                return GoogleUnavailable();
            }
        }

        // POST /api/places/search：用店名、地名搜尋
        [Authorize]
        [HttpPost("search")]
        public async Task<ActionResult<List<PlaceDTO>>> SearchPlaces(
            [FromBody] PlaceSearchRequestDTO request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _placeService.SearchPlacesAsync(request, cancellationToken));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException)
            {
                return GoogleUnavailable();
            }
            catch (TaskCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                return GoogleUnavailable();
            }
        }

        // POST /api/places/resolve：Google 地點 → 內部店家，回傳完整店家資料（含 fPlaceId）
        // （原本只回傳 int 的 fPlaceId）
        [Authorize]
        [HttpPost("resolve")]
        public async Task<ActionResult<PlaceDTO>> ResolvePlace(
            [FromBody] ResolvePlaceRequestDTO request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _placeService.ResolvePlaceDetailAsync(request, cancellationToken));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException)
            {
                return GoogleUnavailable();
            }
            catch (TaskCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                return GoogleUnavailable();
            }
        }

        private ObjectResult GoogleUnavailable() =>
            StatusCode(StatusCodes.Status502BadGateway, new { message = "Google 地圖服務暫時無法使用，請稍後再試" });
    }
}
