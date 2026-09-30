using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class PlaceService : IFoodMapService
    {
        private const double InitialRadiusKm = 1.0;

        private const double ExpandedRadiusKm = 3.0;

        private const int DefaultMinimumResults = 5;

        // 本地店家資料幾天內同步過就算「新鮮」，不再用 Google 資料覆寫
        private const int PlaceFreshnessDays = 30;

        private const string CategoryIdsCacheKey = "foodmap:place-category-ids";

        private readonly FriendlyFoodDbContext _context;
        private readonly IGooglePlacesClient _googlePlacesClient;

        public PlaceService(FriendlyFoodDbContext context, IGooglePlacesClient googlePlacesClient)
        {
            _context = context;
            _googlePlacesClient = googlePlacesClient;
        }

        public async Task<List<PlaceDTO>> GetPlacesAsync()
        {
            return await _context.TFoodMapPlaces
                .AsNoTracking()
                .Select(r => new PlaceDTO
                {
                    FPlaceId = r.FPlaceId,
                    FGooglePlaceId = r.FGooglePlaceId,
                    FPlaceCategoryId = r.FPlaceCategoryId,
                    FName = r.FName,
                    FAddress = r.FAddress,
                    FLatitude = r.FLatitude,
                    FLongitude = r.FLongitude,
                    FPhone = r.FPhone,
                    FGoogleRating = r.FGoogleRating,
                    FGoogleReviewCount = r.FGoogleReviewCount,
                    FBusinessStatus = r.FBusinessStatus,
                    FIsRecommend = r.TFoodMapRecommendationPlaces.Any(item => item.FIsRecommend)
                })
                .ToListAsync();
        }

        public async Task<PlaceDTO?> GetPlaceByIdAsync(long id)
        {
            return await _context.TFoodMapPlaces
                .AsNoTracking()
                .Where(r => r.FPlaceId == id)
                .Select(r => new PlaceDTO
                {
                    FPlaceId = r.FPlaceId,
                    FGooglePlaceId = r.FGooglePlaceId,
                    FPlaceCategoryId = r.FPlaceCategoryId,
                    FName = r.FName,
                    FAddress = r.FAddress,
                    FLatitude = r.FLatitude,
                    FLongitude = r.FLongitude,
                    FPhone = r.FPhone,
                    FGoogleRating = r.FGoogleRating,
                    FGoogleReviewCount = r.FGoogleReviewCount,
                    FBusinessStatus = r.FBusinessStatus,
                    FIsRecommend = r.TFoodMapRecommendationPlaces.Any(item => item.FIsRecommend)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<NearbyResponseDTO> GetNearbyPlacesAsync(NearbyRequestDTO request, CancellationToken cancellationToken = default)
        {
            var minimumResults = request.MinimumRequests > 0 ? request.MinimumRequests : DefaultMinimumResults;

            // 分類 → Google 類型改讀資料庫（原本的中文對照值 Google 不認得）
            string? googlePlaceType = null;
            if (request.FPlacesCategoryId.HasValue)
            {
                var typeMap = await PlaceCategoryTypeMap.LoadAsync(_context, cancellationToken);
                typeMap.TryGetValue(request.FPlacesCategoryId.Value, out googlePlaceType);
            }

            List<GooglePlace> places;
            double radiusMeters;
            var expandedSearch = false;

            if (request.RadiusMeters.HasValue)
            {
                radiusMeters = request.RadiusMeters.Value;
                places = await _googlePlacesClient.SearchNearbyCachedAsync(
                    request.FLatitude, request.FLongitude, radiusMeters, googlePlaceType, cancellationToken);
            }
            else
            {
                radiusMeters = InitialRadiusKm * 1000;
                places = await _googlePlacesClient.SearchNearbyCachedAsync(
                    request.FLatitude, request.FLongitude, radiusMeters, googlePlaceType, cancellationToken);

                if (places.Count < minimumResults)
                {
                    radiusMeters = ExpandedRadiusKm * 1000;
                    places = await _googlePlacesClient.SearchNearbyCachedAsync(
                        request.FLatitude, request.FLongitude, radiusMeters, googlePlaceType, cancellationToken);
                    expandedSearch = true;
                }
            }

            var result = await ToPlaceDtosAsync(places, cancellationToken);

            return new NearbyResponseDTO
            {
                FLatitude = request.FLatitude,
                FLongitude = request.FLongitude,
                SearchRadiusKm = radiusMeters / 1000,
                ExpandedSearch = expandedSearch,
                ResultCount = result.Count,
                Places = result
            };
        }

        public async Task<List<PlaceDTO>> SearchPlacesAsync(PlaceSearchRequestDTO request, CancellationToken cancellationToken = default)
        {
            var query = request.Query.Trim();
            if (query.Length == 0)
            {
                throw new ArgumentException("請輸入要搜尋的地點名稱");
            }

            var places = await _googlePlacesClient.SearchTextCachedAsync(
                query,
                request.Latitude,
                request.Longitude,
                request.RadiusMeters,
                pageSize: 10,
                timeToLive: TimeSpan.FromMinutes(30),
                cancellationToken);

            return await ToPlaceDtosAsync(places, cancellationToken);
        }

        public async Task<int> ResolvePlaceAsync(ResolvePlaceRequestDTO request, CancellationToken cancellationToken = default)
        {
            var googlePlaceId = request.FGooglePlaceId?.Trim();
            if (string.IsNullOrEmpty(googlePlaceId))
            {
                throw new ArgumentException("缺少 Google 地點 ID");
            }

            var categoryId = await NormalizeCategoryIdAsync(request.FPlaceCategoryId, cancellationToken);

            var existing = await _context.TFoodMapPlaces.FirstOrDefaultAsync(
                p => p.FGooglePlaceId == googlePlaceId,
                cancellationToken);

            if (existing is not null)
            {
                // 以前建立時分類被註解掉沒寫入，這裡順便補上
                if (existing.FPlaceCategoryId is null && categoryId is not null)
                {
                    existing.FPlaceCategoryId = categoryId;
                    existing.FUpdatedTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return existing.FPlaceId;
            }

            var detail = await _googlePlacesClient.GetPlaceDetailsAsync(googlePlaceId, cancellationToken);

            if (detail is null || detail.Location is null)
            {
                throw new ArgumentException($"Google 查無此地點：{googlePlaceId}");
            }

            var place = MapGooglePlaceToPlace(detail, categoryId);
            _context.TFoodMapPlaces.Add(place);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return place.FPlaceId;
            }
            catch (DbUpdateException)
            {
                // 同一時間別的請求已經寫入同一個 Google 地點（唯一索引衝突），改讀那一筆
                _context.Entry(place).State = EntityState.Detached;

                var concurrent = await _context.TFoodMapPlaces
                    .AsNoTracking()
                    .Where(p => p.FGooglePlaceId == googlePlaceId)
                    .Select(p => (int?)p.FPlaceId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (concurrent is null)
                {
                    throw;
                }

                return concurrent.Value;
            }
        }

        public async Task<PlaceDTO> ResolvePlaceDetailAsync(ResolvePlaceRequestDTO request, CancellationToken cancellationToken = default)
        {
            var placeId = await ResolvePlaceAsync(request, cancellationToken);

            return await GetPlaceByIdAsync(placeId)
                ?? throw new InvalidOperationException("店家建立後無法取得資料。");
        }

        public async Task<Dictionary<string, int>> UpsertGooglePlacesAsync(
            IReadOnlyCollection<(GooglePlace Place, int PlaceCategoryId)> places,
            CancellationToken cancellationToken = default)
        {
            var unique = places
                .Where(p => !string.IsNullOrWhiteSpace(p.Place.Id) && p.Place.Location is not null)
                .GroupBy(p => p.Place.Id)
                .Select(g => g.First())
                .ToList();

            if (unique.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            var googleIds = unique.Select(p => p.Place.Id).ToList();
            var validCategoryIds = await GetValidCategoryIdsAsync(cancellationToken);

            // 一次撈出已經存在的店家，避免迴圈裡逐筆查（N+1）
            var existingPlaces = await _context.TFoodMapPlaces
                .Where(p => p.FGooglePlaceId != null && googleIds.Contains(p.FGooglePlaceId))
                .ToListAsync(cancellationToken);

            var existingByGoogleId = existingPlaces.ToDictionary(p => p.FGooglePlaceId!);

            var now = DateTime.UtcNow;
            var freshnessThreshold = now.AddDays(-PlaceFreshnessDays);
            var addedPlaces = new List<TFoodMapPlace>();

            foreach (var (googlePlace, placeCategoryId) in unique)
            {
                int? categoryId = validCategoryIds.Contains(placeCategoryId) ? placeCategoryId : null;

                if (existingByGoogleId.TryGetValue(googlePlace.Id, out var existing))
                {
                    if (existing.FPlaceCategoryId is null && categoryId is not null)
                    {
                        existing.FPlaceCategoryId = categoryId;
                        existing.FUpdatedTime = now;
                    }

                    // 30 天內同步過：直接沿用本地資料
                    if (existing.FSyncedAt is null || existing.FSyncedAt < freshnessThreshold)
                    {
                        ApplyGoogleData(existing, googlePlace);
                        existing.FSyncedAt = now;
                        existing.FUpdatedTime = now;
                    }

                    continue;
                }

                var newPlace = MapGooglePlaceToPlace(googlePlace, categoryId);
                _context.TFoodMapPlaces.Add(newPlace);
                addedPlaces.Add(newPlace);
            }

            if (_context.ChangeTracker.HasChanges())
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException) when (addedPlaces.Count > 0)
                {
                    // 有其他請求同時寫入相同的 Google 地點：撤掉這批新增，只保留更新，再存一次
                    foreach (var added in addedPlaces)
                    {
                        _context.Entry(added).State = EntityState.Detached;
                    }

                    if (_context.ChangeTracker.HasChanges())
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            return await _context.TFoodMapPlaces
                .AsNoTracking()
                .Where(p => p.FGooglePlaceId != null && googleIds.Contains(p.FGooglePlaceId))
                .Select(p => new { GooglePlaceId = p.FGooglePlaceId!, p.FPlaceId })
                .ToDictionaryAsync(p => p.GooglePlaceId, p => p.FPlaceId, cancellationToken);
        }

        // ---------- 私有方法 ----------

        // Google 地點轉成 PlaceDTO；本地已經有的店家會帶上內部 FPlaceId（沒有的是 0）
        private async Task<List<PlaceDTO>> ToPlaceDtosAsync(List<GooglePlace> places, CancellationToken cancellationToken)
        {
            var googleIds = places.Select(p => p.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var known = await _context.TFoodMapPlaces
                .AsNoTracking()
                .Where(p => p.FGooglePlaceId != null && googleIds.Contains(p.FGooglePlaceId))
                .Select(p => new
                {
                    GooglePlaceId = p.FGooglePlaceId!,
                    p.FPlaceId,
                    p.FPlaceCategoryId,
                    IsRecommend = p.TFoodMapRecommendationPlaces.Any(r => r.FIsRecommend)
                })
                .ToListAsync(cancellationToken);

            var knownByGoogleId = known
                .GroupBy(k => k.GooglePlaceId)
                .ToDictionary(g => g.Key, g => g.First());

            return places
                .Where(p => p.Location is not null)
                .Select(p =>
                {
                    knownByGoogleId.TryGetValue(p.Id, out var local);

                    return new PlaceDTO
                    {
                        FPlaceId = local?.FPlaceId ?? 0,
                        FGooglePlaceId = p.Id,
                        FPlaceCategoryId = local?.FPlaceCategoryId,
                        FName = p.DisplayName?.Text ?? string.Empty,
                        FAddress = p.FormattedAddress ?? string.Empty,
                        FLatitude = (decimal)p.Location!.Latitude,
                        FLongitude = (decimal)p.Location!.Longitude,
                        FPhone = p.NationalPhoneNumber,
                        FGoogleRating = ToRating(p.Rating),
                        FGoogleReviewCount = p.UserRatingCount,
                        FBusinessStatus = p.BusinessStatus,
                        FIsRecommend = local?.IsRecommend ?? false
                    };
                })
                .ToList();
        }

        private async Task<HashSet<int>> GetValidCategoryIdsAsync(CancellationToken cancellationToken)
        {
            if (FoodMapMemoryCache.TryGet<HashSet<int>>(CategoryIdsCacheKey, out var cached))
            {
                return cached;
            }

            var ids = await _context.TFoodMapPlaceCategories
                .AsNoTracking()
                .Select(c => c.FPlaceCategoryId)
                .ToListAsync(cancellationToken);

            var set = ids.ToHashSet();
            FoodMapMemoryCache.Set(CategoryIdsCacheKey, set, TimeSpan.FromMinutes(10));
            return set;
        }

        // 分類 ID 不存在於 tFoodMapPlaceCategory 時存 null，避免外鍵錯誤
        private async Task<int?> NormalizeCategoryIdAsync(int placeCategoryId, CancellationToken cancellationToken)
        {
            if (placeCategoryId <= 0)
            {
                return null;
            }

            var validIds = await GetValidCategoryIdsAsync(cancellationToken);
            return validIds.Contains(placeCategoryId) ? placeCategoryId : null;
        }

        private static TFoodMapPlace MapGooglePlaceToPlace(GooglePlace googlePlace, int? categoryId)
        {
            var now = DateTime.UtcNow;

            var place = new TFoodMapPlace
            {
                FGooglePlaceId = Truncate(googlePlace.Id, 100),
                FPlaceCategoryId = categoryId,
                FIsActive = true,
                FSyncedAt = now,
                FCreatedTime = now,
                FName = string.Empty,
                FAddress = string.Empty
            };

            ApplyGoogleData(place, googlePlace);
            return place;
        }

        // 只更新 Google 那邊的資料，不動 FIsActive、FRecommend 這些 FoodMap 自己的商業欄位
        private static void ApplyGoogleData(TFoodMapPlace place, GooglePlace googlePlace)
        {
            place.FName = Truncate(googlePlace.DisplayName?.Text ?? place.FName, 100) ?? string.Empty;
            place.FAddress = Truncate(googlePlace.FormattedAddress ?? place.FAddress, 300) ?? string.Empty;

            if (googlePlace.Location is not null)
            {
                place.FLatitude = Math.Round((decimal)googlePlace.Location.Latitude, 7);
                place.FLongitude = Math.Round((decimal)googlePlace.Location.Longitude, 7);
            }

            place.FPhone = Truncate(googlePlace.NationalPhoneNumber, 30) ?? place.FPhone;
            place.FGoogleRating = ToRating(googlePlace.Rating) ?? place.FGoogleRating;
            place.FGoogleReviewCount = googlePlace.UserRatingCount ?? place.FGoogleReviewCount;
            place.FBusinessStatus = Truncate(googlePlace.BusinessStatus, 30) ?? place.FBusinessStatus;
        }

        // 資料庫欄位是 decimal(2,1)
        private static decimal? ToRating(double? rating) =>
            rating.HasValue ? Math.Round((decimal)rating.Value, 1) : null;

        private static string? Truncate(string? value, int maxLength) =>
            value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
