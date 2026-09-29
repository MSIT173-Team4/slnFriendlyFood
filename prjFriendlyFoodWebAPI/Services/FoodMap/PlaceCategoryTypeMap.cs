using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    // 店家分類 ID → Google Place Type 的唯一來源。
    // 以前 PlaceService（"食品"、"賣場"…中文值，Google 不認得）和
    // TripPlanningService 各自寫死一份對照表，兩邊還對不起來。
    // 現在優先讀 tFoodMapPlaceCategory.fGooglePlaceType，
    // 資料庫沒填或填的不是合法 Google 類型，才退回下面的預設值。
    internal static partial class PlaceCategoryTypeMap
    {
        private const string CacheKey = "foodmap:place-category-types";

        // ⚠️ 預設值是依規格推測的分類，請確認資料庫 fPlaceCategoryID 的實際意義，
        // 最好直接把正確的 Google 類型填進 tFoodMapPlaceCategory.fGooglePlaceType。
        private static readonly Dictionary<int, string> Defaults = new()
        {
            [1] = "grocery_store",
            [2] = "supermarket",
            [3] = "warehouse_store",
            [4] = "convenience_store"
        };

        [GeneratedRegex("^[a-z_]+$")]
        private static partial Regex GooglePlaceTypePattern();

        public static async Task<Dictionary<int, string>> LoadAsync(
            FriendlyFoodDbContext context,
            CancellationToken cancellationToken = default)
        {
            if (FoodMapMemoryCache.TryGet<Dictionary<int, string>>(CacheKey, out var cached))
            {
                return cached;
            }

            var map = new Dictionary<int, string>(Defaults);

            var rows = await context.TFoodMapPlaceCategories
                .AsNoTracking()
                .Select(c => new { c.FPlaceCategoryId, c.FGooglePlaceType })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var type = row.FGooglePlaceType?.Trim();

                if (!string.IsNullOrEmpty(type) && GooglePlaceTypePattern().IsMatch(type))
                {
                    map[row.FPlaceCategoryId] = type;
                }
            }

            FoodMapMemoryCache.Set(CacheKey, map, TimeSpan.FromMinutes(10));
            return map;
        }
    }
}
