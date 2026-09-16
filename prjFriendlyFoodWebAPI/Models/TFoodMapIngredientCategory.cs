using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapIngredientCategory
{
    public int FIngredientCategoryId { get; set; }

    public string FCategoryName { get; set; } = null!;

    public string? FDescription { get; set; }

    public DateTime FCreatedTime { get; set; }

    public virtual ICollection<TFoodMapIngredientPlaceCategory> TFoodMapIngredientPlaceCategories { get; set; } = new List<TFoodMapIngredientPlaceCategory>();
}
