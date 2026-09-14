using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Model;

public partial class TRecipeFavorite
{
    public int FFavoriteId { get; set; }

    public int FUserId { get; set; }

    public int FRecipeId { get; set; }

    public virtual TUser FUser { get; set; } = null!;
}
