using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 購物車
/// </summary>
public partial class TMarketShoppingCart
{
    /// <summary>
    /// 明細唯一識別碼
    /// </summary>
    public int FCartItemId { get; set; }

    /// <summary>
    /// 會員/買家 ID
    /// </summary>
    public int FUserId { get; set; }

    /// <summary>
    /// 商家編號
    /// </summary>
    public int FSellerId { get; set; }

    /// <summary>
    /// 商品 ID
    /// </summary>
    public int FProductId { get; set; }

    /// <summary>
    /// 購買數量
    /// </summary>
    public int FQuantity { get; set; }

    /// <summary>
    /// 加入時間
    /// </summary>
    public DateTime FCreatedDate { get; set; }

    public virtual TMarketProduct FProduct { get; set; } = null!;

    public virtual TSeller FSeller { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
