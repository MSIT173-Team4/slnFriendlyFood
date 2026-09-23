namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

internal sealed record RecipeSeedDefinition(
    string Title,
    string AuthorUsername,
    string Category,
    string Description,
    string CoverImageUrl,
    string? YouTubeVideoId,
    int CookingMinutes,
    int Servings,
    decimal Calories,
    int SeedViewCount,
    int SeedLikeCount,
    int SeedFavoriteCount,
    int PublishedDaysAgo,
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

internal sealed record RecipeUserSeedDefinition(
    string Username,
    string Email,
    string IdNumber,
    bool IsActive);

internal static class RecipeSeedDefinitions
{
    public const string DemoOwnerUsername = "recipe.demo";
    public const string DemoTesterUsername = "recipe.tester";
    public const string HomeCookUsername = "小滿家常菜";
    public const string FitnessCookUsername = "健身便當日記";
    public const string FamilyCookUsername = "樂樂親子餐桌";
    public const string PetCookUsername = "毛孩鮮食筆記";

    private const string NutritionGovUrl = "https://www.nutrition.gov/recipes";

    private static readonly IReadOnlyDictionary<string, string> CoverImageFileNames =
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
            ["寶寶彩蔬軟飯小餐盤"] = "18-baby-vegetable-soft-rice.jpg"
        };

    public static readonly IReadOnlyCollection<RecipeUserSeedDefinition> Users =
    [
        new(DemoOwnerUsername, "recipe.demo@friendlyfood.local", "A123456789", true),
        new(DemoTesterUsername, "recipe.tester@friendlyfood.local", "B123456789", true),
        new(HomeCookUsername, "homecook.author@friendlyfood.local", "C123456789", false),
        new(FitnessCookUsername, "fitness.author@friendlyfood.local", "D123456789", false),
        new(FamilyCookUsername, "family.author@friendlyfood.local", "E123456789", false),
        new(PetCookUsername, "pet.author@friendlyfood.local", "F123456789", false)
    ];

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
            [("鮭魚擦乾後調味，蘆筍切除粗硬末端。", 300), ("魚皮朝下以中火煎至金黃，再翻面煎熟。", 480), ("蘆筍入鍋煎熟，淋上柳橙汁後盛盤。", 180)],
            coverImageFileName: "01-pan-seared-salmon.jpg",
            seedViewCount: 1680,
            seedLikeCount: 4,
            seedFavoriteCount: 3,
            publishedDaysAgo: 2),
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
            [("雞肉去皮去骨，南瓜去籽後切小塊。", 300), ("分別以清水煮至完全熟透，不加任何調味。", 900), ("放涼後切碎拌勻，依犬隻體型少量餵食。", 300)]),
        Create(
            "台式肉絲家常炒麵", "家常菜",
            "油麵吸附醬香，搭配肉絲與高麗菜，是適合清冰箱的台式家常主食。", 25, 2, 620, false,
            "YouTube／H.tool的廚房日誌", "https://www.youtube.com/watch?v=xnvJGP0BDr8", "本食譜依影片主題改寫為測試資料；豬肉須完全加熱。",
            ["台式家常", "平底鍋", "零剩食"],
            [("油麵", "300 公克", 300, "g"), ("豬里肌肉絲", "120 公克", 120, "g"), ("高麗菜", "150 公克", 150, "g"), ("紅蘿蔔", "半根", 0.5m, "根")],
            [("高麗菜切絲，紅蘿蔔切細條，肉絲以少量醬油抓勻。", 420), ("熱鍋後炒熟肉絲，先盛出備用。", 300), ("原鍋炒軟紅蘿蔔與高麗菜。", 300), ("加入油麵與少量水，蓋鍋燜至麵體鬆開。", 240), ("放回肉絲並加入醬油拌炒均勻。", 180), ("確認肉絲熟透、湯汁收乾後盛盤。", 120)],
            authorUsername: HomeCookUsername,
            youtubeVideoId: "xnvJGP0BDr8",
            seedViewCount: 930,
            seedLikeCount: 4,
            seedFavoriteCount: 3,
            publishedDaysAgo: 5),
        Create(
            "馬鈴薯豬肉咖哩", "異國料理",
            "柔嫩豬肉與根莖蔬菜慢煮入味，隔天加熱也適合的家庭咖哩。", 45, 4, 680, false,
            "YouTube／H.tool的廚房日誌", "https://www.youtube.com/watch?v=hT1pCR45bWU", "本食譜依影片主題改寫為測試資料；咖哩塊鈉含量較高，可依需求減量。",
            ["一鍋到底", "新手友善", "零剩食"],
            [("豬梅花肉", "300 公克", 300, "g"), ("馬鈴薯", "2 顆", 2, "顆"), ("紅蘿蔔", "1 根", 1, "根"), ("洋蔥", "1 顆", 1, "顆"), ("咖哩塊", "4 小塊", 80, "g")],
            [("豬肉切塊，馬鈴薯與紅蘿蔔滾刀切，洋蔥切片。", 600), ("鍋中少油煎香豬肉表面。", 360), ("加入洋蔥炒至透明，再放入根莖蔬菜。", 360), ("加水蓋過食材，煮滾後轉小火。", 300), ("燉至蔬菜柔軟後關小火，放入咖哩塊攪拌融化。", 1200), ("重新小火煮至濃稠，確認豬肉熟透後完成。", 300)],
            authorUsername: HomeCookUsername,
            youtubeVideoId: "hT1pCR45bWU",
            seedViewCount: 760,
            seedLikeCount: 3,
            seedFavoriteCount: 3,
            publishedDaysAgo: 8),
        Create(
            "柴魚涼拌洋蔥絲", "家常菜",
            "冰鎮洋蔥保留爽脆口感，搭配柴魚與和風醬汁，十分鐘快速上桌。", 10, 2, 120, false,
            "YouTube／H.tool的廚房日誌", "https://www.youtube.com/watch?v=ebW1XCD7bYQ", "本食譜依影片主題改寫為測試資料；洋蔥辛辣度可用冰水浸泡時間調整。",
            ["十五分鐘", "新手友善", "零剩食"],
            [("洋蔥", "1 顆", 1, "顆"), ("柴魚片", "5 公克", 5, "g"), ("薄鹽醬油", "1 大匙", 15, "ml"), ("白醋", "1 小匙", 5, "ml")],
            [("洋蔥逆紋切成均勻細絲。", 180), ("放入冰水輕抓後浸泡，降低辛辣感。", 300), ("瀝乾洋蔥並以廚房紙巾吸除多餘水分。", 120), ("醬油與白醋混合成醬汁。", 60), ("洋蔥裝盤，淋醬後撒上柴魚片。", 60)],
            authorUsername: HomeCookUsername,
            youtubeVideoId: "ebW1XCD7bYQ",
            seedViewCount: 260,
            seedLikeCount: 2,
            seedFavoriteCount: 1,
            publishedDaysAgo: 3),
        Create(
            "冬瓜蛤蜊清湯", "家常菜",
            "冬瓜清甜、蛤蜊鮮味自然釋出，不需繁複調味的清爽湯品。", 30, 4, 150, false,
            "YouTube／H.tool的廚房日誌", "https://www.youtube.com/watch?v=FwsfSbGOcuQ", "本食譜依影片主題改寫為測試資料；未開殼或有異味的蛤蜊不得食用。",
            ["台式家常", "一鍋到底", "新手友善"],
            [("冬瓜", "500 公克", 500, "g"), ("蛤蜊", "300 公克", 300, "g"), ("薑", "3 片", 15, "g"), ("青蔥", "1 根", 1, "根")],
            [("蛤蜊吐沙後刷洗外殼，冬瓜去皮去籽切塊。", 900), ("鍋中加水、薑片與冬瓜煮滾。", 300), ("轉中小火煮至冬瓜邊緣透明。", 600), ("放入蛤蜊並蓋鍋煮至開殼。", 240), ("撈除未開殼蛤蜊，撒上蔥花後完成。", 120)],
            authorUsername: HomeCookUsername,
            youtubeVideoId: "FwsfSbGOcuQ",
            seedViewCount: 1040,
            seedLikeCount: 4,
            seedFavoriteCount: 2,
            publishedDaysAgo: 4),
        Create(
            "古早味醬香滷豆腐", "家常菜",
            "板豆腐煎出金黃表面後吸收醬汁，是簡單下飯的台灣家常料理。", 35, 4, 260, false,
            "YouTube／KIMLAN金蘭醬油", "https://www.youtube.com/watch?v=BWrMXB31YH8", "本食譜依影片主題改寫為測試資料；醬油鈉含量較高，可依需求減量。",
            ["台式家常", "一鍋到底", "新手友善"],
            [("板豆腐", "2 盒", 800, "g"), ("薄鹽醬油", "3 大匙", 45, "ml"), ("薑", "4 片", 20, "g"), ("青蔥", "2 根", 2, "根")],
            [("豆腐以紙巾吸乾水分，切成厚片。", 300), ("平底鍋加少量油，將豆腐兩面煎至金黃。", 600), ("加入薑片與蔥白炒香。", 120), ("倒入醬油與清水至豆腐一半高度。", 120), ("小火滷煮並中途翻面，使兩面均勻入味。", 900), ("湯汁略收後撒上蔥綠即可。", 180)],
            authorUsername: HomeCookUsername,
            youtubeVideoId: "BWrMXB31YH8",
            seedViewCount: 580,
            seedLikeCount: 3,
            seedFavoriteCount: 2,
            publishedDaysAgo: 12),
        Create(
            "寶寶彩蔬軟飯小餐盤", "特殊照護",
            "以軟飯、根莖蔬菜與雞肉組成可依月齡調整質地的寶寶餐盤。", 35, 2, 210, false,
            "YouTube／international mommy國際媽咪", "https://www.youtube.com/watch?v=YYHm-m-EwJ4", "本食譜依影片主題改寫為測試資料；月齡、質地與過敏原須依寶寶發展及醫療建議調整，全程陪同進食。",
            ["寶寶副食品", "無加鹽", "一鍋到底"],
            [("白飯", "100 公克", 100, "g"), ("雞胸肉", "40 公克", 40, "g"), ("紅蘿蔔", "20 公克", 20, "g"), ("青花椰菜", "20 公克", 20, "g"), ("飲用水", "200 毫升", 200, "ml")],
            [("雞肉去筋切碎，蔬菜洗淨後切成適合月齡的小丁。", 420), ("鍋中加入白飯與水，以小火煮開。", 300), ("加入雞肉碎並充分攪散。", 300), ("加入紅蘿蔔與青花椰菜，持續小火燉煮。", 600), ("確認雞肉熟透、蔬菜柔軟後關火。", 180), ("依寶寶發展壓碎或剪細，放涼至適口溫度再餵食。", 180)],
            authorUsername: FamilyCookUsername,
            youtubeVideoId: "YYHm-m-EwJ4",
            seedViewCount: 420,
            seedLikeCount: 3,
            seedFavoriteCount: 3,
            publishedDaysAgo: 15)
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
        IReadOnlyCollection<(string Instruction, int TimerSeconds)> steps,
        string? authorUsername = null,
        string? youtubeVideoId = null,
        string? coverImageFileName = null,
        int? seedViewCount = null,
        int? seedLikeCount = null,
        int? seedFavoriteCount = null,
        int? publishedDaysAgo = null)
    {
        var imageCode = Math.Abs(title.Aggregate(17, (hash, character) => hash * 31 + character)) % 1000;
        var resolvedAuthorUsername = authorUsername ?? category switch
        {
            "家常菜" => HomeCookUsername,
            "健康輕食" => FitnessCookUsername,
            "特殊照護" => FamilyCookUsername,
            "寵物料理" => PetCookUsername,
            _ => DemoOwnerUsername
        };
        var resolvedCoverImageFileName = coverImageFileName;
        if (resolvedCoverImageFileName is null)
        {
            CoverImageFileNames.TryGetValue(title, out resolvedCoverImageFileName);
        }

        var coverImageUrl = resolvedCoverImageFileName is null
            ? $"https://placehold.co/1200x800/f1ded5/5b382c?text=FriendlyFood+{imageCode}"
            : $"/images/recipes/{resolvedCoverImageFileName}";

        return new RecipeSeedDefinition(
            title,
            resolvedAuthorUsername,
            category,
            description,
            coverImageUrl,
            youtubeVideoId,
            cookingMinutes,
            servings,
            calories,
            seedViewCount ?? 180 + imageCode,
            seedLikeCount ?? 1 + imageCode % 4,
            seedFavoriteCount ?? imageCode % 3,
            publishedDaysAgo ?? 1 + imageCode % 90,
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
