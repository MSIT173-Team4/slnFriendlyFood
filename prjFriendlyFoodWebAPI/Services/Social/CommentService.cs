using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    public class CommentService : ICommentService
    {
        private readonly FriendlyFoodDbContext _context;

        public CommentService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommentDto>> GetCommentsByPostIdAsync(int postId, int currentUserId)
        {
            var comments = await _context.TMessageTables
                .Where(m => m.FPostId == postId && m.FMessageState == 1)
                .Include(m => m.FUser)
                .Include(m => m.TMessageLikes)
                .OrderBy(m => m.FMessageDate)
                .ToListAsync();

            var result = new List<CommentDto>();

            foreach (var m in comments)
            {
                string? replyToUser = null;
                if (m.FReplyMessageId.HasValue)
                {
                    var parentMsg = await _context.TMessageTables
                        .Include(p => p.FUser)
                        .FirstOrDefaultAsync(p => p.FMessageId == m.FReplyMessageId);
                    replyToUser = parentMsg?.FUser?.FUsername;
                }

                result.Add(new CommentDto
                {
                    MessageId = m.FMessageId,
                    PostId = m.FPostId,
                    UserId = m.FUserId,
                    UserName = m.FUser.FUsername,
                    UserImage = m.FUser.FImage,
                    ReplyMessageId = m.FReplyMessageId,
                    ReplyToUserName = replyToUser,
                    MessageContent = m.FMessageContent,
                    Likes = m.FLikes,
                    MessageDate = m.FMessageDate,
                    IsLikedByCurrentUser = m.TMessageLikes.Any(l => l.FUserId == currentUserId)
                });
            }

            return result;
        }

        public async Task<int> CreateCommentAsync(CreateOrUpdateCommentDto dto, int userId)
        {
            var comment = new TMessageTable
            {
                FPostId = dto.PostId,
                FUserId = userId,
                FReplyMessageId = dto.ReplyMessageId,
                FMessageContent = dto.MessageContent,
                FLikes = 0,
                FMessageDate = DateTime.Now,
                FMessageState = 1
            };

            _context.TMessageTables.Add(comment);
            await _context.SaveChangesAsync();
            return comment.FMessageId;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.TMessageTables.FirstOrDefaultAsync(m => m.FMessageId == commentId && m.FUserId == userId);
            if (comment == null) return false;

            comment.FMessageState = 0; // 軟刪除
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLikeCommentAsync(int commentId, int userId)
        {
            var comment = await _context.TMessageTables.FindAsync(commentId);
            if (comment == null) return false;

            var existingLike = await _context.TMessageLikes.FirstOrDefaultAsync(l => l.FMessageId == commentId && l.FUserId == userId);
            if (existingLike != null)
            {
                _context.TMessageLikes.Remove(existingLike);
                comment.FLikes = Math.Max(0, comment.FLikes - 1);
            }
            else
            {
                _context.TMessageLikes.Add(new TMessageLike { FMessageId = commentId, FUserId = userId });
                comment.FLikes += 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
