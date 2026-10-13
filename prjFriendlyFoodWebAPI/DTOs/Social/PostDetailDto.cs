using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //貼文詳細資訊的DTO，繼承自PostListDto，並新增排序ID和貼文區塊列表
    public class PostDetailDto : PostListDto
    {
        public int SortId { get; set; }
        public List<PostBlockDto> Blocks { get; set; } = new();
    }
}
