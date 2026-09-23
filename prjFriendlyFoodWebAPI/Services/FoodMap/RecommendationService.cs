using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class RecommendationService : IRecommendationServices
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly IShoppingListMappingService _mappingService;

        public RecommendationService(
            FriendlyFoodDbContext context,
            IShoppingListMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        public async Task<List<PlaceCoverageDTO>> GetRecommendationsAsync(
            int shoppingListId,
            List<PlaceCandidateDTO> candidatePlaces,
            CancellationToken cancellationToken = default)
        {
            // B.2：先取得 A 節算好的「店家分類 -> 可滿足品項」對照
            var mapping = await _mappingService.GetPlaceCategoriesForShoppingListAsync(
                shoppingListId, cancellationToken);

            if (mapping.TotalItemCount == 0 || candidatePlaces.Count == 0)
            {
                return new List<PlaceCoverageDTO>();
            }

            // B.3：查出目前生效中活動涵蓋的 fPlaceId 集合
            var recommendedPlaceIds = await GetActiveRecommendedPlaceIdsAsync(cancellationToken);

            var result = new List<PlaceCoverageDTO>();

            foreach (var place in candidatePlaces)
            {
                var matchedItems = mapping.ItemsByPlaceCategory
                    .GetValueOrDefault(place.FPlaceCategoryId, new List<ShoppingListItemDTO>());

                var coveragePercentage = (double)matchedItems.Count / mapping.TotalItemCount;

                result.Add(new PlaceCoverageDTO
                {
                    FPlaceId = place.FPlaceId,
                    FName = place.FName,
                    CoveragePercentage = coveragePercentage,
                    MatchedItemNames = matchedItems.Select(i => i.FIngredientName).ToList(),
                    // 注意：IsRecommend（平台活動）跟 CoveragePercentage（採買符合度）是兩件獨立的事，
                    // 前端顯示時請用兩個獨立標籤，不要合併成單一排序依據
                    IsRecommend = recommendedPlaceIds.Contains(place.FPlaceId)
                });
            }

            // 依 CoveragePercentage 由高到低排序
            return result
                .OrderByDescending(r => r.CoveragePercentage)
                .ToList();
        }

        public async Task<HashSet<int>> GetActiveRecommendedPlaceIdsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // 1. 篩出「目前生效中」的活動
            var activeCampaignIds = await _context.TFoodMapRecommendationCampaigns
                .Where(c => c.FIsActive
                            && c.FStartDate <= now
                            && c.FEndDate >= now)
                .Select(c => c.FCampaignId)
                .ToListAsync(cancellationToken);

            if (activeCampaignIds.Count == 0)
            {
                return new HashSet<int>();
            }

            // 2. 找出這些活動底下涵蓋的 fPlaceId
            var placeIds = await _context.TFoodMapRecommendationPlaces
                .Where(rp => activeCampaignIds.Contains(rp.FCampaignId))
                .Select(rp => rp.FPlaceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return placeIds.ToHashSet();
        }
    }
}

