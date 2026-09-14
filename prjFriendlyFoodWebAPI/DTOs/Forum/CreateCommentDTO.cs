namespace prjFriendlyFoodWebAPI.DTOs.Forum
{
    public class CreateCommentDTO
    {
        public int FPostId { get; set; }

        public string FMessageContent { get; set; } = null!;

        public int? FReplyMessageId { get; set; }
    }
}
