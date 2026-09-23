
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Exceptions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

public class TripServices : ITripServices
{
    private readonly FriendlyFoodDbContext _context;
    private readonly IGooglePlacesClient _googlePlacesClient;
    private readonly IGoogleRoutesClient _routesClient;
    private readonly ILogger<TripServices> _logger;

    public TripServices(FriendlyFoodDbContext context, IGooglePlacesClient googlePlacesClient, IGoogleRoutesClient routesClient)
    {
        _context = context;
        _googlePlacesClient = googlePlacesClient;
        _routesClient = routesClient;
    }
    public async Task<List<TripDTO>> GetTripsAsync()
    {
        return await _context.TFoodMapTrips
            .AsNoTracking()
            .Select(t => new TripDTO
            {
                FTripId = t.FTripId,
                FTripName = t.FTripName,
                Places = t.TFoodMapTripPlaces
                        .OrderBy(tp => tp.FSortOrder)
                        .Select(tp => new
                        TripPlaceDTO
                        {
                            FTripPlaceId = tp.FTripPlaceId,
                            FPlaceId = tp.FPlaceId,
                            FPlaceName = tp.FPlace.FName,
                            FSortOrder = tp.FSortOrder
                        })
                        .ToList()
            })
            .ToListAsync();
    }
    public async Task<TripDTO?>GetTripByIdAsync(int tripId, CancellationToken cancellationToken = default)
    {
        return await _context.TFoodMapTrips
            .AsNoTracking()
            .Where(t =>
                t.FTripId == tripId)
            .Select(t => new TripDTO
            {
                FTripId =t.FTripId,
                FTripName =t.FTripName,
                Places =t.TFoodMapTripPlaces
                        .OrderBy(tp =>tp.FSortOrder)
                        .Select(tp =>new TripPlaceDTO
                            {
                                FTripPlaceId =tp.FTripPlaceId,
                                FPlaceId =tp.FPlaceId,
                                FPlaceName =tp.FPlace.FName,
                                FSortOrder =tp.FSortOrder
                            })
                        .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TripDTO> CreateTripAsync(CreateTripRequestDTO request, CancellationToken cancellationToken = default)
    {
        var trip = new TFoodMapTrip
        {
            FTripName = request.FTripName,
            FCreatedTime =DateTime.UtcNow,
            FUpdatedTime =DateTime.UtcNow
        };

        foreach (var place in request.Places)
        {
            var restaurantExists =
                await _context.TFoodMapPlaces
                    .AnyAsync(r =>r.FPlaceId ==place.FPlaceID);
            if (!restaurantExists)
            {
                throw new ArgumentException($"找不到店家 ID：{place.FPlaceID}");
            }

            trip.TFoodMapTripPlaces.Add(new TFoodMapTripPlace
                {
                    FPlaceId =place.FPlaceID,
                    FSortOrder =place.FSortOrder
                });
        }

        _context.TFoodMapTrips.Add(trip);

        await _context.SaveChangesAsync();

        return await GetTripByIdAsync(
            trip.FTripId)
            ?? throw new InvalidOperationException(
                "建立行程後無法取得資料。");
    }

    public async Task<TripDTO> FinalizeTripAsync(
            int tripId,
            GoogleTravelMode travelMode,
            CancellationToken cancellationToken = default)
    {
        var trip = await _context.TFoodMapTrips
            .Include(t => t.TFoodMapTripPlaces)
            .ThenInclude(tp => tp.FPlace)
            .FirstAsync(t => t.FTripId == tripId, cancellationToken);

        var orderedTripPlaces = trip.TFoodMapTripPlaces
            .OrderBy(tp => tp.FSortOrder)
            .ToList();

        // 少於 2 站不需要規劃路線，直接回傳目前的 Trip 資料
        if (orderedTripPlaces.Count < 2)
        {
            return MapToTripDto(trip);
        }

        var waypoints = orderedTripPlaces
            .Select(tp => ((double)tp.FPlace.FLatitude, (double)tp.FPlace.FLongitude))
            .ToList();

        GoogleRouteResult? routeResult;

        try
        {
            routeResult = await _routesClient.ComputeRouteAsync(waypoints, travelMode, cancellationToken);
        }
        catch (GoogleRoutesUnavailableException ex)
        {
            // Google Routes API 暫時掛掉：不讓整個流程失敗，
            // 行程本身（店家、順序）已經建立成功，路線資料留空，
            // 前端顯示「路線資料暫時無法取得，可稍後重試」
            _logger.LogWarning(ex, "計算 Trip {TripId} 路線失敗，行程仍保留，路線資料略過", tripId);
            return MapToTripDto(trip);
        }

        if (routeResult is not null)
        {
            // 先清掉這個 Trip 舊的逐段資料，避免順序變了之後留下對不上的舊紀錄
            var oldRoutes = _context.TFoodMapTripRoutes.Where(r => r.FTripId == tripId);
            _context.TFoodMapTripRoutes.RemoveRange(oldRoutes);

            for (var i = 0; i < routeResult.Legs.Count; i++)
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
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToTripDto(trip);
    }

    private TripDTO MapToTripDto(TFoodMapTrip trip)
    {
        return new TripDTO
        {
            FTripId = trip.FTripId,
            FTripName = trip.FTripName,
            Places = trip.TFoodMapTripPlaces
            .OrderBy(tp => tp.FSortOrder)
            .Select(tp => new TripPlaceDTO
            {
                FTripPlaceId = tp.FTripPlaceId,
                FLatitude = tp.FPlace.FLatitude,
                FLongitude = tp.FPlace.FLongitude,
                FPlaceId = tp.FPlace.FPlaceId,
                FPlaceName = tp.FPlace.FName,
                FAddress = tp.FPlace.FAddress,
                FSortOrder = tp.FSortOrder
            })
            .ToList()
        };
    }
}