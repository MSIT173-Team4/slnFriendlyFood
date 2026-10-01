using System.ComponentModel.DataAnnotations;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // POST /api/places/nearby 的 body
    public class NearbyRequestDTO
    {
        [Range(-90, 90)]
        public double FLatitude { get; set; }

        [Range(-180, 180)]
        public double FLongitude { get; set; }

        public int? FPlacesCategoryId { get; set; }

        public int MinimumRequests { get; set; } = 5;

        // 有帶就用指定半徑；沒帶維持原本「先 1 公里、不足 5 家再擴大到 3 公里」的邏輯
        [Range(100, 20000)]
        public int? RadiusMeters { get; set; }
    }
}
