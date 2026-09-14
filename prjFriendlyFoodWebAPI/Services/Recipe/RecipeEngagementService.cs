using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeEngagementService(FriendlyFoodDbContext context) : IRecipeEngagementService
{
    public async Task<ServiceResult<RecipeViewDto>> RecordViewAsync(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var affectedRows = await context.TRecipes
            .Where(recipe => recipe.FRecipeId == recipeId && recipe.FStatus == 1)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(recipe => recipe.FViews, recipe => recipe.FViews + 1)
                    .SetProperty(recipe => recipe.FUpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affectedRows == 0)
        {
            return ServiceResult<RecipeViewDto>.NotFound("找不到公開的食譜資料。");
        }

        var viewCount = await context.TRecipes
            .AsNoTracking()
            .Where(recipe => recipe.FRecipeId == recipeId)
            .Select(recipe => recipe.FViews)
            .SingleAsync(cancellationToken);

        return ServiceResult<RecipeViewDto>.Success(
            new RecipeViewDto(recipeId, viewCount),
            "瀏覽次數已更新。");
    }

    public async Task<ServiceResult<RecipeEngagementDto>> ToggleLikeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(recipeId, userId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var existingLike = await context.TRecipeLikes
            .FirstOrDefaultAsync(
                item => item.FRecipeId == recipeId && item.FUserId == userId,
                cancellationToken);

        if (existingLike is null)
        {
            context.TRecipeLikes.Add(new TRecipeLike { FRecipeId = recipeId, FUserId = userId });
        }
        else
        {
            context.TRecipeLikes.Remove(existingLike);
        }

        await SynchronizeCountersAsync(recipeId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await BuildResultAsync(recipeId, userId, "食譜按讚狀態已更新。", cancellationToken);
    }

    public async Task<ServiceResult<RecipeEngagementDto>> ToggleFavoriteAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(recipeId, userId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var existingFavorite = await context.TRecipeFavorites
            .FirstOrDefaultAsync(
                item => item.FRecipeId == recipeId && item.FUserId == userId,
                cancellationToken);

        if (existingFavorite is null)
        {
            context.TRecipeFavorites.Add(new TRecipeFavorite { FRecipeId = recipeId, FUserId = userId });
        }
        else
        {
            context.TRecipeFavorites.Remove(existingFavorite);
        }

        await SynchronizeCountersAsync(recipeId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await BuildResultAsync(recipeId, userId, "食譜收藏狀態已更新。", cancellationToken);
    }

    private async Task<ServiceResult<RecipeEngagementDto>?> ValidateAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await context.TRecipes.AnyAsync(
                item => item.FRecipeId == recipeId && item.FStatus == 1,
                cancellationToken))
        {
            return ServiceResult<RecipeEngagementDto>.NotFound("找不到指定的公開食譜。");
        }

        if (!await context.TUsers.AnyAsync(item => item.FId == userId, cancellationToken))
        {
            return ServiceResult<RecipeEngagementDto>.NotFound("找不到指定的會員。");
        }

        return null;
    }

    private async Task SynchronizeCountersAsync(
        int recipeId,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);

        var recipe = await context.TRecipes
            .FirstAsync(item => item.FRecipeId == recipeId, cancellationToken);
        recipe.FLikes = await context.TRecipeLikes.CountAsync(
            item => item.FRecipeId == recipeId,
            cancellationToken);
        recipe.FFavorites = await context.TRecipeFavorites.CountAsync(
            item => item.FRecipeId == recipeId,
            cancellationToken);
        recipe.FUpdatedAt = DateTime.UtcNow;
    }

    private async Task<ServiceResult<RecipeEngagementDto>> BuildResultAsync(
        int recipeId,
        int userId,
        string message,
        CancellationToken cancellationToken)
    {
        var recipe = await context.TRecipes
            .AsNoTracking()
            .FirstAsync(item => item.FRecipeId == recipeId, cancellationToken);
        var isLiked = await context.TRecipeLikes.AnyAsync(
            item => item.FRecipeId == recipeId && item.FUserId == userId,
            cancellationToken);
        var isFavorite = await context.TRecipeFavorites.AnyAsync(
            item => item.FRecipeId == recipeId && item.FUserId == userId,
            cancellationToken);

        return ServiceResult<RecipeEngagementDto>.Success(
            new RecipeEngagementDto(
                recipeId,
                userId,
                isLiked,
                isFavorite,
                recipe.FLikes,
                recipe.FFavorites),
            message);
    }
}
