using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapIngredient
{
    public int FIngredientId { get; set; }

    public string FName { get; set; } = null!;

    public int FIngredientCategoryId { get; set; }
}
