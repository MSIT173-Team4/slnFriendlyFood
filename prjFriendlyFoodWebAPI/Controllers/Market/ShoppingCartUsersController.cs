using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartUsersController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;

        public ShoppingCartUsersController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // GET /api/ShoppingCartUsers/profile
        // TODO: 之後換成從 JWT 拿 userId
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = 1; // TODO: 換成 JWT

            var user = await _context.TUsers
                .Where(u => u.FId == userId)
                .Select(u => new
                {
                    userId = u.FId,
                    username = u.FUsername,
                    phone = u.FPhone,
                    address = u.FAddress
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "找不到使用者資料" });

            return Ok(user);
        }
    }
}