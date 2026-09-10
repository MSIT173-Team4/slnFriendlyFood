using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeLike
{
    public int FLikeId { get; set; }

    public int FUserId { get; set; }

    public int FRecipeId { get; set; }
}
