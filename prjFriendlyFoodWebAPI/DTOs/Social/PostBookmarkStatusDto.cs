namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //收藏貼文狀態需要貼文ID和是否已收藏
    public class PostBookmarkStatusDto
    {
        public int PostId { get; set; }

        public bool IsBookmarked { get; set; }
    }
}
