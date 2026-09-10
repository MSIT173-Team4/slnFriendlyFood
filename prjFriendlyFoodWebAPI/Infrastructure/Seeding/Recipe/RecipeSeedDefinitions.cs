namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

internal sealed record RecipeSeedDefinition(
    string Title,
    string Category,
    string Description,
    string CoverImageUrl,
    int CookingMinutes,
    int Servings,
    decimal Calories,
    bool IsAiGenerated,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<IngredientSeedDefinition> Ingredients,
    IReadOnlyCollection<StepSeedDefinition> Steps);

internal sealed record IngredientSeedDefinition(
    string Name,
    string DisplayAmount,
    decimal? BaseAmount,
    string? Unit,
    bool IsMain = true);

internal sealed record StepSeedDefinition(
    string Instruction,
    int TimerSeconds);

internal static class RecipeSeedDefinitions
{
    public static readonly IReadOnlyCollection<(string Name, short Order)> Categories =
    [
        ("家常菜", 1),
        ("異國料理", 2),
        ("烘焙", 3),
        ("健康輕食", 4)
    ];

    public static readonly IReadOnlyCollection<(string Type, string Name)> Tags =
    [
        ("Cuisine", "台式家常"),
        ("Cuisine", "西式料理"),
        ("Tool", "平底鍋"),
        ("Tool", "一鍋到底"),
        ("Difficulty", "新手友善"),
        ("Feature", "十五分鐘"),
        ("Feature", "高蛋白"),
        ("Feature", "零剩食")
    ];

    public static readonly IReadOnlyCollection<RecipeSeedDefinition> Recipes =
    [
        Create(
            "香煎鮭魚佐蘆筍",
            "異國料理",
            "外酥內嫩的鮭魚搭配嫩綠蘆筍與清爽柑橘油醋。",
            25,
            2,
            480,
            true,
            ["西式料理", "平底鍋", "高蛋白"],
            [("大西洋鮭魚排", "350 公克", 350, "g"), ("嫩蘆筍", "150 公克", 150, "g"), ("柳橙", "1 顆", 1, "顆")]),
        Create(
            "香煎鱸魚佐檸檬奶油醬",
            "異國料理",
            "魚皮金黃酥脆，佐以微酸解膩的蒜香檸檬奶油醬。",
            35,
            2,
            420,
            true,
            ["西式料理", "平底鍋", "高蛋白"],
            [("鱸魚片", "300 公克", 300, "g"), ("無鹽奶油", "30 公克", 30, "g"), ("黃檸檬", "1 顆", 1, "顆")]),
        Create(
            "窯烤風味瑪格麗特披薩",
            "烘焙",
            "番茄、羅勒與莫札瑞拉起司交織的經典拿坡里風味。",
            45,
            4,
            620,
            true,
            ["西式料理", "新手友善"],
            [("高筋麵粉", "300 公克", 300, "g"), ("牛番茄", "2 顆", 2, "顆"), ("莫札瑞拉起司", "150 公克", 150, "g")]),
        Create(
            "松露野菇燉飯",
            "異國料理",
            "綜合蕈菇與義大利米慢煮出滑順而濃郁的森林香氣。",
            30,
            2,
            510,
            false,
            ["西式料理", "一鍋到底"],
            [("義大利米", "180 公克", 180, "g"), ("鮮香菇", "200 公克", 200, "g"), ("帕瑪森起司", "30 公克", 30, "g")]),
        Create(
            "活力巴西莓果碗",
            "健康輕食",
            "十分鐘完成的高纖早餐，搭配莓果與堅果穀物。",
            10,
            1,
            290,
            false,
            ["十五分鐘", "新手友善"],
            [("冷凍莓果", "100 公克", 100, "g"), ("香蕉", "1 根", 1, "根"), ("希臘優格", "60 公克", 60, "g")]),
        Create(
            "菠菜豆腐清湯",
            "家常菜",
            "優先消耗即期菠菜與板豆腐的清爽零剩食料理。",
            15,
            2,
            180,
            true,
            ["台式家常", "一鍋到底", "十五分鐘", "零剩食"],
            [("菠菜", "150 公克", 150, "g"), ("板豆腐", "半盒", 200, "g"), ("雞蛋", "1 顆", 1, "顆")]),
        Create(
            "鮮菇時蔬快炒",
            "家常菜",
            "用冰箱常備蔬菜快速完成色彩豐富的家常菜。",
            18,
            2,
            260,
            true,
            ["台式家常", "平底鍋", "零剩食"],
            [("鮮香菇", "200 公克", 200, "g"), ("紅蘿蔔", "1 根", 1, "根"), ("菠菜", "100 公克", 100, "g")]),
        Create(
            "味噌豆腐蔬食鍋",
            "家常菜",
            "一鍋到底的暖胃料理，適合清理零散菇類與蔬菜。",
            25,
            2,
            360,
            false,
            ["一鍋到底", "新手友善", "零剩食"],
            [("板豆腐", "1 盒", 400, "g"), ("鮮香菇", "150 公克", 150, "g"), ("味噌", "2 大匙", 30, "g")])
    ];

    private static RecipeSeedDefinition Create(
        string title,
        string category,
        string description,
        int cookingMinutes,
        int servings,
        decimal calories,
        bool isAiGenerated,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<(string Name, string Display, decimal? Amount, string? Unit)> ingredients)
    {
        var slug = RecipesCountForImage(title);
        return new RecipeSeedDefinition(
            title,
            category,
            description,
            $"https://placehold.co/1200x800/f1ded5/5b382c?text=FriendlyFood+{slug}",
            cookingMinutes,
            servings,
            calories,
            isAiGenerated,
            tags,
            ingredients.Select(item => new IngredientSeedDefinition(
                item.Name,
                item.Display,
                item.Amount,
                item.Unit)).ToArray(),
            [
                new StepSeedDefinition("清洗並備妥所有食材，依照標示份量完成前置處理。", 300),
                new StepSeedDefinition("依料理特性控制火候，將主要食材烹調至適當熟度。", 480),
                new StepSeedDefinition("加入調味、完成盛盤，趁熱享用。", 180)
            ]);
    }

    private static int RecipesCountForImage(string title)
    {
        unchecked
        {
            return Math.Abs(title.Aggregate(17, (hash, character) => hash * 31 + character)) % 1000;
        }
    }
}
