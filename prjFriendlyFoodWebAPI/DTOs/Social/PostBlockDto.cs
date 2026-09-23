namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class PostBlockDto
    {
        public int PostBlockId { get; set; }
        public string BlockType { get; set; } = "text";
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
