using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 商品收藏
/// </summary>
public partial class TMarketProductFavorite
{
    /// <summary>
    /// 收藏ID
    /// </summary>
    public int FFavoriteId { get; set; }

    /// <summary>
    /// 會員編號
    /// </summary>
    public int FUserId { get; set; }

    /// <summary>
    /// 產品編號
    /// </summary>
    public int FProductId { get; set; }

    /// <summary>
    /// 加入時間
    /// </summary>
    public DateTime FCreatedDate { get; set; }

    public virtual TMarketProduct FProduct { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
