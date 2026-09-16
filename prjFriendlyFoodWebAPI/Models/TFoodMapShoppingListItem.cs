using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapShoppingListItem
{
    public int FShoppingItemId { get; set; }

    public int FShoppingListId { get; set; }

    public int FIngredientId { get; set; }

    public decimal FQuantity { get; set; }

    public string FUnit { get; set; } = null!;

    public bool FIsPurchased { get; set; }

    public string? FNote { get; set; }

    public DateTime FCreatedTime { get; set; }

    public DateTime? FUpdatedTime { get; set; }

    public virtual TFoodMapShoppingList FShoppingList { get; set; } = null!;
}
