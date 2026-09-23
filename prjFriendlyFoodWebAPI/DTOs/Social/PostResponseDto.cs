namespace prjFriendlyFoodWebAPI.DTOs.Social
{
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
