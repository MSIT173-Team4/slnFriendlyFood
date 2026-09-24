using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Exceptions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class TripPlanningService : ITripPlanningService
    {
        private static readonly Dictionary<int, string> CategoryToGooglePlaceTypeMap = new()
        {
            [1] = "grocery_store",
            [2] = "supermarket",
            [3] = "warehouse_store",
            [4] = "convenience_store"
        };

        private readonly IShoppingListMappingService _mappingService;
        private readonly IRecommendationServices _recommendationService;
        private readonly ITripOptimizationService _tripOptimizationService;
        private readonly IFoodMapService _foodmap;
        private readonly IGooglePlacesClient _placesClient;
        private readonly ITripServices _tripService;
        private readonly ILogger<TripPlanningService> _logger;

        public TripPlanningService(
            IShoppingListMappingService mappingService,
            IRecommendationServices recommendationService,
            ITripOptimizationService tripOptimizationService,
            IFoodMapService foodmap,
            IGooglePlacesClient placesClient,
            ITripServices tripService,
            ILogger<TripPlanningService> logger)
        {
            _mappingService = mappingService;
            _recommendationService = recommendationService;
            _tripOptimizationService = tripOptimizationService;
            _placesClient = placesClient;
            _tripService = tripService;
            _logger = logger;
            _foodmap = foodmap;
        }

        public async Task<PlanTripResultDTO> PlanTripAsync(
            int userId,
            PlanTripRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var mapping = await _mappingService.GetPlaceCategoriesForShoppingListAsync(
                request.ShoppingListId, cancellationToken);

            if (mapping.TotalItemCount == 0)
            {
                throw new ArgumentException("這份採買清單是空的，無法規劃行程");
            }

            var relevantPlaceCategoryIds = mapping.ItemsByPlaceCategory.Keys.ToList();

            var googleResultsByCategory = new List<(int PlaceCategoryId, GooglePlace GooglePlace)>();

            foreach (var placeCategoryId in relevantPlaceCategoryIds)
            {
                if (!CategoryToGooglePlaceTypeMap.TryGetValue(placeCategoryId, out var googlePlaceType))
                {
                    continue;
                }

                var googlePlaces = await _placesClient.SearchNearbyAsync(
                    (double)request.OriginLatitude,
                    (double)request.OriginLongitude,
                    request.SearchRadiusMeters,
                    googlePlaceType,
                    cancellationToken);

                foreach (var googlePlace in googlePlaces)
                {
                    googleResultsByCategory.Add((placeCategoryId, googlePlace));
                }
            }

            if (googleResultsByCategory.Count == 0)
            {
                throw new ArgumentException("附近找不到任何符合這份清單需求的店家，建議放大搜尋範圍");
            }

            var candidatePlaces = new List<PlaceCandidateWithLocationDto>();
            var seenGooglePlaceIds = new HashSet<string>();

            foreach (var (placeCategoryId, googlePlace) in googleResultsByCategory)
            {
                if (!seenGooglePlaceIds.Add(googlePlace.Id))
                {
                    continue;
                }

                var internalPlaceId = await _foodmap.ResolvePlaceAsync(
                    new ResolvePlaceRequestDTO
                    {
                        FGooglePlaceId = googlePlace.Id,
                        FPlaceCategoryId = placeCategoryId
                    },
                    cancellationToken);

                candidatePlaces.Add(new PlaceCandidateWithLocationDto
                {
                    FPlaceId = internalPlaceId,
                    FName = googlePlace.DisplayName?.Text ?? string.Empty,
                    FPlaceCategoryId = placeCategoryId,
                    Latitude = googlePlace.Location?.Latitude ?? 0,
                    Longitude = googlePlace.Location?.Longitude ?? 0
                });
            }

            if (candidatePlaces.Count == 0)
            {
                throw new ArgumentException("附近找不到任何符合這份清單需求的店家，建議放大搜尋範圍");
            }

            var optimized = await _tripOptimizationService.OptimizeAsync(
                request.ShoppingListId,
                request.OriginLatitude,
                request.OriginLongitude,
                candidatePlaces,
                cancellationToken);

            if (optimized.PlaceCoverages.Count == 0)
            {
                throw new ArgumentException("找不到任何能滿足清單需求的店家組合");
            }

            var recommendedPlaceIds = await _recommendationService.GetActiveRecommendedPlaceIdsAsync(
                cancellationToken);

            foreach (var place in optimized.PlaceCoverages)
            {
                place.IsRecommend = recommendedPlaceIds.Contains(place.FPlaceId);
            }

            var createRequest = new CreateTripRequestDTO
            {
                FTripName = $"採買行程 {DateTime.Now:MM/dd HH:mm}",
                Places = optimized.PlaceCoverages
                    .Select((place, index) => new CreateTripPlacesRequestDTO
                    {
                        FPlaceID = place.FPlaceId,
                        FSortOrder = index + 1
                    })
                    .ToList()
            };

            var trip = await _tripService.CreateTripAsync(createRequest, cancellationToken);

            try
            {
                trip = await _tripService.FinalizeTripAsync(
                    trip.FTripId, request.TravelMode, cancellationToken);
            }
            catch (GoogleRoutesUnavailableException ex)
            {
                _logger.LogWarning(ex, "Trip {TripId} 建立成功，但路線計算暫時失敗", trip.FTripId);
            }

            return new PlanTripResultDTO
            {
                Trip = trip,
                FinalCoveragePercentage = optimized.FinalCoveragePercentage,
                UncoveredItemNames = optimized.UncoveredItemNames
            };
        }
    }


}

