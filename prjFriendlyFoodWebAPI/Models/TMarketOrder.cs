using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

/// <summary>
/// 訂單主檔(依賣家分組後的子訂單)
/// </summary>
public partial class TMarketOrder
{
    /// <summary>
    /// 訂單序號
    /// </summary>
    public long FOrderId { get; set; }

    /// <summary>
    /// 訂單編號
    /// </summary>
    public string FOrderNo { get; set; } = null!;

    /// <summary>
    /// 會員編號
    /// </summary>
    public int FUserId { get; set; }

    /// <summary>
    /// 商家編號
    /// </summary>
    public int FSellerId { get; set; }

    /// <summary>
    /// 訂單日期
    /// </summary>
    public DateTime FOrderDate { get; set; }

    /// <summary>
    /// 原始運費
    /// </summary>
    public decimal FShippingFee { get; set; }

    /// <summary>
    /// 運費折抵金額
    /// </summary>
    public decimal FShippingDiscount { get; set; }

    /// <summary>
    /// 商品折抵金額
    /// </summary>
    public decimal FProductDiscount { get; set; }

    /// <summary>
    /// 應付總金額
    /// </summary>
    public decimal FTotalAmount { get; set; }

    /// <summary>
    /// 收件人姓名
    /// </summary>
    public string FRecipientName { get; set; } = null!;

    /// <summary>
    /// 收件人電話
    /// </summary>
    public string FRecipientPhone { get; set; } = null!;

    /// <summary>
    /// 收件地址
    /// </summary>
    public string FShippingAddress { get; set; } = null!;

    /// <summary>
    /// 配送方式
    /// </summary>
    public string FShippingMethod { get; set; } = null!;

    /// <summary>
    /// 賣家是否已確認/列印出貨單；0 未確認 1 已確認/已列印
    /// </summary>
    public bool FIsShippingConfirmed { get; set; }

    /// <summary>
    /// 訂單狀態；0 待處理 / 1 已成立 / 2 已完成 / 3 已取消
    /// </summary>
    public int FOrderStatus { get; set; }

    /// <summary>
    /// 付款狀態；0 待付款 / 1 已付款 / 2 待退款 / 3 已退款
    /// </summary>
    public int FPaymentStatus { get; set; }

    /// <summary>
    /// 運送狀態；0 待出貨 / 1 運送中 / 2 已送達 / 3 運送失敗 / 4 退回包裹運送中 / 5 賣家已取回退件
    /// </summary>
    public int FShippingStatus { get; set; }

    /// <summary>
    /// 取消狀態；0 無取消申請 / 1 待回覆 / 2 已取消 / 3 拒絕取消
    /// </summary>
    public int FCancellationStatus { get; set; }

    /// <summary>
    /// 退貨狀態；0 無退貨 / 1 待處理 / 2 已處理
    /// </summary>
    public int FReturnStatus { get; set; }

    /// <summary>
    /// 批次ID
    /// </summary>
    public long FBatchId { get; set; }

    public virtual TMarketCheckoutBatch FBatch { get; set; } = null!;

    public virtual TSeller FSeller { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;

    public virtual ICollection<TMarketOrderDetail> TMarketOrderDetails { get; set; } = new List<TMarketOrderDetail>();

    public virtual ICollection<TMarketOrderDiscount> TMarketOrderDiscounts { get; set; } = new List<TMarketOrderDiscount>();
}
