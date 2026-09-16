using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TConversationMessagesTable
{
    public int FId { get; set; }

    public int FConversationId { get; set; }

    public int FSenderId { get; set; }

    public string FMessageType { get; set; } = null!;

    public string FContent { get; set; } = null!;

    public DateTime FCreatedDate { get; set; }

    public DateTime FUpdatedDate { get; set; }

    public DateTime? FDeletedDate { get; set; }

    public virtual TConversationsTable FConversation { get; set; } = null!;

    public virtual ICollection<TAttachmentTable> TAttachmentTables { get; set; } = new List<TAttachmentTable>();
}
