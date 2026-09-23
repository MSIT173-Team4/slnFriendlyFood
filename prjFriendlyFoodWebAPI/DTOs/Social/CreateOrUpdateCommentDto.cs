namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class CreateOrUpdateCommentDto
    {
        public int PostId { get; set; }
        public int? ReplyMessageId { get; set; }
        public string MessageContent { get; set; } = string.Empty;
    }
}
