namespace prjFriendlyFoodWebAPI.Services.Recipe;

public sealed class IngredientNameNormalizer : IIngredientNameNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> TaiwanAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["西紅柿"] = "牛番茄",
            ["番茄"] = "牛番茄",
            ["土豆"] = "馬鈴薯",
            ["花菜"] = "白花椰菜",
            ["西蘭花"] = "青花椰菜",
            ["捲心菜"] = "高麗菜",
            ["菠蘿"] = "鳳梨",
            ["菠萝"] = "鳳梨",
            ["鳳梨果"] = "鳳梨"
        };

    public string Normalize(string ingredientName)
    {
        var normalized = ingredientName.Trim();
        return TaiwanAliases.TryGetValue(normalized, out var standardName)
            ? standardName
            : normalized;
    }
}
