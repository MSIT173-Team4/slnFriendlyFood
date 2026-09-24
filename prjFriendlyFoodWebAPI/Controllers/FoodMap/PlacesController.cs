using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Services;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController : Controller
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
        [HttpGet("nearby")]
        public async Task<ActionResult<NearbyResponseDTO>> GetNearbyPlaces([FromQuery] NearbyRequestDTO request,CancellationToken cancellationToken)
        {
            var result = await _placeService.GetNearbyPlacesAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("resolve")]
        public async Task<ActionResult<int>> ResolvePlace([FromBody] ResolvePlaceRequestDTO request,CancellationToken cancellationToken)
        {
            try
            {
                var placeId = await _placeService.ResolvePlaceAsync(request, cancellationToken);
                return Ok(placeId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}