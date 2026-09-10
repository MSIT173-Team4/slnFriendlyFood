using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeIngredient
{
    public int FRecipeIngredientId { get; set; }

    public int FRecipeId { get; set; }

    public int FIngredientId { get; set; }

    public string FDisplayAmount { get; set; } = null!;

    public decimal? FBaseAmount { get; set; }

    public string? FStandardUnit { get; set; }

    public bool FIsMain { get; set; }

    public short FSortOrder { get; set; }
}
