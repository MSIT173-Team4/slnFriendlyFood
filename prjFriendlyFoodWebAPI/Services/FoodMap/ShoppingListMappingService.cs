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

        // 修正重點：
        // 1. 採買清單（食譜模組 RecipeService 存的）裡的 fIngredientID 是「標準食材表 tIngredient.fId」，
        //    但食材分類設定在 tFoodMapIngredient，兩張表的 ID 不是同一套。
        //    現在先用 tIngredient 取得食材名稱，再用「名稱」對到 tFoodMapIngredient 的分類。
        //    只有在 tIngredient 找不到這個 ID 時（例如早期直接塞進 tFoodMapIngredient 的測試資料），
        //    才退回用 ID 直接對 tFoodMapIngredient。
        // 2. 已經勾選「買到了」的品項不再列入規劃。
        // 3. 找不到分類對照的品項不再被默默丟掉，會放進 UnmappedItems，算進覆蓋率分母、列在「附近買不到」。
        public async Task<ShoppingListMappingResult> GetPlaceCategoriesForShoppingListAsync(
            int shoppingListId,
            CancellationToken cancellationToken = default)
        {
            var items = await _context.TFoodMapShoppingListItems
                .AsNoTracking()
                .Where(i => i.FShoppingListId == shoppingListId && !i.FIsPurchased)
                .OrderBy(i => i.FShoppingItemId)
                .Select(i => new
                {
                    i.FShoppingItemId,
                    i.FIngredientId,
                    i.FQuantity,
                    i.FUnit,
                    i.FIsPurchased
                })
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return new ShoppingListMappingResult();
            }

            var ingredientIds = items.Select(i => i.FIngredientId).Distinct().ToList();

            // 1-a. 標準食材表的名稱
            var standardNames = await _context.TIngredients
                .AsNoTracking()
                .Where(i => ingredientIds.Contains(i.FId))
                .Select(i => new { i.FId, i.FName })
                .ToDictionaryAsync(i => i.FId, i => i.FName.Trim(), cancellationToken);

            // 1-b. 用名稱對 tFoodMapIngredient 的分類
            var namesToMatch = standardNames.Values.Distinct().ToList();

            var foodMapByName = (await _context.TFoodMapIngredients
                    .AsNoTracking()
                    .Where(i => namesToMatch.Contains(i.FName))
                    .Select(i => new { i.FIngredientId, i.FName, i.FIngredientCategoryId })
                    .ToListAsync(cancellationToken))
                .GroupBy(i => i.FName.Trim())
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.FIngredientId).First());

            // 1-c. tIngredient 找不到的 ID，退回直接用 ID 對 tFoodMapIngredient
            var legacyIds = ingredientIds.Where(id => !standardNames.ContainsKey(id)).ToList();

            var foodMapById = new Dictionary<int, (string Name, int CategoryId)>();

            if (legacyIds.Count > 0)
            {
                var legacyRows = await _context.TFoodMapIngredients
                    .AsNoTracking()
                    .Where(i => legacyIds.Contains(i.FIngredientId))
                    .Select(i => new { i.FIngredientId, i.FName, i.FIngredientCategoryId })
                    .ToListAsync(cancellationToken);

                foreach (var row in legacyRows)
                {
                    foodMapById[row.FIngredientId] = (row.FName.Trim(), row.FIngredientCategoryId);
                }
            }

            // 每個清單品項 → (顯示名稱, 食材分類 ID 或 null)
            var resolved = items
                .Select(item =>
                {
                    if (standardNames.TryGetValue(item.FIngredientId, out var standardName))
                    {
                        int? categoryId = foodMapByName.TryGetValue(standardName, out var match)
                            ? match.FIngredientCategoryId
                            : null;

                        return (Item: item, Name: standardName, IngredientCategoryId: categoryId);
                    }

                    if (foodMapById.TryGetValue(item.FIngredientId, out var legacy))
                    {
                        return (Item: item, Name: legacy.Name, IngredientCategoryId: (int?)legacy.CategoryId);
                    }

                    return (Item: item, Name: $"食材 #{item.FIngredientId}", IngredientCategoryId: (int?)null);
                })
                .ToList();

            // 2. 食材分類 → 店家分類（只取啟用中的對照，依優先順序）
            var ingredientCategoryIds = resolved
                .Where(r => r.IngredientCategoryId.HasValue)
                .Select(r => r.IngredientCategoryId!.Value)
                .Distinct()
                .ToList();

            var categoryMappings = await _context.TFoodMapIngredientPlaceCategories
                .AsNoTracking()
                .Where(m => m.FIsActive && ingredientCategoryIds.Contains(m.FIngredientCategoryId))
                .OrderBy(m => m.FPriority)
                .Select(m => new { m.FIngredientCategoryId, m.FPlaceCategoryId })
                .ToListAsync(cancellationToken);

            var placeCategoriesByIngredientCategory = categoryMappings
                .GroupBy(m => m.FIngredientCategoryId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.FPlaceCategoryId).Distinct().ToList());

            // 3. 組出結果
            var result = new ShoppingListMappingResult
            {
                TotalItemCount = items.Count
            };

            foreach (var (item, name, ingredientCategoryId) in resolved)
            {
                var itemDto = new ShoppingListItemDTO
                {
                    FShoppingListItemId = item.FShoppingItemId,
                    FIngredientId = item.FIngredientId,
                    FIngredientName = name,
                    FQuantity = item.FQuantity,
                    FUnit = item.FUnit,
                    FIsPurchased = item.FIsPurchased
                };

                result.AllItems.Add(itemDto);

                if (ingredientCategoryId is null
                    || !placeCategoriesByIngredientCategory.TryGetValue(ingredientCategoryId.Value, out var placeCategoryIds)
                    || placeCategoryIds.Count == 0)
                {
                    result.UnmappedItems.Add(itemDto);
                    continue;
                }

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
