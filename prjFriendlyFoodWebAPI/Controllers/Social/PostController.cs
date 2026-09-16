using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Social;

namespace prjFriendlyFoodWebAPI.Controllers.Forum
{
    public class PostController : ControllerBase
    {
        [ApiController]
        [Route("api/[controller]")]
        public class PostsController : ControllerBase
        {
            private readonly IPostService _postService;

            public PostsController(IPostService postService)
            {
                _postService = postService;
            }

            //使用者Id=1(暫定)
            private int CurrentUserId => 1;

            [HttpGet]
            public async Task<IActionResult> GetPosts([FromQuery] string sortBy = "latest", [FromQuery] string? keyword = null)
            {
                var posts = await _postService.GetPostsAsync(sortBy, keyword, CurrentUserId);
                return Ok(posts);
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetPost(int id)
            {
                var post = await _postService.GetPostByIdAsync(id, CurrentUserId);
                if (post == null) return NotFound();
                return Ok(post);
            }

            [HttpPost]
            public async Task<IActionResult> CreatePost([FromBody] CreateOrUpdatePostDto dto)
            {
                var postId = await _postService.CreatePostAsync(dto, CurrentUserId);
                return Ok(new { postId });
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> UpdatePost(int id, [FromBody] CreateOrUpdatePostDto dto)
            {
                var UpdateSuccess = await _postService.UpdatePostAsync(id, dto, CurrentUserId);
                if (!UpdateSuccess) return BadRequest("未知錯誤");
                return Ok();
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeletePost(int id)
            {
                var DeleteSuccess = await _postService.DeletePostAsync(id, CurrentUserId);
                if (!DeleteSuccess) return BadRequest("未知錯誤");
                return Ok();
            }

            [HttpPost("{id}/like")]
            public async Task<IActionResult> ToggleLike(int id)
            {
                var success = await _postService.ToggleLikePostAsync(id, CurrentUserId);
                if (!success) return NotFound();
                return Ok();
            }
        }
    }
}
