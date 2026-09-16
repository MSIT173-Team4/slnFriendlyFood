using prjFriendlyFoodWebAPI.Models;
using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    public class CommentDto
    {
        public int MessageId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserImage { get; set; }
        public int? ReplyMessageId { get; set; }
        public string? ReplyToUserName { get; set; }
        public string MessageContent { get; set; } = string.Empty;
        public int Likes { get; set; }
        public DateTime MessageDate { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
