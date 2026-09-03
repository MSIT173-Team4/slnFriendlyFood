using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class MarketProductCreateDto
    {
        /// <summary>
        /// 產品類別編號
        /// </summary>
        [Required(ErrorMessage = "產品類別必填")]
        public string FProductsCategoryNo { get; set; }

        /// <summary>
        /// 產品名稱
        /// </summary>
        [Required(ErrorMessage = "產品名稱必填")]
        [StringLength(100, ErrorMessage = "產品名稱不可超過 100 字")]
        public string FProductname { get; set; }

        /// <summary>
        /// 產品描述
        /// </summary>
        public string FDescription { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "庫存不可為負數")]
        public int FStock { get; set; }


        /// <summary>
        /// 單價
        /// </summary>
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "售價必須大於 0")]
        public decimal FPrice { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        public string FBrandOrOrigin { get; set; }

        /// <summary>
        /// 生產日期
        /// </summary>
        public DateOnly FManufacturingDate { get; set; }

        /// <summary>
        /// 有效期限
        /// </summary>
        public DateOnly? FExpirationDate { get; set; }

        /// <summary>
        /// 商品圖片 URL 列表
        /// </summary>
        public List<string>? ImageUrls { get; set; }
    }
}
