using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class RecommendationService : IRecommendationServices
    {
        private readonly FriendlyFoodDbContext _context;
        public RecommendationService(FriendlyFoodDbContext context)
        {
            _context = context;
        }
        public async Task<List<RecommendationDTO>> GetRecommendationDTOsAsync(int shoppinglistId)
        {
            var items = await _context.TFoodMapShoppingListItems
                .AsNoTracking()
                .Where(
                x => x.FShoppingListId == shoppinglistId
                )
                .Select(x => new
                {
                    x.FShoppingItemId,
                    x.FUnit
                })
                .ToListAsync();

            if (items.Count == 0)
            {
                return [];
            }

            var itemNames =
                items
                    .Select(x =>
                        x.FUnit.Trim())
                    .Where(x =>
                        x != string.Empty)
                    .ToList();

            if (itemNames.Count == 0)
            {
                return [];
            }

            var places =
                await _context.TFoodMapPlaces
                    .AsNoTracking()
                    .Where(r =>
                        (bool)r.FIsRecommend)
                    .Select(r => new
                    {
                        r.FPlaceId,
                        r.FName,
                        r.FGoogleRating,
                        r.FIsRecommend
                    })
                    .ToListAsync();

            var result =
                places
                    .Select(r => new RecommendationDTO
                    {
                        FPlaceId =
                            r.FPlaceId,

                        FTitle =
                            r.FName,

                        FGoogleRating =
                            r.FGoogleRating,

                        FDistance = 0,

                        isRecommend =
                            (bool)r.FIsRecommend,

                        MatchedShoppingItemIDs =
                            items
                                .Select(i =>
                                    i.FShoppingItemId)
                                .ToList(),

                        MatchedItemNames =
                            items
                                .Select(i =>
                                    i.FUnit)
                                .ToList()
                    })
                    .ToList();

            return result;
        }

    }

}

