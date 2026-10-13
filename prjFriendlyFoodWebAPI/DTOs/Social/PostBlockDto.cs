namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //貼文區塊的DTO，包含區塊ID、區塊類型、內容、媒體URL和排序順序
    public class PostBlockDto
    {
        public int PostBlockId { get; set; }
        public string BlockType { get; set; } = "text";
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
