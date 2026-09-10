using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 商品主檔
/// </summary>
public partial class TMarketProduct
{
    /// <summary>
    /// 產品序號
    /// </summary>
    public int FProductId { get; set; }

    /// <summary>
    /// 產品編號
    /// </summary>
    public string FProductNo { get; set; } = null!;

    /// <summary>
    /// 商家編號
    /// </summary>
    public int FSellerId { get; set; }

    /// <summary>
    /// 產品類別編號
    /// </summary>
    public string FProductsCategoryNo { get; set; } = null!;

    /// <summary>
    /// 產品名稱
    /// </summary>
    public string FProductname { get; set; } = null!;

    /// <summary>
    /// 產品描述
    /// </summary>
    public string? FDescription { get; set; }

    /// <summary>
    /// 數量
    /// </summary>
    public int FStock { get; set; }

    /// <summary>
    /// 樂觀並行鎖
    /// </summary>
    public byte[] FRowVersion { get; set; } = null!;

    /// <summary>
    /// 單價
    /// </summary>
    public decimal FPrice { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    public string? FBrandOrOrigin { get; set; }

    /// <summary>
    /// 生產日期
    /// </summary>
    public DateOnly FManufacturingDate { get; set; }

    /// <summary>
    /// 有效期限
    /// </summary>
    public DateOnly? FExpirationDate { get; set; }

    /// <summary>
    /// 上架日期
    /// </summary>
    public DateTime FProductDate { get; set; }

    /// <summary>
    /// 商品狀態；0 審核中 / 1 架上商品 / 2 已售完 / 3 未上架 / 4 已違規
    /// </summary>
    public byte FProductStatus { get; set; }

    /// <summary>
    /// 被檢舉次數
    /// </summary>
    public int FReportCount { get; set; }

    public virtual TMarketProductCategory FProductsCategoryNoNavigation { get; set; } = null!;

    public virtual TSeller FSeller { get; set; } = null!;

    public virtual ICollection<TMarketOrderDetail> TMarketOrderDetails { get; set; } = new List<TMarketOrderDetail>();

    public virtual ICollection<TMarketProductFavorite> TMarketProductFavorites { get; set; } = new List<TMarketProductFavorite>();

    public virtual ICollection<TMarketProductImage> TMarketProductImages { get; set; } = new List<TMarketProductImage>();

    public virtual ICollection<TMarketProductReview> TMarketProductReviews { get; set; } = new List<TMarketProductReview>();

    public virtual ICollection<TMarketShoppingCart> TMarketShoppingCarts { get; set; } = new List<TMarketShoppingCart>();
}
