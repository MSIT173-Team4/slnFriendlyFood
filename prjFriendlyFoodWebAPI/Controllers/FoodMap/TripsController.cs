using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Controllers.FoodMap
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripServices _tripServices;

        public TripsController(ITripServices tripServices)
        {
            _tripServices = tripServices;
        }

        [HttpGet]
        public async Task<ActionResult<List<TripDTO>>> GetTrips()
        {
            var trips = await _tripServices.GetTripsAsync();
            return Ok(trips);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<TripDTO>>
        GetTrip(long id)
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
    }
}
