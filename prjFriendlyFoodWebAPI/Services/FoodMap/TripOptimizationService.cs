using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

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
            // 分母用「全部待買品項」（包含找不到店家類型的），
            // 原本只算有對應到分類的品項，會讓覆蓋率虛高、買不到的品項也不會列出來
            var uncoveredItemIds = mapping.AllItems
                .Select(i => i.FShoppingListItemId)
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

            // ---------------- 第二階段：最短路程排序（生鮮類店家排最後） ----------------
            var orderedPlaces = OrderPlacesByDistance(
                selectedPlaces, candidatePlaces, originLatitude, originLongitude);

            var uncoveredItemNames = mapping.AllItems
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

        // 8 間店以內用窮舉找最短順序（8! = 40320，計算量可接受）
        private const int MaxExhaustivePlaces = 8;

        // 排出造訪順序：
        //   1. 生鮮類店家（肉舖、傳統市場…）一律排在一般店家之後，生鮮食材才不會跟著跑完整趟
        //   2. 在這個前提下，從起點出發的總移動距離最短
        // 距離用直線距離估算，不消耗 Google API 額度；實際行車路線在確認儲存時才計算。
        private static List<PlaceCoverageDTO> OrderPlacesByDistance(
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

            var regularPlaces = selectedPlaces.Where(p => !locationById[p.FPlaceId].IsFresh).ToList();
            var freshPlaces = selectedPlaces.Where(p => locationById[p.FPlaceId].IsFresh).ToList();

            if (selectedPlaces.Count <= MaxExhaustivePlaces)
            {
                return FindShortestGroupedOrder(regularPlaces, freshPlaces, locationById, origin);
            }

            // 超過 8 間店：兩組各自用最近鄰法排出初始順序，再用 2-opt 消除繞路
            var orderedRegular = ImproveWithTwoOpt(
                NearestNeighborOrder(regularPlaces, locationById, origin), locationById, origin);

            var freshStart = origin;
            if (orderedRegular.Count > 0)
            {
                var last = locationById[orderedRegular[^1].FPlaceId];
                freshStart = (last.Latitude, last.Longitude);
            }

            var orderedFresh = ImproveWithTwoOpt(
                NearestNeighborOrder(freshPlaces, locationById, freshStart), locationById, freshStart);

            orderedRegular.AddRange(orderedFresh);
            return orderedRegular;
        }

        // 窮舉「一般店家的所有排列 × 生鮮店家的所有排列」，取整趟距離最短的組合
        private static List<PlaceCoverageDTO> FindShortestGroupedOrder(
            List<PlaceCoverageDTO> regularPlaces,
            List<PlaceCoverageDTO> freshPlaces,
            Dictionary<int, PlaceCandidateWithLocationDto> locationById,
            (double Lat, double Lng) origin)
        {
            var freshPermutations = GetPermutations(freshPlaces).ToList();

            List<PlaceCoverageDTO>? bestOrder = null;
            var bestDistance = double.MaxValue;

            foreach (var regularOrder in GetPermutations(regularPlaces))
            {
                foreach (var freshOrder in freshPermutations)
                {
                    var combined = new List<PlaceCoverageDTO>(regularOrder.Count + freshOrder.Count);
                    combined.AddRange(regularOrder);
                    combined.AddRange(freshOrder);

                    var totalDistance = CalculateTotalDistance(combined, locationById, origin);
                    if (totalDistance < bestDistance)
                    {
                        bestDistance = totalDistance;
                        bestOrder = combined;
                    }
                }
            }

            return bestOrder ?? regularPlaces.Concat(freshPlaces).ToList();
        }

        // 2-opt：反轉路線中的一段，如果總距離變短就採用，直到沒有更短的為止
        private static List<PlaceCoverageDTO> ImproveWithTwoOpt(
            List<PlaceCoverageDTO> route,
            Dictionary<int, PlaceCandidateWithLocationDto> locationById,
            (double Lat, double Lng) start)
        {
            if (route.Count < 3)
            {
                return route;
            }

            var best = new List<PlaceCoverageDTO>(route);
            var bestDistance = CalculateTotalDistance(best, locationById, start);
            var improved = true;

            while (improved)
            {
                improved = false;

                for (var i = 0; i < best.Count - 1; i++)
                {
                    for (var j = i + 1; j < best.Count; j++)
                    {
                        var candidate = new List<PlaceCoverageDTO>(best);
                        candidate.Reverse(i, j - i + 1);

                        var distance = CalculateTotalDistance(candidate, locationById, start);
                        if (distance < bestDistance - 0.5)
                        {
                            best = candidate;
                            bestDistance = distance;
                            improved = true;
                        }
                    }
                }
            }

            return best;
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
