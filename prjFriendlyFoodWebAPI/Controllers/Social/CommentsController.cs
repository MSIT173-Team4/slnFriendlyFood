using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Services.Social;
using System.Security.Claims;

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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            int currentUserId = GetCurrentUserId();
            var comments = await _commentService.GetCommentsByPostIdAsync(postId, currentUserId);
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateOrUpdateCommentDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            if (string.IsNullOrWhiteSpace(dto.MessageContent))
                return BadRequest("請輸入內容");

            try
            {
                var commentId = await _commentService.CreateCommentAsync(dto, currentUserId);
                return Ok(new { commentId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(
            int id,
            [FromBody] CreateOrUpdateCommentDto dto)
        {
            int currentUserId = GetCurrentUserId();

            if (currentUserId == 0)
                return Unauthorized("尚未登入");

            if (string.IsNullOrWhiteSpace(dto.MessageContent))
                return BadRequest("請輸入內容");

            try
            {
                var updateSuccess = await _commentService.UpdateCommentAsync(
                    id,
                    dto,
                    currentUserId);

                if (!updateSuccess)
                    return BadRequest("編輯失敗");

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var deleteSuccess = await _commentService.DeleteCommentAsync(id, currentUserId);
            if (!deleteSuccess) return BadRequest("刪除失敗");

            return Ok();
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var success = await _commentService.ToggleLikeCommentAsync(id, currentUserId);
            if (!success) return NotFound("操作失敗");

            return Ok();
        }
    }
}