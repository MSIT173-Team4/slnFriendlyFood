using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class PostListDto
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserImage { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Likes { get; set; }
        public int Views { get; set; }
        public DateTime PostDate { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
