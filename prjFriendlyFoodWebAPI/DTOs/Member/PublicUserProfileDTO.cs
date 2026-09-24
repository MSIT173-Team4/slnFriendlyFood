using Microsoft.AspNetCore.SignalR;

namespace prjFriendlyFoodWebAPI.DTOs.Member
{
    public class PublicUserProfileDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Image { get; set; }
        public string CreateTime { get; set; }
    }
}
