using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public interface ISellerIdentityService
    {
        // 回傳該會員「生效中」的賣場；沒有賣場或已停權回傳 null
        Task<TSeller?> GetActiveSellerAsync(int userId);
    }

    public class SellerIdentityService : ISellerIdentityService
    {
        // tStatus：1 = 生效、0 = 停權
        private const int ActiveStatus = 1;

        private readonly FriendlyFoodDbContext _context;

        public SellerIdentityService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<TSeller?> GetActiveSellerAsync(int userId)
        {
            return await _context.TSellers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.FUserId == userId && s.FStatus == ActiveStatus);
        }
    }
}