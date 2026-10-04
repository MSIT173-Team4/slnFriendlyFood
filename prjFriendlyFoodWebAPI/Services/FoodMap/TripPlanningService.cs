using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    // 規劃流程拆成兩段：
    //   PreviewTripAsync  → 找附近店家、最佳化、推薦標記，只回傳結果，不寫 tFoodMapTrip
    //   ConfirmTripAsync  → 使用者確認後才建立行程，並呼叫 Google Routes 算實際路線
    // 使用者在預覽階段換店、調順序、重新規劃幾次都不會花 Routes API 的錢，
    // 附近店家與地點資料也都有快取（見 GooglePlacesClientCacheExtensions、PlaceService.UpsertGooglePlacesAsync）。
    public class TripPlanningService : ITripPlanningService
    {
        private readonly IShoppingListMappingService _mappingService;
        private readonly IRecommendationServices _recommendationService;
        private readonly ITripOptimizationService _tripOptimizationService;
        private readonly IFoodMapService _foodmap;
        private readonly IGooglePlacesClient _placesClient;
        private readonly ITripServices _tripService;
        private readonly FriendlyFoodDbContext _context;
        private readonly ILogger<TripPlanningService> _logger;

        public TripPlanningService(
            IShoppingListMappingService mappingService,
            IRecommendationServices recommendationService,
            ITripOptimizationService tripOptimizationService,
            IFoodMapService foodmap,
            IGooglePlacesClient placesClient,
            ITripServices tripService,
            FriendlyFoodDbContext context,
            ILogger<TripPlanningService> logger)
        {
            _mappingService = mappingService;
            _recommendationService = recommendationService;
            _tripOptimizationService = tripOptimizationService;
            _foodmap = foodmap;
            _placesClient = placesClient;
            _tripService = tripService;
            _context = context;
            _logger = logger;
        }

        // ---------- 進頁面的初始資料 ----------

        public async Task<PlanningContextDTO> GetPlanningContextAsync(
            int userId,
            int? shoppingListId,
            CancellationToken cancellationToken = default)
        {
            TFoodMapShoppingList? list;

            if (shoppingListId is > 0)
            {
                list = await LoadOwnedShoppingListAsync(userId, shoppingListId.Value, cancellationToken);
            }
            else
            {
                // 網址沒帶清單 ID：拿這個使用者目前的清單（食譜模組的「我的料理採購清單」是 Draft 狀態）
                list = await _context.TFoodMapShoppingLists
                    .AsNoTracking()
                    .Where(l => l.FUserId == userId)
                    .OrderByDescending(l => l.FStatus == "Draft")
                    .ThenByDescending(l => l.FUpdatedTime ?? l.FCreatedTime)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var pendingItemCount = list is null
                ? 0
                : await _context.TFoodMapShoppingListItems
                    .CountAsync(
                        i => i.FShoppingListId == list.FShoppingListId && !i.FIsPurchased,
                        cancellationToken);

            var memberAddress = await _context.TUsers
                .AsNoTracking()
                .Where(u => u.FId == userId)
                .Select(u => u.FAddress)
                .FirstOrDefaultAsync(cancellationToken);

            return new PlanningContextDTO
            {
                ShoppingListId = list?.FShoppingListId,
                ListName = list?.FListName,
                PendingItemCount = pendingItemCount,
                MemberLocation = await GeocodeAddressAsync(memberAddress, cancellationToken)
            };
        }

        // ---------- 第一段：預覽（不寫行程） ----------

        public async Task<PlanTripPreviewResultDTO> PreviewTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var list = await LoadOwnedShoppingListAsync(userId, request.ShoppingListId, cancellationToken);

            var mapping = await _mappingService.GetPlaceCategoriesForShoppingListAsync(
                list.FShoppingListId, cancellationToken);

            if (mapping.TotalItemCount == 0)
            {
                throw new ArgumentException("這份採買清單沒有待採買的品項");
            }

            if (mapping.ItemsByPlaceCategory.Count == 0)
            {
                throw new ArgumentException("清單裡的食材都還沒有設定可以在哪一類店家買到，暫時無法規劃");
            }

            var originLatitude = (double)request.OriginLatitude;
            var originLongitude = (double)request.OriginLongitude;
            var typeMap = await PlaceCategoryTypeMap.LoadAsync(_context, cancellationToken);

            // 依每個需要的店家分類各搜尋一次（Google 一次只能指定一種類型），能買最多品項的分類先搜
            var found = new List<(GooglePlace Place, int PlaceCategoryId)>();
            var seenGooglePlaceIds = new HashSet<string>();

            foreach (var category in mapping.ItemsByPlaceCategory.OrderByDescending(kv => kv.Value.Count))
            {
                if (!typeMap.TryGetValue(category.Key, out var googlePlaceType))
                {
                    _logger.LogWarning("店家分類 {PlaceCategoryId} 沒有對應的 Google 類型，略過", category.Key);
                    continue;
                }

                List<GooglePlace> places;

                try
                {
                    places = await _placesClient.SearchNearbyCachedAsync(
                        originLatitude,
                        originLongitude,
                        request.SearchRadiusMeters,
                        googlePlaceType,
                        cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException
                                               || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    // 某一種類型搜尋失敗或逾時（例如類型名稱 Google 不支援）不影響其他分類
                    _logger.LogWarning(ex, "搜尋附近 {GooglePlaceType} 失敗，略過這個分類", googlePlaceType);
                    continue;
                }

                foreach (var place in places)
                {
                    if (place.Location is not null && seenGooglePlaceIds.Add(place.Id))
                    {
                        found.Add((place, category.Key));
                    }
                }
            }

            if (found.Count == 0)
            {
                throw new ArgumentException("附近找不到任何符合這份清單需求的店家，建議放大搜尋範圍");
            }

            // Google 地點 → 內部 tFoodMapPlace（批次、新鮮的直接沿用，不打 Place Details）
            var placeIdByGoogleId = await _foodmap.UpsertGooglePlacesAsync(found, cancellationToken);

            var resolved = found
                .Where(f => placeIdByGoogleId.ContainsKey(f.Place.Id))
                .Select(f => (f.Place, f.PlaceCategoryId, PlaceId: placeIdByGoogleId[f.Place.Id]))
                .ToList();

            var candidatePlaces = resolved
                .Select(r => new PlaceCandidateWithLocationDto
                {
                    FPlaceId = r.PlaceId,
                    FName = r.Place.DisplayName?.Text ?? string.Empty,
                    FPlaceCategoryId = r.PlaceCategoryId,
                    Latitude = r.Place.Location!.Latitude,
                    Longitude = r.Place.Location!.Longitude,
                    IsFresh = PlaceCategoryTypeMap.IsFreshType(typeMap.GetValueOrDefault(r.PlaceCategoryId))
                })
                .ToList();

            var optimized = await _tripOptimizationService.OptimizeAsync(
                list.FShoppingListId,
                request.OriginLatitude,
                request.OriginLongitude,
                candidatePlaces,
                cancellationToken);

            var suggestedOrderByPlaceId = optimized.PlaceCoverages
                .Select((place, index) => (place.FPlaceId, Order: index + 1))
                .ToDictionary(x => x.FPlaceId, x => x.Order);

            var recommendedPlaceIds = await _recommendationService.GetActiveRecommendedPlaceIdsAsync(cancellationToken);

            var candidates = resolved
                .Select(r =>
                {
                    var matchedItems = mapping.ItemsByPlaceCategory.GetValueOrDefault(r.PlaceCategoryId)
                        ?? new List<ShoppingListItemDTO>();
                    var hasOrder = suggestedOrderByPlaceId.TryGetValue(r.PlaceId, out var order);

                    return new TripCandidateDTO
                    {
                        FPlaceId = r.PlaceId,
                        FGooglePlaceId = r.Place.Id,
                        FName = r.Place.DisplayName?.Text ?? string.Empty,
                        FAddress = r.Place.FormattedAddress ?? string.Empty,
                        FLatitude = (decimal)r.Place.Location!.Latitude,
                        FLongitude = (decimal)r.Place.Location!.Longitude,
                        FPlaceCategoryId = r.PlaceCategoryId,
                        FGoogleRating = r.Place.Rating.HasValue ? Math.Round((decimal)r.Place.Rating.Value, 1) : null,
                        IsRecommend = recommendedPlaceIds.Contains(r.PlaceId),
                        IsFresh = PlaceCategoryTypeMap.IsFreshType(typeMap.GetValueOrDefault(r.PlaceCategoryId)),
                        IsSuggested = hasOrder,
                        SuggestedOrder = hasOrder ? order : null,
                        MatchedItemIds = matchedItems.Select(i => i.FShoppingListItemId).ToList(),
                        MatchedItemNames = matchedItems.Select(i => i.FIngredientName).ToList(),
                        DistanceMeters = Math.Round(GeoDistance.Meters(
                            originLatitude, originLongitude,
                            r.Place.Location!.Latitude, r.Place.Location!.Longitude))
                    };
                })
                .OrderBy(c => c.SuggestedOrder ?? int.MaxValue)
                .ThenByDescending(c => c.MatchedItemIds.Count)
                .ThenBy(c => c.DistanceMeters)
                .ToList();

            return new PlanTripPreviewResultDTO
            {
                ShoppingListId = list.FShoppingListId,
                ListName = list.FListName,
                OriginLatitude = request.OriginLatitude,
                OriginLongitude = request.OriginLongitude,
                TravelMode = request.TravelMode,
                SearchRadiusMeters = request.SearchRadiusMeters,
                TotalItemCount = mapping.TotalItemCount,
                Items = mapping.AllItems,
                Candidates = candidates,
                FinalCoveragePercentage = optimized.FinalCoveragePercentage,
                UncoveredItemNames = optimized.UncoveredItemNames
            };
        }

        // ---------- 第二段：確認（建立行程 + 算路線） ----------

        public async Task<PlanTripResultDTO> ConfirmTripAsync(
            int userId,
            ConfirmTripRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            if (request.Places.Count == 0)
            {
                throw new ArgumentException("行程至少要有一個地點");
            }

            ShoppingListMappingResult? mapping = null;
            string? description = null;

            if (request.ShoppingListId is > 0)
            {
                var list = await LoadOwnedShoppingListAsync(userId, request.ShoppingListId.Value, cancellationToken);
                mapping = await _mappingService.GetPlaceCategoriesForShoppingListAsync(list.FShoppingListId, cancellationToken);
                description = $"依採買清單「{list.FListName}」規劃";
            }

            var trip = await _tripService.CreateTripAsync(
                new CreateTripRequestDTO
                {
                    FTripName = request.FTripName,
                    Places = request.Places
                },
                userId,
                description,
                cancellationToken);

            try
            {
                trip = await _tripService.FinalizeTripAsync(trip.FTripId, request.TravelMode, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // 行程已經建立成功，路線算不出來不應該讓整個請求失敗
                _logger.LogError(ex, "Trip {TripId} 已建立，但計算路線時發生錯誤", trip.FTripId);
            }

            var (coverage, uncoveredNames) = mapping is null
                ? (0d, new List<string>())
                : await CalculateCoverageAsync(mapping, request.Places, cancellationToken);

            return new PlanTripResultDTO
            {
                Trip = trip,
                FinalCoveragePercentage = coverage,
                UncoveredItemNames = uncoveredNames
            };
        }

        // ---------- 採買模式：勾選已買 ----------

        public async Task<ShoppingItemPurchasedDTO> SetItemPurchasedAsync(
            int userId,
            int shoppingItemId,
            bool isPurchased,
            CancellationToken cancellationToken = default)
        {
            var item = await _context.TFoodMapShoppingListItems
                .Include(i => i.FShoppingList)
                .FirstOrDefaultAsync(i => i.FShoppingItemId == shoppingItemId, cancellationToken)
                ?? throw new FoodMapNotFoundException("找不到這個採買品項，清單可能已經被重新儲存，請重新規劃");

            if (item.FShoppingList.FUserId != userId)
            {
                throw new FoodMapForbiddenException("這份採買清單不屬於目前登入的帳號");
            }

            if (item.FIsPurchased != isPurchased)
            {
                item.FIsPurchased = isPurchased;
                item.FUpdatedTime = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new ShoppingItemPurchasedDTO
            {
                FShoppingListItemId = item.FShoppingItemId,
                FIsPurchased = item.FIsPurchased
            };
        }

        // ---------- 舊版相容：一次做完 ----------

        public async Task<PlanTripResultDTO> PlanTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var preview = await PreviewTripAsync(userId, request, cancellationToken);

            var suggested = preview.Candidates
                .Where(c => c.IsSuggested)
                .OrderBy(c => c.SuggestedOrder)
                .ToList();

            if (suggested.Count == 0)
            {
                throw new ArgumentException("找不到任何能滿足清單需求的店家組合");
            }

            var result = await ConfirmTripAsync(
                userId,
                new ConfirmTripRequestDTO
                {
                    FTripName = $"採買行程 {DateTime.Now:MM/dd HH:mm}",
                    ShoppingListId = preview.ShoppingListId,
                    TravelMode = request.TravelMode,
                    Places = suggested
                        .Select((c, index) => new CreateTripPlacesRequestDTO
                        {
                            FPlaceID = c.FPlaceId,
                            FSortOrder = index + 1,
                            FPlaceCategoryId = c.FPlaceCategoryId
                        })
                        .ToList()
                },
                cancellationToken);

            result.FinalCoveragePercentage = preview.FinalCoveragePercentage;
            result.UncoveredItemNames = preview.UncoveredItemNames;
            return result;
        }

        // ---------- 私有方法 ----------

        // 清單必須存在，而且屬於目前登入的使用者（避免改網址就能拿別人的清單）
        private async Task<TFoodMapShoppingList> LoadOwnedShoppingListAsync(
            int userId,
            int shoppingListId,
            CancellationToken cancellationToken)
        {
            var list = await _context.TFoodMapShoppingLists
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.FShoppingListId == shoppingListId, cancellationToken)
                ?? throw new FoodMapNotFoundException("找不到這份採買清單");

            if (list.FUserId != userId)
            {
                throw new FoodMapForbiddenException("這份採買清單不屬於目前登入的帳號");
            }

            return list;
        }

        // 覆蓋率：優先用前端送回來的分類（也就是預覽時搜尋到這家店的分類，跟預覽畫面一致），
        // 沒帶的（例如從搜尋、地圖點選加入的店）才用資料庫裡記錄的分類
        private async Task<(double Coverage, List<string> UncoveredNames)> CalculateCoverageAsync(
            ShoppingListMappingResult mapping,
            List<CreateTripPlacesRequestDTO> places,
            CancellationToken cancellationToken)
        {
            if (mapping.TotalItemCount == 0)
            {
                return (0d, new List<string>());
            }

            var placeCategoryIds = places
                .Where(p => p.FPlaceCategoryId is > 0)
                .Select(p => p.FPlaceCategoryId!.Value)
                .ToHashSet();

            var placeIdsWithoutCategory = places
                .Where(p => p.FPlaceCategoryId is null or <= 0)
                .Select(p => p.FPlaceID)
                .Distinct()
                .ToList();

            if (placeIdsWithoutCategory.Count > 0)
            {
                var storedCategoryIds = await _context.TFoodMapPlaces
                    .AsNoTracking()
                    .Where(p => placeIdsWithoutCategory.Contains(p.FPlaceId) && p.FPlaceCategoryId != null)
                    .Select(p => p.FPlaceCategoryId!.Value)
                    .ToListAsync(cancellationToken);

                placeCategoryIds.UnionWith(storedCategoryIds);
            }

            var coveredItemIds = placeCategoryIds
                .SelectMany(id => mapping.ItemsByPlaceCategory.GetValueOrDefault(id) ?? new List<ShoppingListItemDTO>())
                .Select(i => i.FShoppingListItemId)
                .ToHashSet();

            var uncoveredNames = mapping.AllItems
                .Where(i => !coveredItemIds.Contains(i.FShoppingListItemId))
                .Select(i => i.FIngredientName)
                .Distinct()
                .ToList();

            var coverage = (double)mapping.AllItems.Count(i => coveredItemIds.Contains(i.FShoppingListItemId))
                / mapping.TotalItemCount;

            return (coverage, uncoveredNames);
        }

        // 會員地址 → 座標（用 Places Text Search，結果快取 1 天，不用另外開 Geocoding API）
        private async Task<LocationDTO?> GeocodeAddressAsync(string? address, CancellationToken cancellationToken)
        {
            var trimmed = address?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            try
            {
                var results = await _placesClient.SearchTextCachedAsync(
                    trimmed,
                    latitude: null,
                    longitude: null,
                    radiusMeters: null,
                    pageSize: 1,
                    timeToLive: TimeSpan.FromDays(1),
                    cancellationToken);

                var location = results.FirstOrDefault(p => p.Location is not null)?.Location;

                return location is null
                    ? null
                    : new LocationDTO
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Address = trimmed
                    };
            }
            catch (Exception ex) when (ex is HttpRequestException
                                           || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning(ex, "會員地址轉換座標失敗");
                return null;
            }
        }
    }
}
