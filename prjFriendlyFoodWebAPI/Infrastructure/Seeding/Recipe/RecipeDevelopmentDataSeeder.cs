using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Member;

namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

public sealed class RecipeDevelopmentDataSeeder(
    FriendlyFoodDbContext context,
    ILogger<RecipeDevelopmentDataSeeder> logger,
    EncodeServices passwordEncoder) : IRecipeDataSeeder
{
    private const string DemoPassword = "DemoOnly2026!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var users = await EnsureUsersAsync(cancellationToken);
            var categories = await EnsureCategoriesAsync(cancellationToken);
            var ingredients = await EnsureIngredientsAsync(cancellationToken);
            var tags = await EnsureTagsAsync(cancellationToken);

            await EnsureRecipesAsync(
                users,
                categories,
                ingredients,
                tags,
                cancellationToken);
            await EnsurePantryAsync(
                users[RecipeSeedDefinitions.DemoTesterUsername],
                ingredients,
                cancellationToken);
            await EnsureEngagementAsync(users, cancellationToken);

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

    private async Task<Dictionary<string, TUser>> EnsureUsersAsync(
        CancellationToken cancellationToken)
    {
        var usernames = RecipeSeedDefinitions.Users.Select(user => user.Username).ToArray();
        var existingUsers = await context.TUsers
            .Where(user => usernames.Contains(user.FUsername))
            .ToListAsync(cancellationToken);
        var users = existingUsers.ToDictionary(user => user.FUsername);

        foreach (var definition in RecipeSeedDefinitions.Users)
        {
            if (!users.TryGetValue(definition.Username, out var user))
            {
                var encodedPassword = await passwordEncoder.HashPassword(DemoPassword);
                user = CreateUser(definition, encodedPassword);
                users[definition.Username] = user;
                context.TUsers.Add(user);
            }
            else if (user.FPassword == DemoPassword)
            {
                user.FPassword = await passwordEncoder.HashPassword(DemoPassword);
            }

            user.FIsActive = definition.IsActive;
        }

        await context.SaveChangesAsync(cancellationToken);
        return users;
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
        IReadOnlyDictionary<string, TUser> users,
        IReadOnlyDictionary<string, TRecipeCategory> categories,
        IReadOnlyDictionary<string, TIngredient> ingredients,
        IReadOnlyDictionary<string, TRecipeTag> tags,
        CancellationToken cancellationToken)
    {
        foreach (var definition in RecipeSeedDefinitions.Recipes)
        {
            var recipe = await context.TRecipes
                .FirstOrDefaultAsync(item => item.FTitle == definition.Title, cancellationToken);

            if (recipe is null)
            {
                recipe = new TRecipe
                {
                    FTitle = definition.Title,
                    FCreatedAt = DateTime.UtcNow
                };
                context.TRecipes.Add(recipe);
            }

            recipe.FUserId = users[definition.AuthorUsername].FId;
            recipe.FCategoryId = categories[definition.Category].FCategoryId;
            recipe.FDescription = definition.Description;
            recipe.FCoverImageUrl = definition.CoverImageUrl;
            recipe.FYtVideoId = definition.YouTubeVideoId;
            recipe.FAiPrepTips = $"資料來源：{definition.SourceName}｜{definition.SourceUrl}。{definition.SafetyNote}";
            recipe.FIsAiGenerated = definition.IsAiGenerated;
            recipe.FDefaultServings = definition.Servings;
            recipe.FCookingMinutes = definition.CookingMinutes;
            recipe.FTotalCalories = definition.Calories;
            recipe.FViews = Math.Max(recipe.FViews, definition.SeedViewCount);
            recipe.FStatus = 1;
            recipe.FCreatedAt = DateTime.UtcNow.AddDays(-definition.PublishedDaysAgo);
            recipe.FUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            await context.TRecipeIngredients
                .Where(item => item.FRecipeId == recipe.FRecipeId)
                .ExecuteDeleteAsync(cancellationToken);
            await context.TRecipeSteps
                .Where(item => item.FRecipeId == recipe.FRecipeId)
                .ExecuteDeleteAsync(cancellationToken);
            await context.TRecipeTagMappings
                .Where(item => item.FRecipeId == recipe.FRecipeId)
                .ExecuteDeleteAsync(cancellationToken);

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

    private async Task EnsureEngagementAsync(
        IReadOnlyDictionary<string, TUser> users,
        CancellationToken cancellationToken)
    {
        var recipeTitles = RecipeSeedDefinitions.Recipes.Select(seed => seed.Title).ToArray();
        var recipes = await context.TRecipes
            .Where(item => recipeTitles.Contains(item.FTitle))
            .ToListAsync(cancellationToken);
        var recipesByTitle = recipes.ToDictionary(recipe => recipe.FTitle);
        var recipeIds = recipes.Select(recipe => recipe.FRecipeId).ToArray();
        var engagementUsers = users.Values
            .Where(user => user.FUsername != RecipeSeedDefinitions.DemoOwnerUsername)
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

    private static TUser CreateUser(
        RecipeUserSeedDefinition definition,
        string encodedPassword)
    {
        return new TUser
        {
            FUsername = definition.Username,
            FPassword = encodedPassword,
            FEmail = definition.Email,
            FPhone = "0900000000",
            FIdNum = definition.IdNumber,
            FAddress = "Taipei",
            FImage = string.Empty,
            FIsActive = definition.IsActive,
            FIsAdmin = false,
            FCreateTime = DateTime.UtcNow
        };
    }
}
