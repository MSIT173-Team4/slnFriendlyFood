using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Chat
{
    public class ChatServices
    {
        private readonly FriendlyFoodDbContext _db;
        public ChatServices(FriendlyFoodDbContext db)
        {
            _db = db;
        }
    }
}
