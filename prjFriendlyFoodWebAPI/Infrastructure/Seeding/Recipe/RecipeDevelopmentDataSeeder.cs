using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

public sealed class RecipeDevelopmentDataSeeder(
    FriendlyFoodDbContext context,
    ILogger<RecipeDevelopmentDataSeeder> logger) : IRecipeDataSeeder
{
    private const string DemoOwnerUsername = "recipe.demo";
    private const string DemoTesterUsername = "recipe.tester";

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
                users.Owner.FId,
                categories,
                ingredients,
                tags,
                cancellationToken);
            await EnsurePantryAsync(users, ingredients, cancellationToken);
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

    private async Task<(TUser Owner, TUser Tester)> EnsureUsersAsync(
        CancellationToken cancellationToken)
    {
        var owner = await context.TUsers
            .FirstOrDefaultAsync(user => user.FUsername == DemoOwnerUsername, cancellationToken);
        var tester = await context.TUsers
            .FirstOrDefaultAsync(user => user.FUsername == DemoTesterUsername, cancellationToken);

        owner ??= CreateUser(DemoOwnerUsername, "recipe.demo@friendlyfood.local", "A123456789");
        tester ??= CreateUser(DemoTesterUsername, "recipe.tester@friendlyfood.local", "B123456789");

        if (owner.FId == 0)
        {
            context.TUsers.Add(owner);
        }

        if (tester.FId == 0)
        {
            context.TUsers.Add(tester);
        }

        await context.SaveChangesAsync(cancellationToken);
        return (owner, tester);
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
        int ownerId,
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

            recipe.FUserId = ownerId;
            recipe.FCategoryId = categories[definition.Category].FCategoryId;
            recipe.FDescription = definition.Description;
            recipe.FCoverImageUrl = definition.CoverImageUrl;
            recipe.FAiPrepTips = $"資料來源：{definition.SourceName}｜{definition.SourceUrl}。{definition.SafetyNote}";
            recipe.FIsAiGenerated = definition.IsAiGenerated;
            recipe.FDefaultServings = definition.Servings;
            recipe.FCookingMinutes = definition.CookingMinutes;
            recipe.FTotalCalories = definition.Calories;
            recipe.FViews = Math.Max(recipe.FViews, 120);
            recipe.FStatus = 1;
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
        (TUser Owner, TUser Tester) users,
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
                item => item.FUserId == users.Tester.FId && item.FIngredientId == ingredientId,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            context.TRecipeUserPantries.Add(new TRecipeUserPantry
            {
                FUserId = users.Tester.FId,
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
        (TUser Owner, TUser Tester) users,
        CancellationToken cancellationToken)
    {
        var recipes = await context.TRecipes
            .Where(item => RecipeSeedDefinitions.Recipes.Select(seed => seed.Title).Contains(item.FTitle))
            .OrderBy(item => item.FRecipeId)
            .ToListAsync(cancellationToken);

        foreach (var recipe in recipes.Take(5))
        {
            if (!await context.TRecipeLikes.AnyAsync(
                    item => item.FRecipeId == recipe.FRecipeId && item.FUserId == users.Tester.FId,
                    cancellationToken))
            {
                context.TRecipeLikes.Add(new TRecipeLike
                {
                    FRecipeId = recipe.FRecipeId,
                    FUserId = users.Tester.FId
                });
            }
        }

        foreach (var recipe in recipes.Take(3))
        {
            if (!await context.TRecipeFavorites.AnyAsync(
                    item => item.FRecipeId == recipe.FRecipeId && item.FUserId == users.Tester.FId,
                    cancellationToken))
            {
                context.TRecipeFavorites.Add(new TRecipeFavorite
                {
                    FRecipeId = recipe.FRecipeId,
                    FUserId = users.Tester.FId
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var recipe in recipes)
        {
            recipe.FLikes = await context.TRecipeLikes.CountAsync(
                item => item.FRecipeId == recipe.FRecipeId,
                cancellationToken);
            recipe.FFavorites = await context.TRecipeFavorites.CountAsync(
                item => item.FRecipeId == recipe.FRecipeId,
                cancellationToken);
        }
    }

    private static TUser CreateUser(
        string username,
        string email,
        string idNumber)
    {
        return new TUser
        {
            FUsername = username,
            FPassword = "DemoOnly2026!",
            FEmail = email,
            FPhone = "0900000000",
            FIdNum = idNumber,
            FAddress = "Taipei",
            FImage = string.Empty,
            FIsActive = true,
            FIsAdmin = false,
            FCreateTime = DateTime.UtcNow
        };
    }
}
