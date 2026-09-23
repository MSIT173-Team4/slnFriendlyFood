using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRecipeStep
{
    public int FStepId { get; set; }

    public int FRecipeId { get; set; }

    public short FStepNumber { get; set; }

    public string FInstruction { get; set; } = null!;

    public string? FImageUrl { get; set; }

    public int FTimerSeconds { get; set; }
}
