using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Services;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaceController : Controller
    {
        private readonly IFoodMapService _placeService;


        public PlaceController(IFoodMapService placeService)
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
        public async Task<ActionResult<List<PlaceDTO>>> GetNearbyPlaces(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal radius)
        {
            var nearbyPlaces = await _placeService.GetNearbyPlacesAsync(new PlacesDTO
            {
                Latitude = latitude,
                Longitude = longitude,
                Radius = radius
            });
            return Ok(nearbyPlaces);
        }
        [HttpGet("nearby/fallback")]
        public async Task<ActionResult<List<PlaceDTO>>> GetNearbyPlacesWithFallback(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude)
        {
            var nearbyPlaces = await _placeService.GetNearbyPlacesWithFallbackAsync(latitude, longitude);

            return Ok(nearbyPlaces);
        }
    }
}