namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    public class CreateTripPlacesRequestDTO
    {
        public int FPlaceID { get; set; }

        public int FSortOrder { get; set; }

        // 選填：預覽時這家店是用哪個店家分類找到的（用來計算覆蓋率，跟預覽畫面一致）
        public int? FPlaceCategoryId { get; set; }
    }
}
