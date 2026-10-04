using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

public sealed class RecipeDevelopmentDataSeeder(
    FriendlyFoodDbContext context,
    ILogger<RecipeDevelopmentDataSeeder> logger) : IRecipeDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var recipeOwner = await GetRecipeOwnerAsync(cancellationToken);
            var categories = await EnsureCategoriesAsync(cancellationToken);
            var ingredients = await EnsureIngredientsAsync(cancellationToken);
            var tags = await EnsureTagsAsync(cancellationToken);

            await EnsureRecipesAsync(
                recipeOwner,
                categories,
                ingredients,
                tags,
                cancellationToken);
            await EnsurePantryAsync(
                recipeOwner,
                ingredients,
                cancellationToken);
            await EnsureEngagementAsync(cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Recipe 開發測試資料已確認完成。");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<TUser> GetRecipeOwnerAsync(
        CancellationToken cancellationToken)
    {
        var recipeOwner = await context.TUsers.SingleOrDefaultAsync(
            user => user.FUsername == RecipeSeedDefinitions.DemoOwnerUsername,
            cancellationToken);
        if (recipeOwner is null)
        {
            throw new InvalidOperationException(
                $"Recipe 種子資料需要會員模組先建立帳號「{RecipeSeedDefinitions.DemoOwnerUsername}」。");
        }

        if (!recipeOwner.FIsActive)
        {
            throw new InvalidOperationException(
                $"會員帳號「{RecipeSeedDefinitions.DemoOwnerUsername}」必須是啟用狀態，Recipe 整合測試才能登入。");
        }

        return recipeOwner;
    }

    private async Task<Dictionary<string, TRecipeCategory>> EnsureCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var names = RecipeSeedDefinitions.Categories.Select(item => item.Name).ToArray();
        var existing = await context.TRecipeCategories
            .Where(item => names.Contains(item.FCategoryName))
            .ToListAsync(cancellationToken);
        var categories = existing.ToDictionary(item => item.FCategoryName);

        foreach (var definition in RecipeSeedDefinitions.Categories)
        {
            if (categories.ContainsKey(definition.Name))
            {
                continue;
            }

            var category = new TRecipeCategory
            {
                FCategoryName = definition.Name,
                FDisplayOrder = definition.Order
            };
            categories[definition.Name] = category;
            context.TRecipeCategories.Add(category);
        }

        await context.SaveChangesAsync(cancellationToken);
        return categories;
    }

    private async Task<Dictionary<string, TIngredient>> EnsureIngredientsAsync(
        CancellationToken cancellationToken)
    {
        var names = RecipeSeedDefinitions.Recipes
            .SelectMany(recipe => recipe.Ingredients)
            .Select(ingredient => ingredient.Name)
            .Concat(["紅蘿蔔", "雞蛋", "無鹽奶油"])
            .Distinct()
            .ToArray();
        var existing = await context.TIngredients
            .Where(item => names.Contains(item.FName))
            .ToListAsync(cancellationToken);
        var ingredients = existing.ToDictionary(item => item.FName);

        foreach (var name in names)
        {
            if (ingredients.ContainsKey(name))
            {
                continue;
            }

            var ingredient = new TIngredient { FName = name };
            ingredients[name] = ingredient;
            context.TIngredients.Add(ingredient);
        }

        await context.SaveChangesAsync(cancellationToken);
        return ingredients;
    }

    private async Task<Dictionary<string, TRecipeTag>> EnsureTagsAsync(
        CancellationToken cancellationToken)
    {
        var names = RecipeSeedDefinitions.Tags.Select(item => item.Name).ToArray();
        var existing = await context.TRecipeTags
            .Where(item => names.Contains(item.FTagName))
            .ToListAsync(cancellationToken);
        var tags = existing.ToDictionary(item => item.FTagName);

        foreach (var definition in RecipeSeedDefinitions.Tags)
        {
            if (tags.ContainsKey(definition.Name))
            {
                continue;
            }

            var tag = new TRecipeTag
            {
                FTagType = definition.Type,
                FTagName = definition.Name
            };
            tags[definition.Name] = tag;
            context.TRecipeTags.Add(tag);
        }

        await context.SaveChangesAsync(cancellationToken);
        return tags;
    }

    private async Task EnsureRecipesAsync(
        TUser recipeOwner,
        IReadOnlyDictionary<string, TRecipeCategory> categories,
        IReadOnlyDictionary<string, TIngredient> ingredients,
        IReadOnlyDictionary<string, TRecipeTag> tags,
        CancellationToken cancellationToken)
    {
        foreach (var definition in RecipeSeedDefinitions.Recipes)
        {
            var recipesWithSameTitle = await context.TRecipes
                .Where(item => item.FTitle == definition.Title)
                .OrderBy(item => item.FRecipeId)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (recipesWithSameTitle.Count > 1)
            {
                throw new InvalidOperationException(
                    $"食譜「{definition.Title}」有重複資料，請先在整合資料庫保留唯一一筆後再執行 Recipe Seeder。");
            }

            if (recipesWithSameTitle.Count == 1)
            {
                continue;
            }

            var recipe = new TRecipe
            {
                FUserId = recipeOwner.FId,
                FCategoryId = categories[definition.Category].FCategoryId,
                FTitle = definition.Title,
                FDescription = definition.Description,
                FCoverImageUrl = definition.CoverImageUrl,
                FYtVideoId = definition.YouTubeVideoId,
                FAiPrepTips = $"資料來源：{definition.SourceName}｜{definition.SourceUrl}。{definition.SafetyNote}",
                FIsAiGenerated = definition.IsAiGenerated,
                FDefaultServings = definition.Servings,
                FCookingMinutes = definition.CookingMinutes,
                FTotalCalories = definition.Calories,
                FViews = definition.SeedViewCount,
                FStatus = 1,
                FCreatedAt = DateTime.UtcNow.AddDays(-definition.PublishedDaysAgo),
                FUpdatedAt = DateTime.UtcNow
            };
            context.TRecipes.Add(recipe);
            await context.SaveChangesAsync(cancellationToken);

            context.TRecipeIngredients.AddRange(definition.Ingredients.Select((item, index) =>
                new TRecipeIngredient
                {
                    FRecipeId = recipe.FRecipeId,
                    FIngredientId = ingredients[item.Name].FId,
                    FDisplayAmount = item.DisplayAmount,
                    FBaseAmount = item.BaseAmount,
                    FStandardUnit = item.Unit,
                    FIsMain = item.IsMain,
                    FSortOrder = (short)(index + 1)
                }));

            context.TRecipeSteps.AddRange(definition.Steps.Select((item, index) =>
                new TRecipeStep
                {
                    FRecipeId = recipe.FRecipeId,
                    FStepNumber = (short)(index + 1),
                    FInstruction = item.Instruction,
                    FImageUrl = definition.CoverImageUrl,
                    FTimerSeconds = item.TimerSeconds
                }));

            context.TRecipeTagMappings.AddRange(definition.Tags.Select(tagName => new TRecipeTagMapping
            {
                FRecipeId = recipe.FRecipeId,
                FTagId = tags[tagName].FTagId
            }));

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsurePantryAsync(
        TUser pantryOwner,
        IReadOnlyDictionary<string, TIngredient> ingredients,
        CancellationToken cancellationToken)
    {
        var pantryDefinitions = new[]
        {
            ("菠菜", 150m, "g", 1),
            ("板豆腐", 200m, "g", 2),
            ("紅蘿蔔", 2m, "根", 7),
            ("鮮香菇", 200m, "g", 5),
            ("雞蛋", 6m, "顆", 9),
            ("無鹽奶油", 120m, "g", 18)
        };

        foreach (var definition in pantryDefinitions)
        {
            var ingredientId = ingredients[definition.Item1].FId;
            var exists = await context.TRecipeUserPantries.AnyAsync(
                item => item.FUserId == pantryOwner.FId && item.FIngredientId == ingredientId,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            context.TRecipeUserPantries.Add(new TRecipeUserPantry
            {
                FUserId = pantryOwner.FId,
                FIngredientId = ingredientId,
                FAmount = definition.Item2,
                FUnit = definition.Item3,
                FExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(definition.Item4)),
                FCreatedAt = DateTime.UtcNow,
                FStorageLocation = "冷藏",
                FNote = "Recipe 模組整合測試資料"
            });
        }
    }

    private async Task EnsureEngagementAsync(CancellationToken cancellationToken)
    {
        var recipeTitles = RecipeSeedDefinitions.Recipes.Select(seed => seed.Title).ToArray();
        var recipes = await context.TRecipes
            .Where(item => recipeTitles.Contains(item.FTitle))
            .ToListAsync(cancellationToken);
        var recipesByTitle = recipes.ToDictionary(recipe => recipe.FTitle);
        var recipeIds = recipes.Select(recipe => recipe.FRecipeId).ToArray();
        var engagementUsers = (await context.TUsers
                .AsNoTracking()
                .Where(user => user.FIsActive)
                .ToListAsync(cancellationToken))
            .OrderBy(user => user.FUsername)
            .ToArray();
        var existingLikeKeys = (await context.TRecipeLikes
                .Where(item => recipeIds.Contains(item.FRecipeId))
                .Select(item => new { item.FRecipeId, item.FUserId })
                .ToListAsync(cancellationToken))
            .Select(item => (item.FRecipeId, item.FUserId))
            .ToHashSet();
        var existingFavoriteKeys = (await context.TRecipeFavorites
                .Where(item => recipeIds.Contains(item.FRecipeId))
                .Select(item => new { item.FRecipeId, item.FUserId })
                .ToListAsync(cancellationToken))
            .Select(item => (item.FRecipeId, item.FUserId))
            .ToHashSet();

        foreach (var definition in RecipeSeedDefinitions.Recipes)
        {
            var recipe = recipesByTitle[definition.Title];
            var eligibleUsers = engagementUsers
                .Where(user => user.FId != recipe.FUserId)
                .ToArray();

            foreach (var user in eligibleUsers.Take(definition.SeedLikeCount))
            {
                if (existingLikeKeys.Add((recipe.FRecipeId, user.FId)))
                {
                    context.TRecipeLikes.Add(new TRecipeLike
                    {
                        FRecipeId = recipe.FRecipeId,
                        FUserId = user.FId
                    });
                }
            }

            foreach (var user in eligibleUsers.Take(definition.SeedFavoriteCount))
            {
                if (existingFavoriteKeys.Add((recipe.FRecipeId, user.FId)))
                {
                    context.TRecipeFavorites.Add(new TRecipeFavorite
                    {
                        FRecipeId = recipe.FRecipeId,
                        FUserId = user.FId
                    });
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var likeCounts = await context.TRecipeLikes
            .Where(item => recipeIds.Contains(item.FRecipeId))
            .GroupBy(item => item.FRecipeId)
            .Select(group => new { RecipeId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RecipeId, item => item.Count, cancellationToken);
        var favoriteCounts = await context.TRecipeFavorites
            .Where(item => recipeIds.Contains(item.FRecipeId))
            .GroupBy(item => item.FRecipeId)
            .Select(group => new { RecipeId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RecipeId, item => item.Count, cancellationToken);

        foreach (var recipe in recipes)
        {
            recipe.FLikes = likeCounts.GetValueOrDefault(recipe.FRecipeId);
            recipe.FFavorites = favoriteCounts.GetValueOrDefault(recipe.FRecipeId);
        }
    }

}
