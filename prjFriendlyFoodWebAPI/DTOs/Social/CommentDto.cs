using prjFriendlyFoodWebAPI.Models;
using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.DTOs.Social
{
    /*留言的DTO，包含留言ID、貼文ID、使用者ID、
     * 使用者名稱、使用者圖片、回覆留言ID、回覆對象名稱、
     * 留言內容、按讚數、留言日期和是否已被當前使用者按讚*/
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
