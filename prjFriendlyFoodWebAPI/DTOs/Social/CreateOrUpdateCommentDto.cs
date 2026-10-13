namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //留言發佈或更新時需要的DTO，包含貼文ID、回覆留言ID和留言內容
    public class CreateOrUpdateCommentDto
    {
        public int PostId { get; set; }
        public int? ReplyMessageId { get; set; }
        public string MessageContent { get; set; } = string.Empty;
    }
}
