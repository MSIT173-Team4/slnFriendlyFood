using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Services.Social;

namespace prjFriendlyFoodWebAPI.Controllers.Social
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        private int CurrentUserId => 1;

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var comments = await _commentService.GetCommentsByPostIdAsync(postId, CurrentUserId);
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateOrUpdateCommentDto dto)
        {
            var commentId = await _commentService.CreateCommentAsync(dto, CurrentUserId);
            return Ok(new { commentId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var DeleteSuccess = await _commentService.DeleteCommentAsync(id, CurrentUserId);
            if (!DeleteSuccess) return BadRequest("未知錯誤");
            return Ok();
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var success = await _commentService.ToggleLikeCommentAsync(id, CurrentUserId);
            if (!success) return NotFound();
            return Ok();
        }
    }
}
