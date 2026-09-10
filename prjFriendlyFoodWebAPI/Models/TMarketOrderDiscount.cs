using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 訂單折扣快照(結帳當下優惠券套用紀錄)
/// </summary>
public partial class TMarketOrderDiscount
{
    /// <summary>
    /// 明細 PK
    /// </summary>
    public int FOrderDiscountId { get; set; }

    /// <summary>
    /// 訂單 ID
    /// </summary>
    public long FOrderId { get; set; }

    /// <summary>
    /// 優惠券 ID
    /// </summary>
    public int FCouponId { get; set; }

    /// <summary>
    /// 活動名稱快照
    /// </summary>
    public string FDiscountName { get; set; } = null!;

    /// <summary>
    /// 活動適用範圍快照
    /// </summary>
    public string FDiscountScope { get; set; } = null!;

    /// <summary>
    /// 折扣類型快照
    /// </summary>
    public string FDiscountType { get; set; } = null!;

    /// <summary>
    /// 該次優惠實際折抵金額
    /// </summary>
    public decimal FAppliedAmount { get; set; }

    public virtual TMarketCoupon FCoupon { get; set; } = null!;

    public virtual TMarketOrder FOrder { get; set; } = null!;
}
