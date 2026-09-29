using System.ComponentModel.DataAnnotations;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;

namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // POST /api/trips/plan/confirm：使用者選好店家、排好順序、取好名字後才送出，這時才寫進資料庫
    public class ConfirmTripRequestDTO
    {
        [Required(ErrorMessage = "請輸入行程名稱")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "行程名稱需在 1 到 100 字之間")]
        public string FTripName { get; set; } = string.Empty;

        // 有帶的話會檢查清單是不是自己的，並用來計算最後的覆蓋率
        public int? ShoppingListId { get; set; }

        public GoogleTravelMode TravelMode { get; set; } = GoogleTravelMode.Drive;

        [Required, MinLength(1, ErrorMessage = "行程至少要有一個地點")]
        public List<CreateTripPlacesRequestDTO> Places { get; set; } = [];
    }
}
