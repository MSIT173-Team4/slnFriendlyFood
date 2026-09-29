using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Exceptions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class TripServices : ITripServices
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly IGoogleRoutesClient _routesClient;
        private readonly ILogger<TripServices> _logger;

        // 修正：原本建構子沒有注入 ILogger，_logger 永遠是 null，
        // Google Routes 一失敗、catch 區塊寫 log 時就會 NullReferenceException。
        public TripServices(
            FriendlyFoodDbContext context,
            IGoogleRoutesClient routesClient,
            ILogger<TripServices> logger)
        {
            _context = context;
            _routesClient = routesClient;
            _logger = logger;
        }

        public async Task<List<TripDTO>> GetTripsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var trips = await ProjectToDto(
                    _context.TFoodMapTrips
                        .AsNoTracking()
                        .Where(t => t.FUserId == userId)
                        .OrderByDescending(t => t.FCreatedTime))
                .ToListAsync(cancellationToken);

            trips.ForEach(FillTotals);
            return trips;
        }

        public async Task<TripDTO?> GetTripByIdAsync(int tripId, int? userId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.TFoodMapTrips
                .AsNoTracking()
                .Where(t => t.FTripId == tripId);

            if (userId.HasValue)
            {
                query = query.Where(t => t.FUserId == userId.Value);
            }

            var trip = await ProjectToDto(query).FirstOrDefaultAsync(cancellationToken);

            if (trip is not null)
            {
                FillTotals(trip);
            }

            return trip;
        }

        public async Task<TripDTO> CreateTripAsync(
            CreateTripRequestDTO request,
            int? userId = null,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            var tripName = request.FTripName?.Trim();
            if (string.IsNullOrEmpty(tripName))
            {
                throw new ArgumentException("請輸入行程名稱");
            }

            if (request.Places.Count == 0)
            {
                throw new ArgumentException("行程至少要有一個地點");
            }

            // 一次檢查全部店家是否存在（原本是迴圈裡逐筆查）
            var placeIds = request.Places.Select(p => p.FPlaceID).Distinct().ToList();

            var existingPlaceIds = await _context.TFoodMapPlaces
                .Where(p => placeIds.Contains(p.FPlaceId))
                .Select(p => p.FPlaceId)
                .ToListAsync(cancellationToken);

            var missing = placeIds.Except(existingPlaceIds).ToList();
            if (missing.Count > 0)
            {
                throw new ArgumentException($"找不到店家 ID：{string.Join(", ", missing)}");
            }

            var now = DateTime.UtcNow;

            var trip = new TFoodMapTrip
            {
                // 修正：原本沒有寫入使用者，行程不屬於任何人
                FUserId = userId,
                FTripName = tripName.Length > 100 ? tripName[..100] : tripName,
                FDescription = description,
                FCreatedTime = now,
                FUpdatedTime = now
            };

            // 同一間店只保留一次，順序依 FSortOrder 重新編成 1、2、3…
            var orderedPlaces = request.Places
                .GroupBy(p => p.FPlaceID)
                .Select(g => g.First())
                .OrderBy(p => p.FSortOrder)
                .ToList();

            for (var i = 0; i < orderedPlaces.Count; i++)
            {
                trip.TFoodMapTripPlaces.Add(new TFoodMapTripPlace
                {
                    FPlaceId = orderedPlaces[i].FPlaceID,
                    FSortOrder = i + 1,
                    FCreatedTime = now
                });
            }

            _context.TFoodMapTrips.Add(trip);
            await _context.SaveChangesAsync(cancellationToken);

            return await GetTripByIdAsync(trip.FTripId, null, cancellationToken)
                ?? throw new InvalidOperationException("建立行程後無法取得資料。");
        }

        public async Task<TripDTO> FinalizeTripAsync(
            int tripId,
            GoogleTravelMode travelMode,
            CancellationToken cancellationToken = default)
        {
            var trip = await _context.TFoodMapTrips
                .Include(t => t.TFoodMapTripPlaces)
                .ThenInclude(tp => tp.FPlace)
                .FirstOrDefaultAsync(t => t.FTripId == tripId, cancellationToken)
                ?? throw new FoodMapNotFoundException("找不到指定行程");

            var orderedTripPlaces = trip.TFoodMapTripPlaces
                .OrderBy(tp => tp.FSortOrder)
                .ToList();

            // 少於 2 站不需要規劃路線
            if (orderedTripPlaces.Count >= 2)
            {
                var waypoints = orderedTripPlaces
                    .Select(tp => ((double)tp.FPlace.FLatitude, (double)tp.FPlace.FLongitude))
                    .ToList();

                GoogleRouteResult? routeResult = null;

                try
                {
                    routeResult = await _routesClient.ComputeRouteAsync(waypoints, travelMode, cancellationToken);
                }
                catch (Exception ex) when (ex is GoogleRoutesUnavailableException
                                               or BrokenCircuitException
                                               or HttpRequestException)
                {
                    // Google Routes 暫時無法使用：行程本身已經建立成功，路線資料留空，
                    // 前端會顯示「路線資料暫時無法取得」
                    _logger.LogWarning(ex, "計算 Trip {TripId} 路線失敗，行程仍保留，路線資料略過", tripId);
                }

                if (routeResult is not null && routeResult.Legs.Count > 0)
                {
                    // 先清掉這個 Trip 舊的逐段資料，避免順序變了之後留下對不上的舊紀錄
                    var oldRoutes = await _context.TFoodMapTripRoutes
                        .Where(r => r.FTripId == tripId)
                        .ToListAsync(cancellationToken);
                    _context.TFoodMapTripRoutes.RemoveRange(oldRoutes);

                    var legCount = Math.Min(routeResult.Legs.Count, orderedTripPlaces.Count - 1);

                    for (var i = 0; i < legCount; i++)
                    {
                        var leg = routeResult.Legs[i];
                        var fromPlace = orderedTripPlaces[i];
                        var toPlace = orderedTripPlaces[i + 1];

                        _context.TFoodMapTripRoutes.Add(new TFoodMapTripRoute
                        {
                            FTripId = tripId,
                            FFromTripPlaceId = fromPlace.FTripPlaceId,
                            FToTripPlaceId = toPlace.FTripPlaceId,
                            FDistanceMeters = leg.DistanceMeters,
                            FDurationSeconds = leg.DurationSeconds,
                            FPolyline = leg.EncodedPolyline,
                            FCreatedTime = DateTime.UtcNow
                        });

                        toPlace.FDurationFromPrevious = leg.DurationSeconds;
                    }

                    trip.FUpdatedTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return await GetTripByIdAsync(tripId, null, cancellationToken)
                ?? throw new FoodMapNotFoundException("找不到指定行程");
        }

        // ---------- 共用：Entity → DTO ----------

        // 修正：原本 GetTripByIdAsync 漏了地址、經緯度，這裡統一一個投影，所有查詢都用它
        private static IQueryable<TripDTO> ProjectToDto(IQueryable<TFoodMapTrip> query) =>
            query.Select(t => new TripDTO
            {
                FTripId = t.FTripId,
                FTripName = t.FTripName,
                FStatus = t.FStatus,
                FDescription = t.FDescription,
                FCreatedTime = t.FCreatedTime,
                Places = t.TFoodMapTripPlaces
                    .OrderBy(tp => tp.FSortOrder)
                    .Select(tp => new TripPlaceDTO
                    {
                        FTripPlaceId = tp.FTripPlaceId,
                        FPlaceId = tp.FPlaceId,
                        FPlaceName = tp.FPlace.FName,
                        FAddress = tp.FPlace.FAddress,
                        FLatitude = tp.FPlace.FLatitude,
                        FLongitude = tp.FPlace.FLongitude,
                        FSortOrder = tp.FSortOrder
                    })
                    .ToList(),
                Routes = t.TFoodMapTripRoutes
                    .OrderBy(r => r.FTripRouteId)
                    .Select(r => new TripRouteDTO
                    {
                        FFromTripPlaceId = r.FFromTripPlaceId,
                        FToTripPlaceId = r.FToTripPlaceId,
                        FDistanceMeters = r.FDistanceMeters,
                        FDurationSeconds = r.FDurationSeconds,
                        FPolyline = r.FPolyline
                    })
                    .ToList()
            });

        private static void FillTotals(TripDTO trip)
        {
            if (trip.Routes.Count == 0)
            {
                return;
            }

            trip.TotalDistanceMeters = trip.Routes.Sum(r => r.FDistanceMeters ?? 0);
            trip.TotalDurationSeconds = trip.Routes.Sum(r => r.FDurationSeconds ?? 0);
        }
    }
}
