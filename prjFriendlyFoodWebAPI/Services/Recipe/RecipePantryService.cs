using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipePantryService(FriendlyFoodDbContext context) : IRecipePantryService
{
    public async Task<ServiceResult<IReadOnlyCollection<PantryItemDto>>> GetItemsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await context.TUsers.AnyAsync(user => user.FId == userId, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PantryItemDto>>.NotFound("找不到指定的會員。");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = await (
            from pantry in context.TRecipeUserPantries.AsNoTracking()
            join ingredient in context.TIngredients.AsNoTracking()
                on pantry.FIngredientId equals ingredient.FId
            where pantry.FUserId == userId
            orderby pantry.FExpirationDate, ingredient.FName
            select new
            {
                Pantry = pantry,
                IngredientName = ingredient.FName
            }).ToListAsync(cancellationToken);

        var items = rows.Select(row => MapPantryItem(row.Pantry, row.IngredientName, today)).ToArray();

        return ServiceResult<IReadOnlyCollection<PantryItemDto>>.Success(
            items,
            "成功取得冰箱庫存。");
    }

    public async Task<ServiceResult<PantryItemDto>> CreateItemAsync(
        CreatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await context.TUsers.AnyAsync(user => user.FId == request.UserId, cancellationToken))
        {
            return ServiceResult<PantryItemDto>.NotFound("找不到指定的會員。");
        }

        var ingredient = await context.TIngredients
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.FId == request.IngredientId, cancellationToken);

        if (ingredient is null)
        {
            return ServiceResult<PantryItemDto>.NotFound("找不到指定的標準食材。");
        }

        var pantryItem = new TRecipeUserPantry
        {
            FUserId = request.UserId,
            FIngredientId = request.IngredientId,
            FAmount = request.Amount,
            FUnit = request.Unit.Trim(),
            FExpirationDate = request.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            FCreatedAt = DateTime.UtcNow,
            FStorageLocation = request.StorageLocation.Trim(),
            FNote = request.Note?.Trim()
        };

        context.TRecipeUserPantries.Add(pantryItem);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult<PantryItemDto>.Created(
            MapPantryItem(pantryItem, ingredient.FName, DateOnly.FromDateTime(DateTime.Today)),
            "食材已加入冰箱。");
    }

    public async Task<ServiceResult<PantryItemDto>> UpdateItemAsync(
        int pantryId,
        UpdatePantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var pantryItem = await context.TRecipeUserPantries
            .FirstOrDefaultAsync(item => item.FPantryId == pantryId, cancellationToken);

        if (pantryItem is null)
        {
            return ServiceResult<PantryItemDto>.NotFound("找不到要更新的冰箱項目。");
        }

        if (pantryItem.FUserId != request.UserId)
        {
            return ServiceResult<PantryItemDto>.Conflict("不可修改其他會員的冰箱資料。");
        }

        pantryItem.FAmount = request.Amount;
        pantryItem.FUnit = request.Unit.Trim();
        pantryItem.FExpirationDate = request.ExpirationDate ?? pantryItem.FExpirationDate;
        pantryItem.FStorageLocation = request.StorageLocation.Trim();
        pantryItem.FNote = request.Note?.Trim();
        await context.SaveChangesAsync(cancellationToken);

        var ingredientName = await context.TIngredients
            .Where(item => item.FId == pantryItem.FIngredientId)
            .Select(item => item.FName)
            .FirstAsync(cancellationToken);

        return ServiceResult<PantryItemDto>.Success(
            MapPantryItem(pantryItem, ingredientName, DateOnly.FromDateTime(DateTime.Today)),
            "冰箱項目已更新。");
    }

    public async Task<ServiceResult<bool>> DeleteItemAsync(
        int pantryId,
        int userId,
        CancellationToken cancellationToken)
    {
        var pantryItem = await context.TRecipeUserPantries
            .FirstOrDefaultAsync(item => item.FPantryId == pantryId, cancellationToken);

        if (pantryItem is null)
        {
            return ServiceResult<bool>.NotFound("找不到要移除的冰箱項目。");
        }

        if (pantryItem.FUserId != userId)
        {
            return ServiceResult<bool>.Conflict("不可移除其他會員的冰箱資料。");
        }

        context.TRecipeUserPantries.Remove(pantryItem);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true, "冰箱項目已移除。");
    }

    private static PantryItemDto MapPantryItem(
        TRecipeUserPantry pantryItem,
        string ingredientName,
        DateOnly today)
    {
        return new PantryItemDto(
            pantryItem.FPantryId,
            pantryItem.FUserId,
            pantryItem.FIngredientId,
            ingredientName,
            pantryItem.FAmount,
            pantryItem.FUnit,
            pantryItem.FExpirationDate,
            pantryItem.FExpirationDate.DayNumber - today.DayNumber,
            pantryItem.FStorageLocation,
            pantryItem.FNote,
            pantryItem.FCreatedAt);
    }
}
