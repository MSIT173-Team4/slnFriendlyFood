using System.ComponentModel.DataAnnotations;
namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketPublicProductListDto
    {
        /// <summary>
        /// 產品序號
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 產品名稱
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 產品描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// 單價
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        public string BrandOrOrigin { get; set; }

        /// <summary>
        /// 生產日期
        /// </summary>
        public DateOnly? ManufacturingDate { get; set; }

        /// <summary>
        /// 有效期限
        /// </summary>
        public DateOnly? ExpirationDate { get; set; }

        /// <summary>
        /// 商品狀態；0 審核中 / 1 架上商品 / 2 已售完 / 3 未上架 / 4 已違規
        /// </summary>
        public byte ProductStatus { get; set; }

        /// <summary>
        /// 商品圖片 URL 列表
        /// </summary>
        public List<string>? ImageUrls { get; set; }
        /// <summary>
        /// 目前登入者是否已收藏（未登入一律 false）
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 是否為目前登入者自己賣場的商品（未登入一律 false）
        /// </summary>
        public bool IsOwnProduct { get; set; }
    }
}
