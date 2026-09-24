using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TPostLike
{
    public int FPostLikeId { get; set; }

    public int FPostId { get; set; }

    public int FUserId { get; set; }

    public virtual TPostTable FPost { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
