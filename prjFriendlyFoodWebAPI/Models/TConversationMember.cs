using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TConversationMember
{
    public int FConversationId { get; set; }

    public int FUserId { get; set; }

    public int? FLastReadMessageId { get; set; }

    public DateTime? FLastReadDate { get; set; }

    public virtual TConversationsTable FConversation { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
