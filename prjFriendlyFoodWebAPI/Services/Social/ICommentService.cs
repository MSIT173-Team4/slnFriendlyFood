using prjFriendlyFoodWebAPI.DTOs.Social;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    //留言服務需要的方法
    /*依貼文ID取得留言列表、
     * 發佈/更新/刪除留言、
     * 以及切換留言按讚狀態*/
    public interface ICommentService
    {
        Task<List<CommentDto>> GetCommentsByPostIdAsync(int postId, int currentUserId);
        Task<int> CreateCommentAsync(CreateOrUpdateCommentDto dto, int userId);
        Task<bool> UpdateCommentAsync(int commentId, CreateOrUpdateCommentDto dto, int userId);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
        Task<bool> ToggleLikeCommentAsync(int commentId, int userId);
    }
}
