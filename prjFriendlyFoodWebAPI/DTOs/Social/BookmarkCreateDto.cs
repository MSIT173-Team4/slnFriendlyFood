using System.Runtime.InteropServices;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    //收藏貼文需要貼文ID
    public class BookmarkCreateDto
    {
        public int PostId { get; set; }
    }
}
