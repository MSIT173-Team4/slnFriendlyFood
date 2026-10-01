using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // POST /api/places/search：用地名/店名搜尋（Google Places Text Search）
    public class PlaceSearchRequestDTO
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Query { get; set; } = string.Empty;

        // 有帶座標時，搜尋結果會優先顯示這附近的地點
        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        [Range(100, 50000)]
        public int? RadiusMeters { get; set; }
    }
}
