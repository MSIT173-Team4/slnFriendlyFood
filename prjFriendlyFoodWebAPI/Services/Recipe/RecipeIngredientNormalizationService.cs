using System.Globalization;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class RecipeIngredientNormalizationService(
    FriendlyFoodDbContext context,
    IIngredientNameNormalizer ingredientNameNormalizer)
    : IRecipeIngredientNormalizationService
{
    private static readonly IReadOnlyDictionary<string, UnitConversion> UnitConversions =
        new Dictionary<string, UnitConversion>(StringComparer.OrdinalIgnoreCase)
        {
            ["kg"] = new("g", 1000m),
            ["公斤"] = new("g", 1000m),
            ["千克"] = new("g", 1000m),
            ["g"] = new("g", 1m),
            ["公克"] = new("g", 1m),
            ["克"] = new("g", 1m),
            ["l"] = new("ml", 1000m),
            ["公升"] = new("ml", 1000m),
            ["升"] = new("ml", 1000m),
            ["ml"] = new("ml", 1m),
            ["毫升"] = new("ml", 1m),
            ["cc"] = new("ml", 1m),
            ["c.c."] = new("ml", 1m),
            ["tbsp"] = new("ml", 15m),
            ["大匙"] = new("ml", 15m),
            ["湯匙"] = new("ml", 15m),
            ["tsp"] = new("ml", 5m),
            ["小匙"] = new("ml", 5m),
            ["茶匙"] = new("ml", 5m)
        };

    public async Task<ServiceResult<IngredientNormalizationResponseDto>> NormalizeAsync(
        IngredientNormalizationRequestDto request,
        CancellationToken cancellationToken)
    {
        var rawName = request.IngredientName.Trim();
        var rawUnit = request.Unit.Trim();
        var standardName = ingredientNameNormalizer.Normalize(rawName);
        var conversion = UnitConversions.GetValueOrDefault(
            rawUnit,
            new UnitConversion(rawUnit, 1m));
        var standardAmount = decimal.Round(
            request.Amount * conversion.Multiplier,
            2,
            MidpointRounding.AwayFromZero);
        var ingredientId = await context.TIngredients
            .AsNoTracking()
            .Where(ingredient => ingredient.FName == standardName)
            .Select(ingredient => (int?)ingredient.FId)
            .FirstOrDefaultAsync(cancellationToken);
        var conversionApplied = !string.Equals(rawName, standardName, StringComparison.Ordinal) ||
                                !string.Equals(rawUnit, conversion.StandardUnit, StringComparison.OrdinalIgnoreCase) ||
                                standardAmount != request.Amount;
        var displayAmount = $"{standardAmount.ToString("0.##", CultureInfo.InvariantCulture)} {conversion.StandardUnit}";

        return ServiceResult<IngredientNormalizationResponseDto>.Success(
            new IngredientNormalizationResponseDto(
                rawName,
                standardName,
                ingredientId,
                request.Amount,
                rawUnit,
                standardAmount,
                conversion.StandardUnit,
                displayAmount,
                conversionApplied),
            conversionApplied ? "食材名稱與單位已標準化。" : "食材資料已是標準格式。");
    }

    private sealed record UnitConversion(string StandardUnit, decimal Multiplier);
}
