namespace prjFriendlyFoodWebAPI.DTOs.FoodMap
{
    // 規劃預覽裡的一間候選店家（地圖上的一個標記）
    public class TripCandidateDTO
    {
        public int FPlaceId { get; set; }
        public string? FGooglePlaceId { get; set; }
        public string FName { get; set; } = string.Empty;
        public string FAddress { get; set; } = string.Empty;
        public decimal FLatitude { get; set; }
        public decimal FLongitude { get; set; }
        public int FPlaceCategoryId { get; set; }
        public decimal? FGoogleRating { get; set; }

        // 平台推薦活動（跟採買符合度是兩回事，前端用不同標籤顯示）
        public bool IsRecommend { get; set; }

        // 最佳化演算法有沒有選這家、建議的造訪順序（1 開始，沒選到為 null）
        public bool IsSuggested { get; set; }
        public int? SuggestedOrder { get; set; }

        // 這家店能買到清單裡的哪些品項（前端拿來即時計算覆蓋率）
        public List<int> MatchedItemIds { get; set; } = [];
        public List<string> MatchedItemNames { get; set; } = [];

        // 跟起點的直線距離（公尺）
        public double DistanceMeters { get; set; }
    }
}
