using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //貼文的新增或更新DTO，包含標題、排序ID和貼文區塊列表
    public class CreateOrUpdatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public int SortId { get; set; } = 1;
        public List<PostBlockDto> Blocks { get; set; } = new();
    }
}
