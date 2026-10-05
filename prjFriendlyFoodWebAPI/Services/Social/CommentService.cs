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

            var userMap = comments.ToDictionary(c => c.FMessageId, c => c.FUser?.FUsername);

            var result = comments.Select(m => new CommentDto
            {
                MessageId = m.FMessageId,
                PostId = m.FPostId,
                UserId = m.FUserId,
                UserName = m.FUser?.FUsername ?? "匿名使用者",
                UserImage = m.FUser?.FImage,
                ReplyMessageId = m.FReplyMessageId,
                ReplyToUserName = m.FReplyMessageId.HasValue && userMap.TryGetValue(m.FReplyMessageId.Value, out var name)
                    ? name
                    : null,
                MessageContent = m.FMessageContent,
                Likes = m.FLikes,
                MessageDate = m.FMessageDate,
                IsLikedByCurrentUser = m.TMessageLikes.Any(l => l.FUserId == currentUserId)
            }).ToList();

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
        public async Task<bool> UpdateCommentAsync(
            int commentId,
            CreateOrUpdateCommentDto dto,
            int userId)
        {
            var comment = await _context.TMessageTables
                .Include(m => m.FPost)
                .FirstOrDefaultAsync(
                    m => m.FMessageId == commentId &&
                         m.FMessageState == 1);

            if (comment == null)
                return false;

            bool isCommentOwner = comment.FUserId == userId;
            //bool isPostOwner = comment.FPost != null &&
            //                   comment.FPost.FUserId == userId;

            if (!isCommentOwner /*&& !isPostOwner*/)
                return false;

            if (string.IsNullOrWhiteSpace(dto.MessageContent))
                return false;

            comment.FMessageContent = dto.MessageContent.Trim();

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.TMessageTables
                .Include(m => m.FPost)
                .FirstOrDefaultAsync(m => m.FMessageId == commentId && m.FMessageState == 1);

            if (comment == null) return false;

            bool isCommentOwner = comment.FUserId == userId;
            bool isPostOwner = comment.FPost != null && comment.FPost.FUserId == userId;

            if (!isCommentOwner && !isPostOwner) return false;

            comment.FMessageState = 0;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLikeCommentAsync(int commentId, int userId)
        {
            var comment = await _context.TMessageTables.FindAsync(commentId);
            if (comment == null || comment.FMessageState != 1) return false;

            var existingLike = await _context.TMessageLikes
                .FirstOrDefaultAsync(l => l.FMessageId == commentId && l.FUserId == userId);

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