using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeRecommendationService(FriendlyFoodDbContext context)
    : IRecipeRecommendationService
{
    private const int ExpiringThresholdDays = 3;
    private const decimal CoverageWeight = 0.9m;
    private const decimal ExpiringIngredientWeight = 0.1m;

    public async Task<ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>> GetZeroWasteRecommendationsAsync(
        int userId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>.Validation(
                "會員 ID 必須大於零。");
        }

        if (limit is < 1 or > 50)
        {
            return ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>.Validation(
                "推薦筆數必須介於 1 到 50 筆之間。");
        }

        if (!await context.TUsers.AsNoTracking().AnyAsync(user => user.FId == userId, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>.NotFound(
                "找不到指定的會員。");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var pantryRows = await (
            from pantry in context.TRecipeUserPantries.AsNoTracking()
            join ingredient in context.TIngredients.AsNoTracking()
                on pantry.FIngredientId equals ingredient.FId
            where pantry.FUserId == userId && pantry.FExpirationDate >= today
            select new
            {
                pantry.FIngredientId,
                ingredient.FName,
                pantry.FExpirationDate
            }).ToListAsync(cancellationToken);

        var pantryByIngredientId = pantryRows
            .GroupBy(row => row.FIngredientId)
            .ToDictionary(
                group => group.Key,
                group => new PantryIngredientAvailability(
                    group.Key,
                    group.First().FName,
                    group.Min(row => row.FExpirationDate).DayNumber - today.DayNumber));

        if (pantryByIngredientId.Count == 0)
        {
            return ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>.Success(
                [],
                "冰箱目前沒有未過期食材，請先新增食材後再取得推薦。");
        }

        var recipeRows = await (
            from recipe in context.TRecipes.AsNoTracking()
            join category in context.TRecipeCategories.AsNoTracking()
                on recipe.FCategoryId equals category.FCategoryId
            where recipe.FStatus == 1
            select new
            {
                recipe.FRecipeId,
                recipe.FTitle,
                recipe.FCoverImageUrl,
                recipe.FCookingMinutes,
                recipe.FViews,
                recipe.FLikes,
                CategoryName = category.FCategoryName
            }).ToListAsync(cancellationToken);

        var recipeIds = recipeRows.Select(row => row.FRecipeId).ToArray();
        var ingredientRows = await (
            from recipeIngredient in context.TRecipeIngredients.AsNoTracking()
            join ingredient in context.TIngredients.AsNoTracking()
                on recipeIngredient.FIngredientId equals ingredient.FId
            where recipeIds.Contains(recipeIngredient.FRecipeId)
            orderby recipeIngredient.FSortOrder
            select new
            {
                recipeIngredient.FRecipeId,
                recipeIngredient.FIngredientId,
                ingredient.FName,
                recipeIngredient.FSortOrder
            }).ToListAsync(cancellationToken);

        var ingredientsByRecipeId = ingredientRows
            .GroupBy(row => row.FRecipeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(row => row.FIngredientId)
                    .Select(ingredientGroup => ingredientGroup.First())
                    .OrderBy(row => row.FSortOrder)
                    .ToArray());

        var recommendations = recipeRows
            .Select(recipe =>
            {
                var requiredIngredients = ingredientsByRecipeId.GetValueOrDefault(recipe.FRecipeId, []);
                if (requiredIngredients.Length == 0)
                {
                    return null;
                }

                var availableIngredients = requiredIngredients
                    .Where(required => pantryByIngredientId.ContainsKey(required.FIngredientId))
                    .Select(required => pantryByIngredientId[required.FIngredientId])
                    .ToArray();
                var missingIngredients = requiredIngredients
                    .Where(required => !pantryByIngredientId.ContainsKey(required.FIngredientId))
                    .Select(required => new RecipeMatchIngredientDto(
                        required.FIngredientId,
                        required.FName,
                        null))
                    .ToArray();
                var expiringIngredients = availableIngredients
                    .Where(ingredient => ingredient.DaysUntilExpiration <= ExpiringThresholdDays)
                    .ToArray();

                var coverageRatio = (decimal)availableIngredients.Length / requiredIngredients.Length;
                var expiringRatio = (decimal)expiringIngredients.Length / requiredIngredients.Length;
                var matchPercentage = Math.Round(
                    Math.Min(
                        100m,
                        ((coverageRatio * CoverageWeight) +
                         (expiringRatio * ExpiringIngredientWeight)) * 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                var availableDtos = availableIngredients
                    .Select(ingredient => ingredient.ToDto())
                    .ToArray();
                var expiringDtos = expiringIngredients
                    .Select(ingredient => ingredient.ToDto())
                    .ToArray();
                var explanation = BuildExplanation(
                    availableDtos,
                    expiringDtos,
                    missingIngredients);

                return new RankedRecommendation(
                    new RecipeRecommendationDto(
                        recipe.FRecipeId,
                        recipe.FTitle,
                        recipe.FCoverImageUrl,
                        recipe.CategoryName,
                        recipe.FCookingMinutes,
                        matchPercentage,
                        availableDtos.Length,
                        requiredIngredients.Length,
                        missingIngredients.Length == 0,
                        availableDtos,
                        expiringDtos,
                        missingIngredients,
                        explanation),
                    expiringDtos.Length,
                    recipe.FLikes,
                    recipe.FViews);
            })
            .Where(result => result is not null && result.Recommendation.MatchedIngredientCount > 0)
            .Select(result => result!)
            .OrderByDescending(result => result.Recommendation.MatchPercentage)
            .ThenByDescending(result => result.ExpiringIngredientCount)
            .ThenByDescending(result => result.Likes)
            .ThenByDescending(result => result.Views)
            .Take(limit)
            .Select(result => result.Recommendation)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<RecipeRecommendationDto>>.Success(
            recommendations,
            recommendations.Length == 0
                ? "目前沒有可使用冰箱食材的食譜。"
                : "成功取得零浪費食譜推薦。");
    }

    public async Task<ServiceResult<IReadOnlyCollection<TrendingRecipeDto>>> GetTrendingRecipesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 50)
        {
            return ServiceResult<IReadOnlyCollection<TrendingRecipeDto>>.Validation(
                "熱門食譜筆數必須介於 1 到 50 筆之間。");
        }

        var rows = await (
            from recipe in context.TRecipes.AsNoTracking()
            join category in context.TRecipeCategories.AsNoTracking()
                on recipe.FCategoryId equals category.FCategoryId
            join user in context.TUsers.AsNoTracking()
                on recipe.FUserId equals user.FId
            where recipe.FStatus == 1
            select new
            {
                Recipe = recipe,
                CategoryName = category.FCategoryName,
                AuthorName = user.FUsername
            }).ToListAsync(cancellationToken);

        var recipeIds = rows.Select(row => row.Recipe.FRecipeId).ToArray();
        var tagRows = await (
            from mapping in context.TRecipeTagMappings.AsNoTracking()
            join recipeTag in context.TRecipeTags.AsNoTracking()
                on mapping.FTagId equals recipeTag.FTagId
            where recipeIds.Contains(mapping.FRecipeId)
            select new { mapping.FRecipeId, recipeTag.FTagName })
            .ToListAsync(cancellationToken);
        var tagsByRecipeId = tagRows
            .GroupBy(row => row.FRecipeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(row => row.FTagName).ToArray());

        var now = DateTime.UtcNow;
        var trendingRecipes = rows
            .Select(row =>
            {
                var recipe = row.Recipe;
                var ageInHours = Math.Max((now - recipe.FCreatedAt).TotalHours, 1d);
                var engagementScore = recipe.FViews +
                                      (recipe.FLikes * 3d) +
                                      (recipe.FFavorites * 4d) +
                                      1d;
                var timeDecayScore = engagementScore / Math.Pow(ageInHours + 2d, 0.7d);
                var summary = new RecipeSummaryDto(
                    recipe.FRecipeId,
                    recipe.FTitle,
                    recipe.FDescription ?? string.Empty,
                    recipe.FCoverImageUrl,
                    recipe.FCookingMinutes,
                    recipe.FTotalCalories,
                    recipe.FDefaultServings,
                    recipe.FViews,
                    recipe.FLikes,
                    recipe.FFavorites,
                    recipe.FIsAiGenerated,
                    row.CategoryName,
                    row.AuthorName,
                    tagsByRecipeId.GetValueOrDefault(recipe.FRecipeId, []));

                return new TrendingRecipeDto(summary, Math.Round((decimal)timeDecayScore, 4));
            })
            .OrderByDescending(result => result.TimeDecayScore)
            .Take(limit)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<TrendingRecipeDto>>.Success(
            trendingRecipes,
            "成功取得近期熱門食譜。");
    }

    private static string BuildExplanation(
        IReadOnlyCollection<RecipeMatchIngredientDto> availableIngredients,
        IReadOnlyCollection<RecipeMatchIngredientDto> expiringIngredients,
        IReadOnlyCollection<RecipeMatchIngredientDto> missingIngredients)
    {
        var availableText = availableIngredients.Count == 0
            ? "沒有可直接使用的食材"
            : $"可使用：{string.Join("、", availableIngredients.Select(item => item.Name))}";
        var missingText = missingIngredients.Count == 0
            ? "食材已備齊"
            : $"尚缺：{string.Join("、", missingIngredients.Select(item => item.Name))}";
        var expiringText = expiringIngredients.Count == 0
            ? null
            : $"優先消耗即期食材：{string.Join("、", expiringIngredients.Select(item => item.Name))}";

        return string.Join("；", new[] { availableText, missingText, expiringText }
            .Where(text => text is not null));
    }

    private sealed record PantryIngredientAvailability(
        int IngredientId,
        string Name,
        int DaysUntilExpiration)
    {
        public RecipeMatchIngredientDto ToDto() =>
            new(IngredientId, Name, DaysUntilExpiration);
    }

    private sealed record RankedRecommendation(
        RecipeRecommendationDto Recommendation,
        int ExpiringIngredientCount,
        int Likes,
        int Views);
}
