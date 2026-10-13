using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    //貼文服務需要的方法
    /*依貼文ID取得貼文詳細資訊、
     * 發佈/更新/刪除貼文、
     * 按讚收藏，
     * 以及取得貼文列表*/
    public interface IPostService
    {
        Task<PostDetailDto?> GetPostByIdAsync(int postId, int currentUserId);
        Task<int> CreatePostAsync(CreateOrUpdatePostDto dto, int userId);
        Task<bool> UpdatePostAsync(int postId, CreateOrUpdatePostDto dto, int userId);
        Task<bool> DeletePostAsync(int postId, int userId);
        Task<PostBookmarkStatusDto> ToggleBookmarkAsync(int postId, int currentUserId);
        Task<bool> ToggleLikeAsync(int postId, int currentUserId);
        Task<PagedResultDto<PostResponseDto>> GetPostsAsync(PostQueryParameters queryParams, int currentUserId);
    }
}
