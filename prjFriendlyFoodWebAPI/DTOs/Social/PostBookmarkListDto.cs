namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class PostBookmarkListDto
    {
        public int BookmarkId { get; set; }

        public DateTime BookmarkDate { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = null!;

        public string? UserImage { get; set; }

        public string Title { get; set; } = null!;

        public int Likes { get; set; }

        public int Views { get; set; }

        public DateTime PostDate { get; set; }

        public bool IsLikedByCurrentUser { get; set; }
    }
}
