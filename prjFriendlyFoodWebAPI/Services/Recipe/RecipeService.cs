using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeService(
    FriendlyFoodDbContext context,
    ILogger<RecipeService> logger) : IRecipeService
{
    public async Task<ServiceResult<IReadOnlyCollection<RecipeSummaryDto>>> GetRecipesAsync(
        string? search,
        int? categoryId,
        string? tag,
        CancellationToken cancellationToken)
    {
        var query = context.TRecipes
            .AsNoTracking()
            .Where(recipe => recipe.FStatus == 1);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(recipe =>
                recipe.FTitle.Contains(keyword) ||
                (recipe.FDescription != null && recipe.FDescription.Contains(keyword)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(recipe => recipe.FCategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagName = tag.Trim();
            var taggedRecipeIds =
                from mapping in context.TRecipeTagMappings
                join recipeTag in context.TRecipeTags on mapping.FTagId equals recipeTag.FTagId
                where recipeTag.FTagName == tagName
                select mapping.FRecipeId;

            query = query.Where(recipe => taggedRecipeIds.Contains(recipe.FRecipeId));
        }

        var rows = await (
            from recipe in query
            join category in context.TRecipeCategories.AsNoTracking()
                on recipe.FCategoryId equals category.FCategoryId
            join user in context.TUsers.AsNoTracking()
                on recipe.FUserId equals user.FId
            orderby recipe.FCreatedAt descending
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

        var recipes = rows.Select(row => new RecipeSummaryDto(
            row.Recipe.FRecipeId,
            row.Recipe.FTitle,
            row.Recipe.FDescription ?? string.Empty,
            row.Recipe.FCoverImageUrl,
            row.Recipe.FCookingMinutes,
            row.Recipe.FTotalCalories,
            row.Recipe.FDefaultServings,
            row.Recipe.FViews,
            row.Recipe.FLikes,
            row.Recipe.FFavorites,
            row.Recipe.FIsAiGenerated,
            row.CategoryName,
            row.AuthorName,
            tagsByRecipeId.GetValueOrDefault(row.Recipe.FRecipeId, [])))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<RecipeSummaryDto>>.Success(
            recipes,
            "成功取得食譜清單。");
    }

    public async Task<ServiceResult<RecipeDetailDto>> GetRecipeAsync(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var detail = await LoadRecipeDetailAsync(recipeId, cancellationToken);

        return detail is null
            ? ServiceResult<RecipeDetailDto>.NotFound("找不到公開的食譜資料。")
            : ServiceResult<RecipeDetailDto>.Success(detail, "成功取得食譜詳情。");
    }

    public async Task<ServiceResult<RecipeDetailDto>> CreateRecipeAsync(
        CreateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateOwnerAndCategoryAsync(
            request.UserId,
            request.CategoryId,
            cancellationToken);

        if (validation is not null)
        {
            return ServiceResult<RecipeDetailDto>.Validation(validation);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var recipe = new TRecipe
            {
                FUserId = request.UserId,
                FCategoryId = request.CategoryId,
                FTitle = request.Title.Trim(),
                FDescription = request.Description?.Trim(),
                FCoverImageUrl = request.CoverImageUrl?.Trim(),
                FYtVideoId = request.YouTubeVideoId?.Trim(),
                FAiPrepTips = request.AiPrepTips?.Trim(),
                FIsAiGenerated = request.IsAiGenerated,
                FDefaultServings = request.DefaultServings,
                FCookingMinutes = request.CookingMinutes,
                FTotalCalories = request.TotalCalories,
                FStatus = 1,
                FCreatedAt = DateTime.UtcNow,
                FUpdatedAt = DateTime.UtcNow
            };

            context.TRecipes.Add(recipe);
            await context.SaveChangesAsync(cancellationToken);

            await ReplaceRecipeCompositionAsync(
                recipe.FRecipeId,
                request.Ingredients,
                request.Steps,
                request.TagIds,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var detail = await LoadRecipeDetailAsync(recipe.FRecipeId, cancellationToken);
            return ServiceResult<RecipeDetailDto>.Created(detail!, "食譜已建立。");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(exception, "建立食譜時發生資料庫限制衝突。");
            return ServiceResult<RecipeDetailDto>.Conflict("食譜資料與既有資料衝突，請重新確認後再送出。");
        }
    }

    public async Task<ServiceResult<RecipeDetailDto>> UpdateRecipeAsync(
        int recipeId,
        UpdateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var recipe = await context.TRecipes
            .FirstOrDefaultAsync(item => item.FRecipeId == recipeId, cancellationToken);

        if (recipe is null || recipe.FStatus == 3)
        {
            return ServiceResult<RecipeDetailDto>.NotFound("找不到要更新的食譜。");
        }

        if (recipe.FUserId != request.UserId)
        {
            return ServiceResult<RecipeDetailDto>.Conflict("只有食譜建立者可以修改內容。");
        }

        var validation = await ValidateOwnerAndCategoryAsync(
            request.UserId,
            request.CategoryId,
            cancellationToken);

        if (validation is not null)
        {
            return ServiceResult<RecipeDetailDto>.Validation(validation);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            recipe.FCategoryId = request.CategoryId;
            recipe.FTitle = request.Title.Trim();
            recipe.FDescription = request.Description?.Trim();
            recipe.FCoverImageUrl = request.CoverImageUrl?.Trim();
            recipe.FYtVideoId = request.YouTubeVideoId?.Trim();
            recipe.FAiPrepTips = request.AiPrepTips?.Trim();
            recipe.FIsAiGenerated = request.IsAiGenerated;
            recipe.FDefaultServings = request.DefaultServings;
            recipe.FCookingMinutes = request.CookingMinutes;
            recipe.FTotalCalories = request.TotalCalories;
            recipe.FUpdatedAt = DateTime.UtcNow;

            await ReplaceRecipeCompositionAsync(
                recipeId,
                request.Ingredients,
                request.Steps,
                request.TagIds,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var detail = await LoadRecipeDetailAsync(recipeId, cancellationToken);
            return ServiceResult<RecipeDetailDto>.Success(detail!, "食譜已更新。");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(exception, "更新食譜 {RecipeId} 時發生資料庫限制衝突。", recipeId);
            return ServiceResult<RecipeDetailDto>.Conflict("食譜更新失敗，請檢查食材與標籤資料。");
        }
    }

    public async Task<ServiceResult<bool>> DeleteRecipeAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken)
    {
        var recipe = await context.TRecipes
            .FirstOrDefaultAsync(item => item.FRecipeId == recipeId, cancellationToken);

        if (recipe is null || recipe.FStatus == 3)
        {
            return ServiceResult<bool>.NotFound("找不到要下架的食譜。");
        }

        if (recipe.FUserId != userId)
        {
            return ServiceResult<bool>.Conflict("只有食譜建立者可以下架內容。");
        }

        recipe.FStatus = 3;
        recipe.FUpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true, "食譜已下架，資料仍保留供後續稽核。");
    }

    public async Task<ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>> CompleteCookingAsync(
        CompleteCookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var recipe = await context.TRecipes
            .FirstOrDefaultAsync(
                item => item.FRecipeId == request.RecipeId && item.FStatus == 1,
                cancellationToken);

        if (recipe is null)
        {
            return ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>.NotFound("找不到該食譜資料。");
        }

        var ingredients = await (
            from recipeIngredient in context.TRecipeIngredients.AsNoTracking()
            join ingredient in context.TIngredients.AsNoTracking()
                on recipeIngredient.FIngredientId equals ingredient.FId
            where recipeIngredient.FRecipeId == request.RecipeId &&
                  recipeIngredient.FBaseAmount.HasValue
            orderby recipeIngredient.FSortOrder
            select new
            {
                recipeIngredient.FIngredientId,
                ingredient.FName,
                BaseAmount = recipeIngredient.FBaseAmount!.Value,
                recipeIngredient.FStandardUnit
            }).ToListAsync(cancellationToken);

        if (ingredients.Count == 0)
        {
            return ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>.Validation(
                "該食譜未設定可扣減的標準用量。");
        }

        var ingredientIds = ingredients.Select(item => item.FIngredientId).ToArray();
        var pantryLots = await context.TRecipeUserPantries
            .Where(item => item.FUserId == request.UserId && ingredientIds.Contains(item.FIngredientId))
            .OrderBy(item => item.FExpirationDate)
            .ToListAsync(cancellationToken);

        var scaleRatio = (decimal)request.TargetServings /
                         Math.Max(recipe.FDefaultServings, 1);
        var deductions = new List<CookingDeductionResultDto>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var ingredient in ingredients)
            {
                var requiredAmount = ingredient.BaseAmount * scaleRatio;
                var amountToConsume = requiredAmount;
                var matchingLots = pantryLots
                    .Where(item => item.FIngredientId == ingredient.FIngredientId)
                    .ToList();

                foreach (var lot in matchingLots)
                {
                    if (amountToConsume <= 0)
                    {
                        break;
                    }

                    var deductedAmount = Math.Min(lot.FAmount, amountToConsume);
                    lot.FAmount -= deductedAmount;
                    amountToConsume -= deductedAmount;

                    if (lot.FAmount <= 0)
                    {
                        context.TRecipeUserPantries.Remove(lot);
                    }
                }

                var remainingAmount = matchingLots.Sum(item => Math.Max(item.FAmount, 0));
                var consumedAmount = requiredAmount - amountToConsume;

                deductions.Add(new CookingDeductionResultDto(
                    ingredient.FIngredientId,
                    ingredient.FName,
                    requiredAmount,
                    consumedAmount,
                    remainingAmount,
                    ingredient.FStandardUnit ?? matchingLots.FirstOrDefault()?.FUnit ?? string.Empty,
                    remainingAmount == 0,
                    amountToConsume > 0));
            }

            recipe.FViews++;
            recipe.FUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>.Success(
                deductions,
                "料理完成，冰箱庫存已依到期日順序扣減。");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(exception, "料理完成結算失敗，交易已復原。");
            return ServiceResult<IReadOnlyCollection<CookingDeductionResultDto>>.Unexpected(
                "庫存扣減失敗，交易已完整復原。");
        }
    }

    public async Task<ServiceResult<RecipeMetadataDto>> GetMetadataAsync(
        CancellationToken cancellationToken)
    {
        var categories = await context.TRecipeCategories
            .AsNoTracking()
            .OrderBy(item => item.FDisplayOrder)
            .Select(item => new RecipeCategoryDto(
                item.FCategoryId,
                item.FCategoryName,
                item.FDisplayOrder))
            .ToListAsync(cancellationToken);

        var tags = await context.TRecipeTags
            .AsNoTracking()
            .OrderBy(item => item.FTagType)
            .ThenBy(item => item.FTagName)
            .Select(item => new RecipeTagDto(item.FTagId, item.FTagType, item.FTagName))
            .ToListAsync(cancellationToken);

        return ServiceResult<RecipeMetadataDto>.Success(
            new RecipeMetadataDto(categories, tags),
            "成功取得食譜分類與標籤。");
    }

    private async Task<string?> ValidateOwnerAndCategoryAsync(
        int userId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        if (!await context.TUsers.AnyAsync(user => user.FId == userId, cancellationToken))
        {
            return "找不到食譜建立者。";
        }

        if (!await context.TRecipeCategories.AnyAsync(
                category => category.FCategoryId == categoryId,
                cancellationToken))
        {
            return "找不到指定的食譜分類。";
        }

        return null;
    }

    private async Task ReplaceRecipeCompositionAsync(
        int recipeId,
        IReadOnlyCollection<RecipeIngredientInputDto> ingredientInputs,
        IReadOnlyCollection<RecipeStepInputDto> stepInputs,
        IReadOnlyCollection<int> tagIds,
        CancellationToken cancellationToken)
    {
        var oldIngredients = await context.TRecipeIngredients
            .Where(item => item.FRecipeId == recipeId)
            .ToListAsync(cancellationToken);
        var oldSteps = await context.TRecipeSteps
            .Where(item => item.FRecipeId == recipeId)
            .ToListAsync(cancellationToken);
        var oldMappings = await context.TRecipeTagMappings
            .Where(item => item.FRecipeId == recipeId)
            .ToListAsync(cancellationToken);

        context.TRecipeIngredients.RemoveRange(oldIngredients);
        context.TRecipeSteps.RemoveRange(oldSteps);
        context.TRecipeTagMappings.RemoveRange(oldMappings);

        var normalizedNames = ingredientInputs
            .Select(item => item.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var knownIngredients = await context.TIngredients
            .Where(item => normalizedNames.Contains(item.FName))
            .ToDictionaryAsync(item => item.FName, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var resolvedIngredients = new List<(RecipeIngredientInputDto Input, TIngredient Ingredient)>();

        foreach (var input in ingredientInputs.OrderBy(item => item.SortOrder))
        {
            TIngredient? ingredient = null;

            if (input.IngredientId.HasValue)
            {
                ingredient = await context.TIngredients.FindAsync([input.IngredientId.Value], cancellationToken);
            }

            if (ingredient is null && !knownIngredients.TryGetValue(input.Name.Trim(), out ingredient))
            {
                ingredient = new TIngredient { FName = input.Name.Trim() };
                context.TIngredients.Add(ingredient);
                knownIngredients[ingredient.FName] = ingredient;
            }

            resolvedIngredients.Add((input, ingredient));
        }

        // 新增的標準食材需要先取得資料庫流水號，才能建立配方明細。
        await context.SaveChangesAsync(cancellationToken);

        context.TRecipeIngredients.AddRange(resolvedIngredients.Select(item => new TRecipeIngredient
        {
            FRecipeId = recipeId,
            FIngredientId = item.Ingredient.FId,
            FDisplayAmount = item.Input.DisplayAmount.Trim(),
            FBaseAmount = item.Input.BaseAmount,
            FStandardUnit = item.Input.StandardUnit?.Trim(),
            FIsMain = item.Input.IsMain,
            FSortOrder = item.Input.SortOrder
        }));

        context.TRecipeSteps.AddRange(stepInputs.Select(input => new TRecipeStep
        {
            FRecipeId = recipeId,
            FStepNumber = input.StepNumber,
            FInstruction = input.Instruction.Trim(),
            FImageUrl = input.ImageUrl?.Trim(),
            FTimerSeconds = input.TimerSeconds
        }));

        var validTagIds = await context.TRecipeTags
            .Where(item => tagIds.Contains(item.FTagId))
            .Select(item => item.FTagId)
            .ToListAsync(cancellationToken);

        context.TRecipeTagMappings.AddRange(validTagIds.Distinct().Select(tagId => new TRecipeTagMapping
        {
            FRecipeId = recipeId,
            FTagId = tagId
        }));

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<RecipeDetailDto?> LoadRecipeDetailAsync(
        int recipeId,
        CancellationToken cancellationToken)
    {
        var header = await (
            from recipeEntity in context.TRecipes.AsNoTracking()
            join category in context.TRecipeCategories.AsNoTracking()
                on recipeEntity.FCategoryId equals category.FCategoryId
            join user in context.TUsers.AsNoTracking()
                on recipeEntity.FUserId equals user.FId
            where recipeEntity.FRecipeId == recipeId && recipeEntity.FStatus == 1
            select new
            {
                Recipe = recipeEntity,
                CategoryName = category.FCategoryName,
                AuthorName = user.FUsername
            }).FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var ingredients = await (
            from recipeIngredient in context.TRecipeIngredients.AsNoTracking()
            join ingredient in context.TIngredients.AsNoTracking()
                on recipeIngredient.FIngredientId equals ingredient.FId
            where recipeIngredient.FRecipeId == recipeId
            orderby recipeIngredient.FSortOrder
            select new RecipeIngredientDto(
                ingredient.FId,
                ingredient.FName,
                recipeIngredient.FDisplayAmount,
                recipeIngredient.FBaseAmount,
                recipeIngredient.FStandardUnit,
                recipeIngredient.FIsMain,
                recipeIngredient.FSortOrder))
            .ToListAsync(cancellationToken);

        var steps = await context.TRecipeSteps
            .AsNoTracking()
            .Where(item => item.FRecipeId == recipeId)
            .OrderBy(item => item.FStepNumber)
            .Select(item => new RecipeStepDto(
                item.FStepNumber,
                item.FInstruction,
                item.FImageUrl,
                item.FTimerSeconds))
            .ToListAsync(cancellationToken);

        var tags = await (
            from mapping in context.TRecipeTagMappings.AsNoTracking()
            join recipeTag in context.TRecipeTags.AsNoTracking()
                on mapping.FTagId equals recipeTag.FTagId
            where mapping.FRecipeId == recipeId
            orderby recipeTag.FTagType, recipeTag.FTagName
            select recipeTag.FTagName)
            .ToListAsync(cancellationToken);

        var recipe = header.Recipe;
        return new RecipeDetailDto(
            recipe.FRecipeId,
            recipe.FUserId,
            recipe.FCategoryId,
            recipe.FTitle,
            recipe.FDescription ?? string.Empty,
            recipe.FCoverImageUrl,
            recipe.FYtVideoId,
            recipe.FAiPrepTips,
            recipe.FIsAiGenerated,
            recipe.FDefaultServings,
            recipe.FCookingMinutes,
            recipe.FTotalCalories,
            recipe.FViews,
            recipe.FLikes,
            recipe.FFavorites,
            header.CategoryName,
            header.AuthorName,
            tags,
            ingredients,
            steps);
    }
}
