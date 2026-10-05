namespace prjFriendlyFoodWebAPI.Services.Recipe;

public static class RecipeImageUrlResolver
{
    public const string BackendSeedImagePrefix = "/RecipeUploads/Seed/recipes/";
    public const string FallbackImageUrl = BackendSeedImagePrefix + "recipe-cover-fallback.svg";

    private const string LegacyFrontendImagePrefix = "/images/recipes/";

    private static readonly IReadOnlyDictionary<string, string> SeedCoverFileNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["香煎鮭魚佐蘆筍"] = "01-pan-seared-salmon.jpg",
            ["香煎鱸魚佐檸檬奶油醬"] = "02-lemon-butter-sea-bass.jpg",
            ["窯烤風味瑪格麗特披薩"] = "03-margherita-pizza.jpg",
            ["松露野菇燉飯"] = "04-truffle-mushroom-risotto.jpg",
            ["活力巴西莓果碗"] = "05-acai-berry-bowl.jpg",
            ["菠菜豆腐清湯"] = "06-spinach-tofu-soup.jpg",
            ["鮮菇時蔬快炒"] = "07-mushroom-vegetable-stir-fry.jpg",
            ["味噌豆腐蔬食鍋"] = "08-miso-tofu-hot-pot.jpg",
            ["香草雞胸藜麥彩蔬碗"] = "09-herb-chicken-quinoa-bowl.jpg",
            ["黑豆薏仁排骨湯"] = "10-black-bean-barley-pork-rib-soup.jpg",
            ["花椰菜馬鈴薯泥"] = "11-broccoli-potato-puree.jpg",
            ["南瓜雞肉犬用佐餐"] = "12-pumpkin-chicken-dog-meal.jpg",
            ["台式肉絲家常炒麵"] = "13-taiwanese-pork-fried-noodles.jpg",
            ["馬鈴薯豬肉咖哩"] = "14-japanese-pork-curry.jpg",
            ["柴魚涼拌洋蔥絲"] = "15-bonito-onion-salad.jpg",
            ["冬瓜蛤蜊清湯"] = "16-winter-melon-clam-soup.jpg",
            ["古早味醬香滷豆腐"] = "17-braised-tofu.jpg",
            ["寶寶彩蔬軟飯小餐盤"] = "18-baby-vegetable-soft-rice.jpg",
            ["下味冷凍味噌豬肉菇菇燒"] = "19-freezer-miso-pork-mushrooms.jpg",
            ["下味冷凍薑汁燒肉"] = "20-freezer-ginger-pork.jpg",
            ["下味冷凍照燒雞腿"] = "21-freezer-teriyaki-chicken.jpg",
            ["下味冷凍芝麻醬油雞胸"] = "22-freezer-sesame-soy-chicken-breast.jpg",
            ["下味冷凍日式燒肉牛肉"] = "23-freezer-yakiniku-beef.jpg",
            ["下味冷凍甘辛肉燥"] = "24-freezer-sweet-savory-pork-soboro.jpg",
            ["冷凍備料豚汁味噌鍋"] = "25-freezer-tonjiru-miso-pot.jpg",
            ["冷凍烏龍蔬菜炒麵包"] = "26-freezer-yaki-udon.jpg",
            ["冷凍鮭魚味噌燒"] = "27-freezer-miso-salmon.jpg",
            ["冷凍白菜雞肉奶油煮"] = "28-freezer-creamy-chicken-napa-cabbage.jpg",
            ["冷凍番茄鯖魚咖哩"] = "29-freezer-tomato-mackerel-curry.jpg",
            ["冷凍飯糰鮭魚茶泡飯"] = "30-freezer-salmon-onigiri-ochazuke.jpg"
        };

    public static string Resolve(string recipeTitle, string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) || IsLegacyPlaceholderUrl(storedUrl))
        {
            return SeedCoverFileNames.TryGetValue(recipeTitle, out var fileName)
                ? BackendSeedImagePrefix + fileName
                : FallbackImageUrl;
        }

        return Resolve(storedUrl);
    }

    public static string Resolve(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return FallbackImageUrl;
        }

        var normalizedUrl = storedUrl.Trim();
        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var absoluteUrl) &&
            absoluteUrl.AbsolutePath.StartsWith(LegacyFrontendImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BackendSeedImagePrefix + absoluteUrl.AbsolutePath[LegacyFrontendImagePrefix.Length..];
        }

        if (normalizedUrl.StartsWith(LegacyFrontendImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BackendSeedImagePrefix + normalizedUrl[LegacyFrontendImagePrefix.Length..];
        }

        var legacyRelativePrefix = LegacyFrontendImagePrefix.TrimStart('/');
        if (normalizedUrl.StartsWith(legacyRelativePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BackendSeedImagePrefix + normalizedUrl[legacyRelativePrefix.Length..];
        }

        return normalizedUrl;
    }

    private static bool IsLegacyPlaceholderUrl(string storedUrl)
    {
        return Uri.TryCreate(storedUrl.Trim(), UriKind.Absolute, out var absoluteUrl) &&
               (absoluteUrl.Host.Equals("placehold.co", StringComparison.OrdinalIgnoreCase) ||
                absoluteUrl.Host.Equals("www.placehold.co", StringComparison.OrdinalIgnoreCase));
    }
}
