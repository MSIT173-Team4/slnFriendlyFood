using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;

        public RecipeController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRecipes()
        {
            var recipes = await _context.TRecipes
                .Where(r => r.FStatus == 1)
                .OrderByDescending(r => r.FCreatedAt)
                .Select(r => new
                {
                    r.FRecipeId,
                    r.FTitle,
                    r.FDescription,
                    r.FCoverImageUrl,
                    r.FYtVideoId,
                    r.FCookingMinutes,
                    r.FTotalCalories,
                    r.FLikes,
                    r.FIsAiGenerated
                })
                .ToListAsync();

            return Ok(recipes);
        }
    }
}
