using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Forum
{
    public class PostDTO
    {
        public int FPostId { get; set; }

        public int FUserId { get; set; }

        public string FTitle { get; set; } = null!;

        public int FLikes { get; set; }

        public int FViews { get; set; }

        public DateTime FPostDate { get; set; }

        public byte FPostState { get; set; }

        public int FSortId { get; set; }

        public int? FRecipeId { get; set; }

        public bool IsLiked { get; set; }
    }
}
