using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.DTOs.Forum
{
    public class CommentDTO
    {
        public int FMessageId { get; set; }

        public int FPostId { get; set; }

        public int FUserId { get; set; }

        public int? FReplyMessageId { get; set; }

        public string FMessageContent { get; set; } = null!;

        public int FLikes { get; set; }

        public DateTime FMessageDate { get; set; }

        public byte FMessageState { get; set; }

        public bool IsLiked { get; set; }

        //public UserDTO User { get; set; } = null!;
    }
}
