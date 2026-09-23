using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Social;
using System.Security.Claims;

namespace prjFriendlyFoodWebAPI.Controllers.Forum
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PostResponseDto>>> GetPosts([FromQuery] PostQueryParameters queryParams)
        {
            int currentUserId = GetCurrentUserId();
            var result = await _postService.GetPostsAsync(queryParams, currentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            int currentUserId = GetCurrentUserId();
            var post = await _postService.GetPostByIdAsync(id, currentUserId);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreateOrUpdatePostDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var postId = await _postService.CreatePostAsync(dto, currentUserId);
            return Ok(new { postId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] CreateOrUpdatePostDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var updateSuccess = await _postService.UpdatePostAsync(id, dto, currentUserId);
            if (!updateSuccess) return NotFound();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var deleteSuccess = await _postService.DeletePostAsync(id, currentUserId);
            if (!deleteSuccess) return NotFound();
            return Ok();
        }

        [HttpPost("bookmark")]
        public async Task<ActionResult<PostBookmarkStatusDto>> ToggleBookmark([FromBody] BookmarkCreateDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var result = await _postService.ToggleBookmarkAsync(dto.PostId, currentUserId);
            return Ok(result);
        }

        [HttpPost("{postId}/like")]
        public async Task<ActionResult> ToggleLike(int postId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            bool isLiked = await _postService.ToggleLikeAsync(postId, currentUserId);
            return Ok(new { postId, isLiked });
        }
    }
}