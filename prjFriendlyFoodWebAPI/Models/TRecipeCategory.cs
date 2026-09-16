using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeCategory
{
    public int FCategoryId { get; set; }

    public string FCategoryName { get; set; } = null!;

    public short FDisplayOrder { get; set; }
}
