using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeTag
{
    public int FTagId { get; set; }

    public string FTagType { get; set; } = null!;

    public string FTagName { get; set; } = null!;
}
