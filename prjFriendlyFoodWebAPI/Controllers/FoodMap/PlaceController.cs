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
        public async Task<ActionResult<NearbyPlacesDTO>> GetNearbyPlaces(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal radiuskm)
        {
            var nearbyPlaces = await _placeService.GetNearbyPlacesAsync(new NearbyPlacesDTO
            {
                Latitude = latitude,
                Longitude = longitude,
                Radius = radiuskm
            });
            return Ok(nearbyPlaces);
        }
    }
}