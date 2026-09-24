using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.FoodMap;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    public class ShoppingListMappingService : IShoppingListMappingService
    {
        private readonly FriendlyFoodDbContext _context;

        public ShoppingListMappingService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingListMappingResult> GetPlaceCategoriesForShoppingListAsync(
            int shoppingListId,
            CancellationToken cancellationToken = default)
        {
            // 1. 撈出這份清單裡所有品項（一次撈完，不在迴圈裡查）
            var items = await _context.TFoodMapShoppingListItems
                .Where(i => i.FShoppingListId == shoppingListId)
                .Select(i => new
                {
                    i.FShoppingItemId,
                    i.FIngredientId,
                    i.FQuantity,
                    i.FIsPurchased
                })
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return new ShoppingListMappingResult();
            }

            // 2. 一次撈出所有相關 Ingredient（含分類），避免 N+1
            var ingredientIds = items.Select(i => i.FIngredientId).Distinct().ToList();

            var ingredients = await _context.TFoodMapIngredients
                .Where(ing => ingredientIds.Contains(ing.FIngredientId))
                .Select(ing => new { ing.FIngredientId, ing.FName, ing.FIngredientCategoryId })
                .ToListAsync(cancellationToken);

            var ingredientById = ingredients.ToDictionary(i => i.FIngredientId);

            // 3. 一次撈出所有相關的「食材分類 → 店家分類」對照
            var ingredientCategoryIds = ingredients
                .Select(i => i.FIngredientCategoryId)
                .Distinct()
                .ToList();

            var categoryMappings = await _context.TFoodMapIngredientPlaceCategories
                .Where(m => ingredientCategoryIds.Contains(m.FIngredientCategoryId))
                .Select(m => new { m.FIngredientCategoryId, m.FPlaceCategoryId })
                .ToListAsync(cancellationToken);

            // 用 GroupBy 在記憶體裡組出：IngredientCategoryId -> 可對應的 PlaceCategoryId 清單
            var placeCategoriesByIngredientCategory = categoryMappings
                .GroupBy(m => m.FIngredientCategoryId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.FPlaceCategoryId).ToList());

            // 4. 組出最終結果：Dictionary<店家分類ID, List<可在此分類買到的Item>>
            var result = new ShoppingListMappingResult
            {
                TotalItemCount = items.Count
            };

            foreach (var item in items)
            {
                if (!ingredientById.TryGetValue(item.FIngredientId, out var ingredient))
                {
                    // 食材主檔裡找不到對應資料（資料異常），跳過，建議另外記 log
                    continue;
                }

                if (!placeCategoriesByIngredientCategory.TryGetValue(
                        ingredient.FIngredientCategoryId, out var placeCategoryIds))
                {
                    // 這個食材分類還沒有設定任何對應的店家分類
                    continue;
                }

                var itemDto = new ShoppingListItemDTO
                {
                    FShoppingListItemId = item.FShoppingItemId,
                    FIngredientId = item.FIngredientId,
                    FIngredientName = ingredient.FName,
                    FQuantity = item.FQuantity,
                    FIsPurchased = item.FIsPurchased
                };

                foreach (var placeCategoryId in placeCategoryIds)
                {
                    if (!result.ItemsByPlaceCategory.TryGetValue(placeCategoryId, out var list))
                    {
                        list = new List<ShoppingListItemDTO>();
                        result.ItemsByPlaceCategory[placeCategoryId] = list;
                    }

                    list.Add(itemDto);
                }
            }

            return result;
        }
    }
    }

