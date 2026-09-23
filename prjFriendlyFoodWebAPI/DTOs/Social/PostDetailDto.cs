using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class PostDetailDto : PostListDto
    {
        public int SortId { get; set; }
        public List<PostBlockDto> Blocks { get; set; } = new();
    }
}
