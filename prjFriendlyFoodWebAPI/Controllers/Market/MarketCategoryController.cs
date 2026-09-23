using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Controllers.Market
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketCategoryController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _context;

        public MarketCategoryController(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        // GET /api/MarketCategory
        // 回傳兩層結構：頂層分類 + 各自的子分類
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var all = await _context.TMarketProductCategories
        .Select(c => new MarketCategoryDto
        {
            CategoryId = (int)c.FCategoryId,
            CategoryNo = c.FCategoryNo,
            CategoryName = c.FCategoriesName,
            ParentCategoryId = (int?)c.FParentCategoryId
        })
        .ToListAsync();

            var topLevel = all.Where(c => c.ParentCategoryId == null).ToList();

            foreach (var top in topLevel)
            {
                top.Children = all
                    .Where(c => c.ParentCategoryId == top.CategoryId)
                    .ToList();
            }

            return Ok(topLevel);
        }
    }
}