using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Social
{
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
