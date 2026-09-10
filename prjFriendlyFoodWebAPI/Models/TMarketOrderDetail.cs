using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 訂單明細
/// </summary>
public partial class TMarketOrderDetail
{
    /// <summary>
    /// 訂單明細ID
    /// </summary>
    public int FOrderDetailsId { get; set; }

    /// <summary>
    /// 訂單ID
    /// </summary>
    public long FOrderId { get; set; }

    /// <summary>
    /// 產品編號
    /// </summary>
    public int FProductId { get; set; }

    /// <summary>
    /// 產品數量
    /// </summary>
    public int FQuantity { get; set; }

    /// <summary>
    /// 產品單價
    /// </summary>
    public decimal FUnitPrice { get; set; }

    public virtual TMarketOrder FOrder { get; set; } = null!;

    public virtual TMarketProduct FProduct { get; set; } = null!;

    public virtual TMarketProductReview? TMarketProductReview { get; set; }
}
