using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using System.Security.Claims;

namespace prjFriendlyFoodWebAPI.Controllers.Chat
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly FriendlyFoodDbContext _db;
        public ChatController(FriendlyFoodDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost("GetOrCreateRoom/{targetUserId:int}")]
        public async Task<IActionResult> GetOrCreateRoom(int targetUserId)
        {
            int currentUserId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            if (currentUserId == targetUserId)
                return BadRequest("不能跟自己聊天");

            int user1Id = Math.Min(currentUserId, targetUserId);
            int user2Id = Math.Max(currentUserId, targetUserId);

            var room = await _db.TChatRooms
                .FirstOrDefaultAsync(r =>
                    r.FUser1Id == user1Id &&
                    r.FUser2Id == user2Id
                );

            if (room == null)
            {
                room = new TChatRoom
                {
                    FUser1Id = user1Id,
                    FUser2Id = user2Id,
                    FCreatedTime = DateTime.Now
                };

                _db.TChatRooms.Add(room);
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                roomId = room.FId
            });
        }
        [Authorize]
        [HttpGet("GetRooms")]
        public async Task<IActionResult> GetRooms()
        {
            int currentUserId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );
            var rooms = await _db.TChatRooms
                .Where(r => r.FUser1Id == currentUserId || r.FUser2Id == currentUserId)
                .Select(r => new
                {
                    roomId = r.FId,
                    otherUserId = r.FUser1Id == currentUserId ? r.FUser2Id : r.FUser1Id,
                    otherUsername = _db.TUsers
                    .Where(u =>
                        u.FId ==
                        (r.FUser1Id == currentUserId
                            ? r.FUser2Id
                            : r.FUser1Id)
                    )
                    .Select(u => u.FUsername)
                    .FirstOrDefault(),
                    createdTime = r.FCreatedTime
                })
                .ToListAsync();
            return Ok(rooms);
        }
        [Authorize]
        [HttpGet("GetMessages/{roomId:int}")]
        public async Task<IActionResult> GetMessages(int roomId)
        {
            int currentUserId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            bool allowed = await _db.TChatRooms
                .AnyAsync(r =>
                    r.FId == roomId &&
                    (
                        r.FUser1Id == currentUserId ||
                        r.FUser2Id == currentUserId
                    )
                );
            if (!allowed) return Forbid();
            var messages = await _db.TChats
            .Where(m => m.FChatRoomId == roomId)
            .OrderBy(m => m.FCreatedTime)
            .Select(m => new
            {
                roomId = m.FChatRoomId,
                senderId = m.FSenderId,
                messageType=m.FMessageType,
                imageUrl=m.FFileUrl,
                content = m.FContent,
                sendTime = m.FCreatedTime
            })
            .ToListAsync();

            return Ok(messages);
        }
        [Authorize]
        [HttpPost("UploadChatImage")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadChatImage(
        [FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new
                {
                    message = "請選擇圖片"
                });
            }

            string extension =
                Path.GetExtension(image.FileName)
                    .ToLowerInvariant();

            string fileName =
                $"{Guid.NewGuid()}{extension}";

            string folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "Chat"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(
                folderPath,
                fileName
            );

            using var stream =
                new FileStream(filePath, FileMode.Create);

            await image.CopyToAsync(stream);

            string imageUrl =
                $"/images/Chat/{fileName}";

            return Ok(new
            {
                imageUrl
            });
        }
    }
}
