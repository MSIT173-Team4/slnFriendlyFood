using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeUserPantry
{
    public int FPantryId { get; set; }

    public int FUserId { get; set; }

    public int FIngredientId { get; set; }

    public decimal FAmount { get; set; }

    public string FUnit { get; set; } = null!;

    public DateOnly FExpirationDate { get; set; }

    public DateTime FCreatedAt { get; set; }
}
