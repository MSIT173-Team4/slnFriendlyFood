using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Forum
{
    public class EditPostDTO
    {
        public string FTitle { get; set; } = null!;

        public string FPostContent { get; set; } = null!;

        public int FSortId { get; set; }

        public int? FRecipeId { get; set; }
    }
}
