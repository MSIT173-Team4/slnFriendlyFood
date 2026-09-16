using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapIngredientPlaceCategory
{
    public int FMappingId { get; set; }

    public int FIngredientCategoryId { get; set; }

    public int FPlaceCategoryId { get; set; }

    public int FPriority { get; set; }

    public bool FIsActive { get; set; }

    public DateTime FCreatedTime { get; set; }

    public virtual TFoodMapIngredientCategory FIngredientCategory { get; set; } = null!;

    public virtual TFoodMapPlaceCategory FPlaceCategory { get; set; } = null!;
}
