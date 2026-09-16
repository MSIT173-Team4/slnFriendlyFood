using prjFriendlyFoodWebAPI.DTOs.Social;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    public interface IPostService
    {
        Task<List<PostListDto>> GetPostsAsync(string sortBy, string? keyword, int currentUserId);
        Task<PostDetailDto?> GetPostByIdAsync(int postId, int currentUserId);
        Task<int> CreatePostAsync(CreateOrUpdatePostDto dto, int userId);
        Task<bool> UpdatePostAsync(int postId, CreateOrUpdatePostDto dto, int userId);
        Task<bool> DeletePostAsync(int postId, int userId);
        Task<bool> ToggleLikePostAsync(int postId, int userId);

    }
}
