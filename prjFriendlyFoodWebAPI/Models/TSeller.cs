using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TSeller
{
    public int FId { get; set; }

    public int FUserId { get; set; }

    public string FSellerName { get; set; } = null!;

    public string? FDescription { get; set; }

    public int FStatus { get; set; }

    public DateTime FApplyDate { get; set; }

    public virtual TStatus FStatusNavigation { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;

    public virtual ICollection<TMarketCoupon> TMarketCoupons { get; set; } = new List<TMarketCoupon>();

    public virtual ICollection<TMarketOrder> TMarketOrders { get; set; } = new List<TMarketOrder>();

    public virtual ICollection<TMarketProduct> TMarketProducts { get; set; } = new List<TMarketProduct>();

    public virtual ICollection<TMarketShoppingCart> TMarketShoppingCarts { get; set; } = new List<TMarketShoppingCart>();
}
