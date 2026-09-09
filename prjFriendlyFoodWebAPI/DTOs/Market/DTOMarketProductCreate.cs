using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class DTOMarketProductCreate
    {
        [Required(ErrorMessage = "產品類別必填")]
        public string ProductsCategoryNo { get; set; }

        [Required(ErrorMessage = "產品名稱必填")]
        [StringLength(100, ErrorMessage = "產品名稱不可超過 100 字")]
        public string ProductName { get; set; }

        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "庫存不可為負數")]
        public int Stock { get; set; }

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "售價必須大於 0")]
        public decimal Price { get; set; }

        public string? BrandOrOrigin { get; set; }

        public DateOnly? ManufacturingDate { get; set; }

        public DateOnly? ExpirationDate { get; set; }

        // 改成接收實際檔案，允許多張，非必填
        public List<IFormFile>? Images { get; set; }
    }
}
