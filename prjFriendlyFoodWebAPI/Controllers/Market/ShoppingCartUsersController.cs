using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShoppingCartUsersController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;

        public ShoppingCartUsersController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // GET /api/ShoppingCartUsers/profile — 結帳頁帶入收件人預設資料
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = User.GetUserId();

            var user = await _context.TUsers
                .Where(u => u.FId == userId)
                .Select(u => new
                {
                    u.FId,
                    u.FUsername,
                    u.FLastName,
                    u.FFirstName,
                    u.FPhone,
                    u.FAddress
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "找不到使用者資料" });

            // 收件人要真實姓名（姓 + 名），跟會員模組 Apply 的組法一致；
            // 會員沒填真實姓名就回傳空字串，讓買家在結帳頁自己填，不拿帳號名稱頂替
            var recipientName = $"{user.FLastName}{user.FFirstName}".Trim();

            return Ok(new
            {
                userId = user.FId,
                username = user.FUsername,
                recipientName,
                phone = user.FPhone ?? string.Empty,
                address = user.FAddress ?? string.Empty
            });
        }
    }
}