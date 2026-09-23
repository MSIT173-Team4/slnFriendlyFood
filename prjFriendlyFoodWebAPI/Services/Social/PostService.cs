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
            int order = 1;
            foreach (var b in dto.Blocks)
            {
                newPost.TPostBlockTables.Add(new TPostBlockTable
                {
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
                .FirstOrDefaultAsync(p => p.FPostId == postId && p.FUserId == userId && p.FPostState == 1);

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
            var post = await _context.TPostTables
                .FirstOrDefaultAsync(p => p.FPostId == postId && p.FUserId == userId && p.FPostState == 1);
            if (post == null) return false;
            post.FPostState = 0;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResultDto<PostResponseDto>> GetPostsAsync(PostQueryParameters queryParams, int currentUserId)
        {
            var query = _context.TPostTables.Where(p => p.FPostState == 1).AsQueryable();

            //關鍵字搜尋
            if (!string.IsNullOrWhiteSpace(queryParams.Keyword))
            {
                string KeyWord = queryParams.Keyword.Trim();
                query = query.Where(p => p.FTitle.Contains(KeyWord) || p.FUser.FUsername.Contains(KeyWord));
            }

            switch (queryParams.Tab?.ToLower())
            {
                case "popular":
                    //最熱門
                    query = query.OrderByDescending(p => (p.FLikes * 10) + p.FViews);
                    break;

                case "my":
                    //我的文章
                    query = query.Where(p => p.FUserId == currentUserId)
                                 .OrderByDescending(p => p.FPostDate);
                    break;

                case "bookmark":
                    //喜歡的文章
                    query = query.Where(p => _context.TPostBookmarks.Any(b => b.FPostId == p.FPostId && b.FUserId == currentUserId))
                                 .OrderByDescending(p => p.FPostDate);
                    break;

                case "latest":
                default:
                    //最新
                    query = query.OrderByDescending(p => p.FPostDate);
                    break;
            }

            //總筆數
            int totalCount = await query.CountAsync();

            //分頁
            var items = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(p => new PostResponseDto
                {
                    PostId = p.FPostId,
                    Title = p.FTitle,
                    //摘要
                    SummaryText = p.TPostBlockTables.Where(b => b.FBlockType == "text").Select(b => b.FContent).FirstOrDefault() ?? "",
                    UserId = p.FUserId,
                    UserName = p.FUser.FUsername,
                    UserImage = p.FUser.FImage,
                    Likes = p.FLikes,
                    Views = p.FViews,
                    CommentCount = p.TMessageTables.Count(),
                    PostDate = p.FPostDate,
                    IsLikedByCurrentUser = _context.TPostLikes.Any(l => l.FPostId == p.FPostId && l.FUserId == currentUserId),
                    IsBookmarkedByCurrentUser = _context.TPostBookmarks.Any(b => b.FPostId == p.FPostId && b.FUserId == currentUserId)
                })
                .ToListAsync();

            return new PagedResultDto<PostResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = queryParams.Page,
                PageSize = queryParams.PageSize
            };
        }

        //收藏狀態
        public async Task<PostBookmarkStatusDto> ToggleBookmarkAsync(int postId, int currentUserId)
        {
            var existing = await _context.TPostBookmarks
                .FirstOrDefaultAsync(b => b.FPostId == postId && b.FUserId == currentUserId);

            bool isBookmarked;
            if (existing != null)
            {
                _context.TPostBookmarks.Remove(existing);
                isBookmarked = false;
            }
            else
            {
                _context.TPostBookmarks.Add(new TPostBookmark
                {
                    FPostId = postId,
                    FUserId = currentUserId,
                    FBookmarkDate = DateTime.Now
                });
                isBookmarked = true;
            }

            await _context.SaveChangesAsync();

            return new PostBookmarkStatusDto
            {
                PostId = postId,
                IsBookmarked = isBookmarked
            };
        }

        //按讚狀態
        public async Task<bool> ToggleLikeAsync(int postId, int currentUserId)
        {
            var post = await _context.TPostTables.FindAsync(postId);
            if (post == null) return false;

            var existing = await _context.TPostLikes
                .FirstOrDefaultAsync(l => l.FPostId == postId && l.FUserId == currentUserId);

            bool isLiked;
            if (existing != null)
            {
                _context.TPostLikes.Remove(existing);
                post.FLikes = Math.Max(0, post.FLikes - 1);
                isLiked = false;
            }
            else
            {
                _context.TPostLikes.Add(new TPostLike { FPostId = postId, FUserId = currentUserId });
                post.FLikes += 1;
                isLiked = true;
            }

            await _context.SaveChangesAsync();
            return isLiked;
        }
    }
}
