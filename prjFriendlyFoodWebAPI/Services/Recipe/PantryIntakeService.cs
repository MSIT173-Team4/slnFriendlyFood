using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class PantryIntakeService(
    FriendlyFoodDbContext context,
    IIngredientNameNormalizer ingredientNameNormalizer) : IPantryIntakeService
{
    private static readonly HashSet<string> AllowedStorageLocations =
        ["冷藏", "冷凍", "常溫"];

    public async Task<ServiceResult<PantryItemDto>> AddItemAsync(
        AddPantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PantryItemDto>.Validation("入庫資料不完整。", [.. validationErrors]);
        }

        if (!await context.TUsers.AnyAsync(user => user.FId == request.UserId, cancellationToken))
        {
            return ServiceResult<PantryItemDto>.NotFound("找不到指定的會員。");
        }

        var standardName = ingredientNameNormalizer.Normalize(request.IngredientName);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ingredient = await context.TIngredients.FirstOrDefaultAsync(
                item => item.FName == standardName,
                cancellationToken);

            if (ingredient is null)
            {
                ingredient = new TIngredient { FName = standardName };
                context.TIngredients.Add(ingredient);
                await context.SaveChangesAsync(cancellationToken);
            }

            var pantryItem = new TRecipeUserPantry
            {
                FUserId = request.UserId,
                FIngredientId = ingredient.FId,
                FAmount = request.Amount,
                FUnit = request.Unit.Trim(),
                FExpirationDate = request.ExpirationDate,
                FCreatedAt = DateTime.UtcNow,
                FStorageLocation = request.StorageLocation.Trim(),
                FNote = request.Note?.Trim()
            };

            context.TRecipeUserPantries.Add(pantryItem);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.Today);
            return ServiceResult<PantryItemDto>.Created(
                new PantryItemDto(
                    pantryItem.FPantryId,
                    pantryItem.FUserId,
                    pantryItem.FIngredientId,
                    ingredient.FName,
                    pantryItem.FAmount,
                    pantryItem.FUnit,
                    request.ExpirationDate,
                    request.ExpirationDate.DayNumber - today.DayNumber,
                    pantryItem.FStorageLocation,
                    pantryItem.FNote,
                    pantryItem.FCreatedAt),
                "食材已放入冰箱。");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static List<string> Validate(AddPantryItemRequestDto request)
    {
        var errors = new List<string>();
        if (request.UserId <= 0)
        {
            errors.Add("會員 ID 必須大於零。");
        }

        if (string.IsNullOrWhiteSpace(request.IngredientName) || request.IngredientName.Trim().Length > 50)
        {
            errors.Add("食材名稱為必填，且不可超過 50 個字元。");
        }

        if (request.Amount <= 0)
        {
            errors.Add("數量必須大於零。");
        }

        if (string.IsNullOrWhiteSpace(request.Unit) || request.Unit.Trim().Length > 20)
        {
            errors.Add("單位為必填，且不可超過 20 個字元。");
        }

        if (!AllowedStorageLocations.Contains(request.StorageLocation.Trim()))
        {
            errors.Add("存放溫層只能是冷藏、冷凍或常溫。");
        }

        if (request.Note?.Trim().Length > 150)
        {
            errors.Add("備註不可超過 150 個字元。");
        }

        return errors;
    }
}
