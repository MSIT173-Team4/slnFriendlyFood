using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace prjFriendlyFoodWebAPI.Services.Social
{
    public class PostService : IPostService
    {
        private readonly FriendlyFoodDbContext _context;

        public PostService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<List<PostListDto>> GetPostsAsync(string sortBy, string? keyword, int currentUserId)
        {
            var query = _context.TPostTables
                .Where(p => p.FPostState == 1)
                .Include(p => p.FUser)
                .Include(p => p.TPostLikes)
                .AsQueryable();

            //關鍵字搜尋 (期末新增"使用者名稱")
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(p => p.FTitle.ToLower().Contains(kw) || p.FUser.FUsername.ToLower().Contains(kw));
            }

            //(按讚數 * 10 + 瀏覽量)
            query = sortBy.ToLower() switch
            {
                "popular" => query.OrderByDescending(p => (p.FLikes * 10) + p.FViews).ThenByDescending(p => p.FPostDate),
                _ => query.OrderByDescending(p => p.FPostDate)
            };

            return await query.Select(p => new PostListDto
            {
                PostId = p.FPostId,
                UserId = p.FUserId,
                UserName = p.FUser.FUsername,
                UserImage = p.FUser.FImage,
                Title = p.FTitle,
                Likes = p.FLikes,
                Views = p.FViews,
                PostDate = p.FPostDate,
                IsLikedByCurrentUser = p.TPostLikes.Any(l => l.FUserId == currentUserId)
            }).ToListAsync();
        }

        public async Task<PostDetailDto?> GetPostByIdAsync(int postId, int currentUserId)
        {
            var post = await _context.TPostTables
                .Include(p => p.FUser)
                .Include(p => p.TPostLikes)
                .Include(p => p.TPostBlockTables)
                .FirstOrDefaultAsync(p => p.FPostId == postId && p.FPostState == 1);

            if (post == null) return null;

            post.FViews += 1;
            await _context.SaveChangesAsync();

            return new PostDetailDto
            {
                PostId = post.FPostId,
                UserId = post.FUserId,
                UserName = post.FUser.FUsername,
                UserImage = post.FUser.FImage,
                Title = post.FTitle,
                Likes = post.FLikes,
                Views = post.FViews,
                PostDate = post.FPostDate,
                SortId = post.FSortId,
                IsLikedByCurrentUser = post.TPostLikes.Any(l => l.FUserId == currentUserId),
                Blocks = post.TPostBlockTables
                    .OrderBy(b => b.FSortOrder)
                    .Select(b => new PostBlockDto
                    {
                        PostBlockId = b.FPostBlockId,
                        BlockType = b.FBlockType,
                        Content = b.FContent,
                        MediaUrl = b.FMediaUrl,
                        SortOrder = b.FSortOrder
                    }).ToList()
            };
        }

        public async Task<int> CreatePostAsync(CreateOrUpdatePostDto dto, int userId)
        {
            var newPost = new TPostTable
            {
                FUserId = userId,
                FTitle = dto.Title,
                FSortId = dto.SortId,
                FLikes = 0,
                FViews = 0,
                FPostDate = DateTime.Now,
                FPostState = 1
            };

            _context.TPostTables.Add(newPost);
            await _context.SaveChangesAsync();

            int order = 1;
            foreach (var b in dto.Blocks)
            {
                _context.TPostBlockTables.Add(new TPostBlockTable
                {
                    FPostId = newPost.FPostId,
                    FBlockType = b.BlockType,
                    FContent = b.Content,
                    FMediaUrl = b.MediaUrl,
                    FSortOrder = order++,
                    FCreateDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return newPost.FPostId;
        }

        public async Task<bool> UpdatePostAsync(int postId, CreateOrUpdatePostDto dto, int userId)
        {
            var post = await _context.TPostTables
                .Include(p => p.TPostBlockTables)
                .FirstOrDefaultAsync(p => p.FPostId == postId && p.FUserId == userId);

            if (post == null) return false;

            post.FTitle = dto.Title;
            post.FSortId = dto.SortId;

            _context.TPostBlockTables.RemoveRange(post.TPostBlockTables);

            int order = 1;
            foreach (var b in dto.Blocks)
            {
                _context.TPostBlockTables.Add(new TPostBlockTable
                {
                    FPostId = post.FPostId,
                    FBlockType = b.BlockType,
                    FContent = b.Content,
                    FMediaUrl = b.MediaUrl,
                    FSortOrder = order++,
                    FCreateDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePostAsync(int postId, int userId)
        {
            var post = await _context.TPostTables.FirstOrDefaultAsync(p => p.FPostId == postId && p.FUserId == userId);
            if (post == null) return false;

            post.FPostState = 0;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLikePostAsync(int postId, int userId)
        {
            var post = await _context.TPostTables.FindAsync(postId);
            if (post == null) return false;

            var existingLike = await _context.TPostLikes.FirstOrDefaultAsync(l => l.FPostId == postId && l.FUserId == userId);
            if (existingLike != null)
            {
                _context.TPostLikes.Remove(existingLike);
                post.FLikes = Math.Max(0, post.FLikes - 1);
            }
            else
            {
                _context.TPostLikes.Add(new TPostLike { FPostId = postId, FUserId = userId });
                post.FLikes += 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
