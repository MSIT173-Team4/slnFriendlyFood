using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeFavorite
{
    public int FFavoriteId { get; set; }

    public int FUserId { get; set; }

    public int FRecipeId { get; set; }
}
