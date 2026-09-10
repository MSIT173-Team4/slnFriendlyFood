using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TUser
{
    public int FId { get; set; }

    public string FUsername { get; set; }

    public string FPassword { get; set; }

    public string FEmail { get; set; } = null!;

    public string FPhone { get; set; } = null!;

    public string FIdNum { get; set; } = null!;

    public string FAddress { get; set; } = null!;

    public string? FImage { get; set; }

    public bool FIsActive { get; set; }

    public bool FIsAdmin { get; set; }

    public DateTime FCreateTime { get; set; }

    public DateTime? FLastLogin { get; set; }

    public virtual ICollection<TApply> TApplies { get; set; } = new List<TApply>();

    public virtual ICollection<TConversationMember> TConversationMembers { get; set; } = new List<TConversationMember>();

    public virtual ICollection<TFoodMapFavorite> TFoodMapFavorites { get; set; } = new List<TFoodMapFavorite>();

    public virtual ICollection<TFoodMapShoppingList> TFoodMapShoppingLists { get; set; } = new List<TFoodMapShoppingList>();

    public virtual ICollection<TFoodMapTrip> TFoodMapTrips { get; set; } = new List<TFoodMapTrip>();

    public virtual ICollection<TMarketCheckoutBatch> TMarketCheckoutBatches { get; set; } = new List<TMarketCheckoutBatch>();

    public virtual ICollection<TMarketOrder> TMarketOrders { get; set; } = new List<TMarketOrder>();

    public virtual ICollection<TMarketProductFavorite> TMarketProductFavorites { get; set; } = new List<TMarketProductFavorite>();

    public virtual ICollection<TMarketProductReview> TMarketProductReviews { get; set; } = new List<TMarketProductReview>();

    public virtual ICollection<TMarketShoppingCart> TMarketShoppingCarts { get; set; } = new List<TMarketShoppingCart>();

    public virtual ICollection<TMessageTable> TMessageTables { get; set; } = new List<TMessageTable>();

    public virtual ICollection<TPostTable> TPostTables { get; set; } = new List<TPostTable>();

    public virtual ICollection<TSeller> TSellers { get; set; } = new List<TSeller>();
}
