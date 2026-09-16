using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeTagMapping
{
    public int FMappingId { get; set; }

    public int FRecipeId { get; set; }

    public int FTagId { get; set; }
}
