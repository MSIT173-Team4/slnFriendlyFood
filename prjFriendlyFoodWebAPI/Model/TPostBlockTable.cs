using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Model;

public partial class TPostBlockTable
{
    public int FPostBlockId { get; set; }

    public int FPostId { get; set; }

    public string FBlockType { get; set; } = null!;

    public string? FContent { get; set; }

    public string? FMediaUrl { get; set; }

    public int FSortOrder { get; set; }

    public DateTime FCreateDate { get; set; }

    public virtual TPostTable FPost { get; set; } = null!;
}
