using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TConversationsTable
{
    public int FConversationId { get; set; }

    public DateTime FCreatedDate { get; set; }

    public DateTime FUpdatedDate { get; set; }

    public virtual ICollection<TConversationMember> TConversationMembers { get; set; } = new List<TConversationMember>();

    public virtual ICollection<TConversationMessagesTable> TConversationMessagesTables { get; set; } = new List<TConversationMessagesTable>();
}
