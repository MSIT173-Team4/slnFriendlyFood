using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TMessageLike
{
    public int FMessageLikeId { get; set; }

    public int FMessageId { get; set; }

    public int FUserId { get; set; }

    public virtual TMessageTable FMessage { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
