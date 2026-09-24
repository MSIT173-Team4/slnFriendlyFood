using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class CreateOrUpdatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public int SortId { get; set; } = 1;
        public List<PostBlockDto> Blocks { get; set; } = new();
    }
}
