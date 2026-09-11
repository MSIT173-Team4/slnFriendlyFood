
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

public class TripServices : ITripServices
{
    private readonly FriendlyFoodDbContext _context;

    public TripServices(FriendlyFoodDbContext context)
    {
        _context = context;
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
    public async Task<TripDTO?>GetTripByIdAsync(long id)
    {
        return await _context.TFoodMapTrips
            .AsNoTracking()
            .Where(t =>
                t.FTripId == id)
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
    public async Task<TripDTO> CreatTripAsync(TripDTO tripDTO)
    {
        var trip = new TFoodMapTrip
        {
            FTripName =tripDTO.FTripName,
            FCreatedTime =DateTime.UtcNow,
            FUpdatedTime =DateTime.UtcNow
        };

        foreach (var place in tripDTO.Places)
        {
            var restaurantExists =
                await _context.TFoodMapPlaces
                    .AnyAsync(r =>r.FPlaceId ==place.FPlaceId);
            if (!restaurantExists)
            {
                throw new ArgumentException($"找不到店家 ID：{place.FPlaceId}");
            }

            trip.TFoodMapTripPlaces.Add(new TFoodMapTripPlace
                {
                    FPlaceId =place.FPlaceId,
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

}