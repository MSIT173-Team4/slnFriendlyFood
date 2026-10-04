using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Services.Social;

namespace prjFriendlyFoodWebAPI.Controllers.Social
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        private int? CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        [HttpGet("post/{postId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComments(int postId)
        {
            int userId = CurrentUserId ?? 0;
            var comments = await _commentService.GetCommentsByPostIdAsync(postId, userId);
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateOrUpdateCommentDto dto)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized("請重新登入");
            }

            var commentId = await _commentService.CreateCommentAsync(dto, userId.Value);
            return Ok(new { commentId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized("請重新登入");
            }

            var deleteSuccess = await _commentService.DeleteCommentAsync(id, userId.Value);
            if (!deleteSuccess) return BadRequest("刪除失敗");

            return Ok();
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized("請重新登入");
            }

            var success = await _commentService.ToggleLikeCommentAsync(id, userId.Value);
            if (!success) return NotFound("操作失敗");

            return Ok();
        }
    }
}