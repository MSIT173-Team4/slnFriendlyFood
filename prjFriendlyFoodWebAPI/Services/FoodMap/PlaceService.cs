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
        private static readonly Dictionary<long, string> CategoryToGooglePlaceTypeMap = new()
        {
            [1] = "食品",
            [2] = "賣場",
            [3] = "市場",
            [4] = "餐廳"
        };
        private readonly FriendlyFoodDbContext _context;
        private readonly IGooglePlacesClient _googlePlacesClient;

        public PlaceService(FriendlyFoodDbContext context, IGooglePlacesClient googlePlacesClient)
        {
            _context = context;
            _googlePlacesClient = googlePlacesClient;
        }
        private static TFoodMapPlace MapGooglePlaceToRestaurant(GooglePlace googlePlace, int categoryId)
        {
            return new TFoodMapPlace
            {
                FGooglePlaceId =
                    googlePlace.Id,

                FPlaceCategoryId =
                    categoryId,

                FName =
                    googlePlace.DisplayName?.Text
                    ?? string.Empty,

                FAddress =
                    googlePlace.FormattedAddress
                    ?? string.Empty,

                FLatitude =
                    (decimal)(
                        googlePlace.Location?.Latitude
                        ?? 0),

                FLongitude =
                    (decimal)(
                        googlePlace.Location?.Longitude
                        ?? 0),

                FPhone =
                    googlePlace.NationalPhoneNumber,

                FGoogleRating =
                    googlePlace.Rating.HasValue
                        ? (decimal)googlePlace.Rating.Value
                        : null,

                FGoogleReviewCount =
                    googlePlace.UserRatingCount,

                FBusinessStatus =
                    googlePlace.BusinessStatus,

                FIsActive = true,

                // 記錄這筆快照的同步時間，供之後判斷資料是否過期（第 60 章的背景排程會用到）。
                FSyncedAt =
                    DateTime.UtcNow,

                FCreatedTime =
                    DateTime.UtcNow
            };
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

        public async Task<NearbyResponseDTO> GetNearbyPlacesAsync(NearbyRequestDTO request, CancellationToken cancellationToken = default)
        {
            var minimumResults = request.MinimumRequests > 0 ? request.MinimumRequests : DefaultMinimumResults;
            string? googlePlaceType = null;
            if (request.FPlacesCategoryId.HasValue)
            {
                // 【方案 B】改查字典，不查資料庫。
                CategoryToGooglePlaceTypeMap.TryGetValue(
                    request.FPlacesCategoryId.Value,
                    out googlePlaceType);
            }
            var places =
                await _googlePlacesClient
                    .SearchNearbyAsync(
                        request.FLatitude,
                        request.FLongitude,
                        InitialRadiusKm * 1000,
                        googlePlaceType,
                        cancellationToken);

            var expandedSearch = false;

            var searchRadiusKm =
                InitialRadiusKm;

            if (places.Count < minimumResults)
            {
                places =
                    await _googlePlacesClient
                        .SearchNearbyAsync(
                            (double)request.FLatitude,
                            (double)request.FLongitude,
                            ExpandedRadiusKm * 1000,
                            googlePlaceType,
                            cancellationToken);

                expandedSearch = true;

                searchRadiusKm =
                    ExpandedRadiusKm;
            }

            var result =
                places.Select(p => new PlaceDTO
                {
                    FPlaceId = 0,

                    FGooglePlaceId = p.Id,

                    FName =
                        p.DisplayName?.Text
                        ?? string.Empty,

                    FAddress =
                        p.FormattedAddress
                        ?? string.Empty,

                    FLatitude =
                        (decimal)(
                            p.Location?.Latitude
                            ?? 0),

                    FLongitude =
                        (decimal)(
                            p.Location?.Longitude
                            ?? 0),

                    FPhone =
                        p.NationalPhoneNumber,

                    FGoogleRating =
                        p.Rating.HasValue
                            ? (decimal)p.Rating.Value
                            : null,

                    FGoogleReviewCount =
                        p.UserRatingCount,

                    FBusinessStatus =
                        p.BusinessStatus,

                    FIsRecommend = false
                }).ToList();

            return new NearbyResponseDTO
            {
                FLatitude = request.FLatitude,

                FLongitude = request.FLongitude,

                SearchRadiusKm =
                    searchRadiusKm,

                ExpandedSearch =
                    expandedSearch,

                ResultCount =
                    result.Count,

                Places =
                    result
            };
        }

    }
}



