using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TPostBookmark
{
    public int FBookmarkId { get; set; }

    public int FUserId { get; set; }

    public int FPostId { get; set; }

    public DateTime FBookmarkDate { get; set; }

    public virtual TPostTable FPost { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
