namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //貼文清單的每一則貼文需要顯示的資訊
    /*需要貼文的ID、使用者ID、
     * 使用者名稱、使用者大頭貼、
     * 貼文標題、按讚數、瀏覽數、貼文日期，
     * 以及是否被當前使用者收藏或按讚*/
    public class PostResponseDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string SummaryText { get; set; } = null!;
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? UserImage { get; set; }
        public int Likes { get; set; }
        public int Views { get; set; }
        public int CommentCount { get; set; }
        public DateTime PostDate { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsBookmarkedByCurrentUser { get; set; }
    }
}
