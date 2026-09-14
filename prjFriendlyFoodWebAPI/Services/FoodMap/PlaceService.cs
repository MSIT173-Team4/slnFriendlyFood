using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class PlaceService : IFoodMapService
    {
        private readonly FriendlyFoodDbContext _context;
        public PlaceService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlaceDTO>> GetPlacesAsync()
        {
            return await _context.TFoodMapPlaces
                .AsNoTracking()
                .Select(r => new PlaceDTO
                {
                    FPlaceId = r.FPlaceId,
                    FName = r.FName,
                    FAddress = r.FAddress,
                    FLatitude = r.FLatitude,
                    FLongitude = r.FLongitude,
                    FPhone = r.FPhone,
                    FGoogleRating = r.FGoogleRating,
                    FGoogleReviewCount = r.FGoogleReviewCount,
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
                    FName = r.FName,
                    FAddress = r.FAddress,
                    FLatitude = r.FLatitude,
                    FLongitude = r.FLongitude,
                    FPhone = r.FPhone,
                    FGoogleRating = r.FGoogleRating,
                    FGoogleReviewCount = r.FGoogleReviewCount,
                    FIsRecommend = r.TFoodMapRecommendationPlaces.Any(item => item.FIsRecommend)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<PlaceDTO>> GetNearbyPlacesAsync(PlacesDTO request)
        {
            // Step 1：算出 Bounding Box 邊界（粗篩，SQL 端執行）
            var latDelta = request.Radius / 111m; // 緯度 1 度約等於 111 公里
            var lngDelta = request.Radius / (111m * (decimal)Math.Cos((double)request.Latitude * Math.PI / 180));

            var minLat = request.Latitude - latDelta;
            var maxLat = request.Latitude + latDelta;
            var minLng = request.Longitude - lngDelta;
            var maxLng = request.Longitude + lngDelta;

            var candidates = await _context.TFoodMapPlaces
                .AsNoTracking()
                .Where(r =>
                    r.FLatitude >= minLat && r.FLatitude <= maxLat &&
                    r.FLongitude >= minLng && r.FLongitude <= maxLng)
                .Include(place => place.TFoodMapRecommendationPlaces)
                .ToListAsync();

            // Step 2：精確計算距離（記憶體端執行，資料量已經很小）
            var result = candidates
                .Select(r => new
                {
                    Place = r,
                    DistanceKm = CalculateDistanceKm(
                        (double)request.Latitude, (double)request.Longitude,
                        (double)r.FLatitude, (double)r.FLongitude)
                })
                .Where(x => x.DistanceKm <= (double)request.Radius)
                .OrderBy(x => x.DistanceKm)
                .Select(x => new PlaceDTO
                {
                    FPlaceId = x.Place.FPlaceId,
                    FName = x.Place.FName,
                    FAddress = x.Place.FAddress,
                    FLatitude = x.Place.FLatitude,
                    FLongitude = x.Place.FLongitude,
                    FPhone = x.Place.FPhone,
                    FGoogleRating = x.Place.FGoogleRating,
                    FGoogleReviewCount = x.Place.FGoogleReviewCount,
                    FIsRecommend = x.Place.TFoodMapRecommendationPlaces.Any(item => item.FIsRecommend)
                })
                .ToList();
            return result;
        }

        private static double CalculateDistanceKm(
            double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371;

            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        public async Task<List<PlaceDTO>> GetNearbyPlacesWithFallbackAsync( decimal latitude, decimal longitude)
        {
            decimal[] searchRadiusSteps = [1m, 3m];
            const int minimumResultCount = 5; // 可依需求調整門檻

            List<PlaceDTO> result = [];

            foreach (var radius in searchRadiusSteps)
            {
                result = await GetNearbyPlacesAsync(new PlacesDTO
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Radius = radius
                }   
                );

                if (result.Count >= minimumResultCount)
                {
                    break;
                }
            }
            return result;
        }
    }
}
