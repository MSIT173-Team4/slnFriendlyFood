using prjFriendlyFoodWebAPI.DTOs.Social;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    public interface ICommentService
    {
        Task<List<CommentDto>> GetCommentsByPostIdAsync(int postId, int currentUserId);
        Task<int> CreateCommentAsync(CreateOrUpdateCommentDto dto, int userId);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
        Task<bool> ToggleLikeCommentAsync(int commentId, int userId);

    }
}
