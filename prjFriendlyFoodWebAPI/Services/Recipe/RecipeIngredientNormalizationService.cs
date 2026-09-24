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
            ["kg"] = new("公克", 1000m),
            ["公斤"] = new("公克", 1000m),
            ["千克"] = new("公克", 1000m),
            ["g"] = new("公克", 1m),
            ["公克"] = new("公克", 1m),
            ["克"] = new("公克", 1m),
            ["l"] = new("毫升", 1000m),
            ["公升"] = new("毫升", 1000m),
            ["升"] = new("毫升", 1000m),
            ["ml"] = new("毫升", 1m),
            ["毫升"] = new("毫升", 1m),
            ["cc"] = new("毫升", 1m),
            ["c.c."] = new("毫升", 1m),
            ["tbsp"] = new("毫升", 15m),
            ["大匙"] = new("毫升", 15m),
            ["湯匙"] = new("毫升", 15m),
            ["tsp"] = new("毫升", 5m),
            ["小匙"] = new("毫升", 5m),
            ["茶匙"] = new("毫升", 5m)
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
