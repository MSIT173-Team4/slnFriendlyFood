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
    string SourceName,
    string SourceUrl,
    string SafetyNote,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<IngredientSeedDefinition> Ingredients,
    IReadOnlyCollection<StepSeedDefinition> Steps);

internal sealed record IngredientSeedDefinition(
    string Name,
    string DisplayAmount,
    decimal? BaseAmount,
    string? Unit,
    bool IsMain = true);

internal sealed record StepSeedDefinition(string Instruction, int TimerSeconds);

internal static class RecipeSeedDefinitions
{
    private const string NutritionGovUrl = "https://www.nutrition.gov/recipes";

    public static readonly IReadOnlyCollection<(string Name, short Order)> Categories =
    [
        ("家常菜", 1),
        ("異國料理", 2),
        ("烘焙", 3),
        ("健康輕食", 4),
        ("特殊照護", 5),
        ("寵物料理", 6)
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
        ("Feature", "零剩食"),
        ("Feature", "無加鹽"),
        ("Audience", "健身餐"),
        ("Audience", "月子餐"),
        ("Audience", "寶寶副食品"),
        ("Audience", "寵物鮮食"),
        ("Audience", "犬用佐餐")
    ];

    public static readonly IReadOnlyCollection<RecipeSeedDefinition> Recipes =
    [
        Create(
            "香煎鮭魚佐蘆筍", "異國料理",
            "外酥內嫩的鮭魚搭配嫩綠蘆筍與清爽柑橘油醋。", 25, 2, 480, true,
            "USDA Nutrition.gov", NutritionGovUrl, "魚肉中心須完全加熱。",
            ["西式料理", "平底鍋", "高蛋白"],
            [("大西洋鮭魚排", "350 公克", 350, "g"), ("嫩蘆筍", "150 公克", 150, "g"), ("柳橙", "1 顆", 1, "顆")],
            [("鮭魚擦乾後調味，蘆筍切除粗硬末端。", 300), ("魚皮朝下以中火煎至金黃，再翻面煎熟。", 480), ("蘆筍入鍋煎熟，淋上柳橙汁後盛盤。", 180)]),
        Create(
            "香煎鱸魚佐檸檬奶油醬", "異國料理",
            "魚皮金黃酥脆，佐以微酸解膩的蒜香檸檬奶油醬。", 35, 2, 420, true,
            "Mayo Clinic Healthy Recipes", "https://www.mayoclinic.org/healthy-lifestyle/recipes/dash-diet-recipes/rcs-20077146", "魚肉中心須完全加熱。",
            ["西式料理", "平底鍋", "高蛋白"],
            [("鱸魚片", "300 公克", 300, "g"), ("無鹽奶油", "30 公克", 30, "g"), ("黃檸檬", "1 顆", 1, "顆")],
            [("鱸魚擦乾並在魚皮劃淺刀，檸檬榨汁。", 300), ("魚皮朝下壓平香煎，翻面後煎至熟透。", 480), ("原鍋融化奶油並拌入檸檬汁，淋回魚片。", 180)]),
        Create(
            "窯烤風味瑪格麗特披薩", "烘焙",
            "番茄、羅勒與莫札瑞拉起司交織的經典風味。", 45, 4, 620, true,
            "USDA Nutrition.gov", NutritionGovUrl, "烤箱與烤盤溫度高，取放時使用隔熱手套。",
            ["西式料理", "新手友善"],
            [("高筋麵粉", "300 公克", 300, "g"), ("牛番茄", "2 顆", 2, "顆"), ("莫札瑞拉起司", "150 公克", 150, "g")],
            [("麵粉加水與酵母揉成光滑麵團，靜置發酵。", 1200), ("麵團擀平，鋪上番茄片與起司。", 300), ("以高溫烘烤至餅皮上色、起司融化。", 720)]),
        Create(
            "松露野菇燉飯", "異國料理",
            "綜合蕈菇與義大利米慢煮出滑順而濃郁的森林香氣。", 30, 2, 510, false,
            "Mayo Clinic Healthy Recipes", "https://www.mayoclinic.org/healthy-lifestyle/recipes/dash-diet-recipes/rcs-20077146", "野菇應充分加熱後食用。",
            ["西式料理", "一鍋到底"],
            [("義大利米", "180 公克", 180, "g"), ("鮮香菇", "200 公克", 200, "g"), ("帕瑪森起司", "30 公克", 30, "g")],
            [("香菇切片後炒至水分收乾。", 360), ("加入義大利米拌炒，分次加入熱高湯。", 900), ("米心熟而帶彈性時拌入起司後離火。", 180)]),
        Create(
            "活力巴西莓果碗", "健康輕食",
            "十分鐘完成的高纖早餐，搭配莓果與堅果穀物。", 10, 1, 290, false,
            "USDA Nutrition.gov", NutritionGovUrl, "幼兒食用堅果時須依年齡調整型態並全程看護。",
            ["十五分鐘", "新手友善"],
            [("冷凍莓果", "100 公克", 100, "g"), ("香蕉", "1 根", 1, "根"), ("希臘優格", "60 公克", 60, "g")],
            [("冷凍莓果稍微退冰，香蕉切段。", 120), ("莓果、香蕉與優格攪打至濃稠。", 120), ("倒入碗中並依喜好加入穀物配料。", 60)]),
        Create(
            "菠菜豆腐清湯", "家常菜",
            "優先消耗即期菠菜與板豆腐的清爽零剩食料理。", 15, 2, 180, true,
            "衛生福利部國民健康署", "https://health99.hpa.gov.tw/storage/files/materials/22208-1.pdf", "雞蛋須煮至蛋白與蛋黃凝固。",
            ["台式家常", "一鍋到底", "十五分鐘", "零剩食"],
            [("菠菜", "150 公克", 150, "g"), ("板豆腐", "半盒", 200, "g"), ("雞蛋", "1 顆", 1, "顆")],
            [("菠菜洗淨切段，豆腐切成小塊。", 240), ("水滾後加入豆腐，小火煮出豆香。", 360), ("放入菠菜並淋入蛋液，蛋熟後關火。", 180)]),
        Create(
            "鮮菇時蔬快炒", "家常菜",
            "用冰箱常備蔬菜快速完成色彩豐富的家常菜。", 18, 2, 260, true,
            "衛生福利部國民健康署", "https://health99.hpa.gov.tw/storage/files/materials/22208-1.pdf", "蔬菜清洗後應瀝乾，避免熱油噴濺。",
            ["台式家常", "平底鍋", "零剩食"],
            [("鮮香菇", "200 公克", 200, "g"), ("紅蘿蔔", "1 根", 1, "根"), ("菠菜", "100 公克", 100, "g")],
            [("香菇切片、紅蘿蔔切絲、菠菜切段。", 300), ("先炒香菇與紅蘿蔔至軟化。", 360), ("加入菠菜大火快炒，葉片轉綠即起鍋。", 120)]),
        Create(
            "味噌豆腐蔬食鍋", "家常菜",
            "一鍋到底的暖胃料理，適合清理零散菇類與蔬菜。", 25, 2, 360, false,
            "USDA Nutrition.gov", NutritionGovUrl, "味噌鈉含量較高，可依需求減量。",
            ["一鍋到底", "新手友善", "零剩食"],
            [("板豆腐", "1 盒", 400, "g"), ("鮮香菇", "150 公克", 150, "g"), ("味噌", "2 大匙", 30, "g")],
            [("豆腐切塊，香菇切片並備妥其他剩餘蔬菜。", 300), ("香菇與耐煮蔬菜入鍋煮熟。", 600), ("轉小火溶入味噌，加入豆腐溫熱後關火。", 240)]),
        Create(
            "香草雞胸藜麥彩蔬碗", "健康輕食",
            "雞胸、藜麥與彩蔬組成方便備餐的高蛋白餐盒。", 30, 2, 460, false,
            "USDA MyPlate", "https://www.myplate.gov/web/web/eat-healthy/protein-foods", "雞肉中心須達安全熟度，不可僅依表面顏色判斷。",
            ["健身餐", "高蛋白", "平底鍋"],
            [("雞胸肉", "300 公克", 300, "g"), ("藜麥", "120 公克", 120, "g"), ("青花椰菜", "180 公克", 180, "g")],
            [("藜麥洗淨後加水煮熟並燜五分鐘。", 900), ("雞胸肉拍平、撒香草，以中火煎至中心熟透。", 600), ("青花椰菜蒸熟，與藜麥及雞胸分區裝盤。", 360)]),
        Create(
            "黑豆薏仁排骨湯", "特殊照護",
            "以黑豆、薏仁與排骨慢煮的產後餐點靈感。", 70, 4, 390, false,
            "臺大醫院新竹分院", "https://www.hch.gov.tw/?aid=626&iid=748&page_name=detail&pid=62", "產後需求因人而異；有過敏、腎臟病或特殊飲食限制者先諮詢醫療人員。",
            ["月子餐", "一鍋到底", "高蛋白"],
            [("豬小排", "500 公克", 500, "g"), ("黑豆", "80 公克", 80, "g"), ("薏仁", "60 公克", 60, "g")],
            [("黑豆與薏仁洗淨浸泡，排骨汆燙後沖洗。", 1800), ("所有材料加足量水煮滾後轉小火。", 300), ("小火燉至豆仁與排骨軟熟，撇油後調味。", 3000)]),
        Create(
            "花椰菜馬鈴薯泥", "特殊照護",
            "六個月以上寶寶練習吞嚥的無加鹽細緻蔬菜泥。", 20, 1, 95, false,
            "NHS Start for Life", "https://www.nhs.uk/best-start-in-life/baby/recipes-and-meal-ideas/", "適用月齡須依寶寶發展與醫療建議；不加鹽糖，首次食材少量嘗試並全程看護。",
            ["寶寶副食品", "無加鹽", "新手友善"],
            [("青花椰菜", "40 公克", 40, "g"), ("馬鈴薯", "50 公克", 50, "g"), ("飲用水", "30 毫升", 30, "ml")],
            [("花椰菜與馬鈴薯洗淨切小塊。", 300), ("蒸至用叉子可輕易壓碎。", 720), ("加入少量溫水壓成適合寶寶發展階段的質地。", 180)]),
        Create(
            "南瓜雞肉犬用佐餐", "寵物料理",
            "無鹽南瓜與雞肉製成的犬用少量佐餐，不作完整主食。", 25, 4, 160, false,
            "UC Davis School of Veterinary Medicine", "https://healthtopics.vetmed.ucdavis.edu/health-topics/canine/treat-guidelines-dogs", "僅作偶爾佐餐且不超過每日熱量約一成；不可加洋蔥、蒜、葡萄、葡萄乾、巧克力或鹽，長期鮮食須諮詢獸醫營養師。",
            ["寵物鮮食", "犬用佐餐", "無加鹽"],
            [("去皮雞胸肉", "150 公克", 150, "g"), ("南瓜", "100 公克", 100, "g"), ("飲用水", "適量", null, null)],
            [("雞肉去皮去骨，南瓜去籽後切小塊。", 300), ("分別以清水煮至完全熟透，不加任何調味。", 900), ("放涼後切碎拌勻，依犬隻體型少量餵食。", 300)])
    ];

    private static RecipeSeedDefinition Create(
        string title,
        string category,
        string description,
        int cookingMinutes,
        int servings,
        decimal calories,
        bool isAiGenerated,
        string sourceName,
        string sourceUrl,
        string safetyNote,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<(string Name, string Display, decimal? Amount, string? Unit)> ingredients,
        IReadOnlyCollection<(string Instruction, int TimerSeconds)> steps)
    {
        var imageCode = Math.Abs(title.Aggregate(17, (hash, character) => hash * 31 + character)) % 1000;
        return new RecipeSeedDefinition(
            title,
            category,
            description,
            $"https://placehold.co/1200x800/f1ded5/5b382c?text=FriendlyFood+{imageCode}",
            cookingMinutes,
            servings,
            calories,
            isAiGenerated,
            sourceName,
            sourceUrl,
            safetyNote,
            tags,
            ingredients.Select(item => new IngredientSeedDefinition(
                item.Name,
                item.Display,
                item.Amount,
                item.Unit)).ToArray(),
            steps.Select(item => new StepSeedDefinition(
                item.Instruction,
                item.TimerSeconds)).ToArray());
    }
}
