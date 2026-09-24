using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;
using System.Xml.Linq;
using static prjFriendlyFoodWebAPI.Services.FoodMap.RecommendationService;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class TripOptimizationService : ITripOptimizationService
    {
        private readonly IShoppingListMappingService _mappingService;

        public TripOptimizationService(IShoppingListMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        public async Task<OptimizedTripResult> OptimizeAsync(
            int shoppingListId,
            decimal originLatitude,
            decimal originLongitude,
            List<PlaceCandidateWithLocationDto> candidatePlaces,
            CancellationToken cancellationToken = default)
        {
            var mapping = await _mappingService.GetPlaceCategoriesForShoppingListAsync(
                shoppingListId, cancellationToken);

            // 全部要買的品項（用 ShoppingListItemId 當唯一鍵，
            // 因為同一個 Item 可能同時出現在多個店家分類底下）
            var uncoveredItemIds = mapping.ItemsByPlaceCategory
                .SelectMany(kv => kv.Value)
                .Select(i => i.FShoppingListItemId)
                .Distinct()
                .ToHashSet();

            var remainingCandidates = new List<PlaceCandidateWithLocationDto>(candidatePlaces);
            var selectedPlaces = new List<PlaceCoverageDTO>();

            var currentLat = (double)originLatitude;
            var currentLng = (double)originLongitude;

            // ---------------- 第一階段：Greedy Set Cover ----------------
            while (uncoveredItemIds.Count > 0 && remainingCandidates.Count > 0)
            {
                PlaceCandidateWithLocationDto? bestPlace = null;
                var bestMatchedItems = new List<ShoppingListItemDTO>();
                var bestDistance = double.MaxValue;

                foreach (var candidate in remainingCandidates)
                {
                    var itemsAtThisPlace = mapping.ItemsByPlaceCategory
                        .GetValueOrDefault(candidate.FPlaceCategoryId, new List<ShoppingListItemDTO>())
                        .Where(i => uncoveredItemIds.Contains(i.FShoppingListItemId))
                        .ToList();

                    if (itemsAtThisPlace.Count == 0)
                    {
                        continue;
                    }

                    var distance = HaversineDistanceMeters(
                        currentLat, currentLng, candidate.Latitude, candidate.Longitude);

                    // 選「能滿足最多 uncoveredItems」的店；打平時選離目前位置最近的
                    var isBetter = bestPlace is null
                        || itemsAtThisPlace.Count > bestMatchedItems.Count
                        || (itemsAtThisPlace.Count == bestMatchedItems.Count && distance < bestDistance);

                    if (isBetter)
                    {
                        bestPlace = candidate;
                        bestMatchedItems = itemsAtThisPlace;
                        bestDistance = distance;
                    }
                }

                if (bestPlace is null || bestMatchedItems.Count == 0)
                {
                    // 沒有店能再貢獻新品項，停止（代表有品項附近真的買不到）
                    break;
                }

                selectedPlaces.Add(new PlaceCoverageDTO
                {
                    FPlaceId = bestPlace.FPlaceId,
                    FName = bestPlace.FName,
                    CoveragePercentage = mapping.TotalItemCount == 0
                        ? 0
                        : (double)bestMatchedItems.Count / mapping.TotalItemCount,
                    MatchedItemNames = bestMatchedItems.Select(i => i.FIngredientName).ToList(),
                    // IsRecommend 屬於 B 節（平台活動）的維度，這裡先給 false，
                    // 若要在畫面上同時顯示，把這批 SelectedPlaces 的 FPlaceId
                    // 拿去跟 B 節結果 join 一次即可
                    IsRecommend = false
                });

                foreach (var item in bestMatchedItems)
                {
                    uncoveredItemIds.Remove(item.FShoppingListItemId);
                }

                remainingCandidates.Remove(bestPlace);
                currentLat = bestPlace.Latitude;
                currentLng = bestPlace.Longitude;
            }

            // ---------------- 第二階段：依總移動距離排序 ----------------
            var orderedPlaces = OrderPlacesByDistance(
                selectedPlaces, candidatePlaces, originLatitude, originLongitude);

            var uncoveredItemNames = mapping.ItemsByPlaceCategory
                .SelectMany(kv => kv.Value)
                .Where(i => uncoveredItemIds.Contains(i.FShoppingListItemId))
                .Select(i => i.FIngredientName)
                .Distinct()
                .ToList();

            return new OptimizedTripResult
            {
                PlaceCoverages = orderedPlaces,
                FinalCoveragePercentage = mapping.TotalItemCount == 0
                    ? 0
                    : 1.0 - (double)uncoveredItemIds.Count / mapping.TotalItemCount,
                UncoveredItemNames = uncoveredItemNames
            };
        }

        private List<PlaceCoverageDTO> OrderPlacesByDistance(
            List<PlaceCoverageDTO> selectedPlaces,
            List<PlaceCandidateWithLocationDto> allCandidates,
            decimal originLatitude,
            decimal originLongitude)
        {
            if (selectedPlaces.Count <= 1)
            {
                return selectedPlaces;
            }

            var locationById = allCandidates.ToDictionary(c => c.FPlaceId);
            var origin = ((double)originLatitude, (double)originLongitude);

            // 8 間店以內：窮舉所有排列（8! = 40320，計算量可接受）
            if (selectedPlaces.Count <= 8)
            {
                return FindShortestPermutation(selectedPlaces, locationById, origin);
            }

            // 超過 8 間店：改用最近鄰法，避免排列數量爆炸
            return NearestNeighborOrder(selectedPlaces, locationById, origin);
        }

        private static List<PlaceCoverageDTO> FindShortestPermutation(
            List<PlaceCoverageDTO> places,
            Dictionary<int, PlaceCandidateWithLocationDto> locationById,
            (double Lat, double Lng) origin)
        {
            List<PlaceCoverageDTO>? bestOrder = null;
            var bestDistance = double.MaxValue;

            foreach (var permutation in GetPermutations(places))
            {
                var totalDistance = CalculateTotalDistance(permutation, locationById, origin);
                if (totalDistance < bestDistance)
                {
                    bestDistance = totalDistance;
                    bestOrder = permutation;
                }
            }

            return bestOrder ?? places;
        }

        // 遞迴產生所有排列組合
        private static IEnumerable<List<PlaceCoverageDTO>> GetPermutations(List<PlaceCoverageDTO> items)
        {
            if (items.Count <= 1)
            {
                yield return new List<PlaceCoverageDTO>(items);
                yield break;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var remaining = new List<PlaceCoverageDTO>(items);
                remaining.RemoveAt(i);

                foreach (var permutation in GetPermutations(remaining))
                {
                    permutation.Insert(0, items[i]);
                    yield return permutation;
                }
            }
        }

        private static double CalculateTotalDistance(
            List<PlaceCoverageDTO> orderedPlaces,
            Dictionary<int, PlaceCandidateWithLocationDto> locationById,
            (double Lat, double Lng) origin)
        {
            var total = 0.0;
            var (currentLat, currentLng) = origin;

            foreach (var place in orderedPlaces)
            {
                var location = locationById[place.FPlaceId];
                total += HaversineDistanceMeters(currentLat, currentLng, location.Latitude, location.Longitude);
                currentLat = location.Latitude;
                currentLng = location.Longitude;
            }

            return total;
        }

        // 最近鄰法：從起點開始，每次選距離目前位置最近的下一間店
        private static List<PlaceCoverageDTO> NearestNeighborOrder(
            List<PlaceCoverageDTO> places,
            Dictionary<int, PlaceCandidateWithLocationDto> locationById,
            (double Lat, double Lng) origin)
        {
            var remaining = new List<PlaceCoverageDTO>(places);
            var ordered = new List<PlaceCoverageDTO>();
            var (currentLat, currentLng) = origin;

            while (remaining.Count > 0)
            {
                PlaceCoverageDTO? nearest = null;
                var nearestDistance = double.MaxValue;

                foreach (var place in remaining)
                {
                    var location = locationById[place.FPlaceId];
                    var distance = HaversineDistanceMeters(
                        currentLat, currentLng, location.Latitude, location.Longitude);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = place;
                    }
                }

                ordered.Add(nearest!);
                remaining.Remove(nearest!);

                var nearestLocation = locationById[nearest!.FPlaceId];
                currentLat = nearestLocation.Latitude;
                currentLng = nearestLocation.Longitude;
            }

            return ordered;
        }

        // Haversine 公式：計算地球上兩經緯度點的直線距離（公尺）
        // 用於演算法內部快速估算，不消耗 Google API 額度
        private static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusMeters = 6371000;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLng = DegreesToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                    * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
