namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

internal sealed record RecipeSeedDefinition(
    string Title,
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

internal static class RecipeSeedDefinitions
{
    public const string DemoOwnerUsername = "recipe.demo";

    private const string NutritionGovUrl = "https://www.nutrition.gov/recipes";
    private const string JapaneseFreezerRecipeUrl = "https://www.kikkoman.co.jp/homecook/theme/popular/shitaaji.html";
    private const string JapaneseFreezerSafetyUrl = "https://www.maff.go.jp/j/heya/sodan/1810/01.html";

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
        ("Cuisine", "日式料理"),
        ("Tool", "平底鍋"),
        ("Tool", "一鍋到底"),
        ("Difficulty", "新手友善"),
        ("Feature", "十五分鐘"),
        ("Feature", "高蛋白"),
        ("Feature", "零剩食"),
        ("Feature", "冷凍備料"),
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
            youtubeVideoId: "YYHm-m-EwJ4",
            seedViewCount: 420,
            seedLikeCount: 3,
            seedFavoriteCount: 3,
            publishedDaysAgo: 15),
        Create(
            "下味冷凍味噌豬肉菇菇燒", "異國料理",
            "週末將豬肉、味噌與菇類分裝冷凍，平日晚餐直接下鍋燒熟。", 18, 2, 520, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "冷凍袋須排出空氣並標示日期；食用前在冷藏室解凍，豬肉須完全加熱。",
            ["日式料理", "冷凍備料", "平底鍋"],
            [("豬里肌肉片", "300 公克", 300, "g"), ("鴻喜菇", "1 包", 100, "g"), ("味噌", "2 大匙", 30, "g"), ("青蔥", "1 根", 1, "根")],
            [("味噌加少量醬油與水拌勻，和豬肉一起放入冷凍袋抓勻。", 300), ("加入鴻喜菇、排出空氣、壓平後冷凍保存。", 180), ("前一晚移至冷藏解凍，倒入平底鍋加熱至豬肉全熟。", 720)],
            seedViewCount: 1420, seedLikeCount: 4, seedFavoriteCount: 4, publishedDaysAgo: 2),
        Create(
            "下味冷凍薑汁燒肉", "異國料理",
            "洋蔥與薑汁在冷凍期間入味，解凍後十分鐘完成日式燒肉。", 15, 2, 460, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "醃肉冷凍後應在冷藏室解凍，解凍完成即烹調且不可反覆冷凍。",
            ["日式料理", "冷凍備料", "十五分鐘", "平底鍋"],
            [("豬梅花肉片", "300 公克", 300, "g"), ("洋蔥", "1 顆", 1, "顆"), ("薑", "20 公克", 20, "g"), ("薄鹽醬油", "2 大匙", 30, "ml")],
            [("洋蔥切絲、薑磨泥，與醬油拌成醃汁。", 300), ("豬肉與醃汁裝袋，壓平排氣後冷凍。", 180), ("冷藏解凍後倒入熱鍋炒至豬肉熟透、醬汁收乾。", 600)],
            seedViewCount: 1360, seedLikeCount: 4, seedFavoriteCount: 3, publishedDaysAgo: 4),
        Create(
            "下味冷凍照燒雞腿", "異國料理",
            "雞腿先以日式照燒汁醃漬冷凍，回家後一只平底鍋即可完成。", 20, 2, 560, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "生雞肉與即食食材分開處理，雞腿最厚處須完全熟透。",
            ["日式料理", "冷凍備料", "平底鍋", "新手友善"],
            [("去骨雞腿肉", "2 片", 400, "g"), ("薄鹽醬油", "2 大匙", 30, "ml"), ("味醂", "2 大匙", 30, "ml"), ("薑", "10 公克", 10, "g")],
            [("雞腿擦乾，在肉面劃刀後與照燒醃汁一起裝袋。", 300), ("壓平排氣並冷凍，烹調前放冷藏室解凍。", 180), ("雞皮朝下煎上色，翻面蓋鍋煮熟，再收濃醬汁。", 900)],
            seedViewCount: 1580, seedLikeCount: 4, seedFavoriteCount: 4, publishedDaysAgo: 1),
        Create(
            "下味冷凍芝麻醬油雞胸", "健康輕食",
            "芝麻與醬油讓雞胸入味，適合一次分裝多份健身便當主菜。", 18, 2, 390, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "雞胸肉須冷藏解凍並完全加熱，醃料不可直接作為未加熱沾醬。",
            ["日式料理", "冷凍備料", "健身餐", "高蛋白"],
            [("雞胸肉", "300 公克", 300, "g"), ("白芝麻", "1 大匙", 10, "g"), ("薄鹽醬油", "1.5 大匙", 22.5m, "ml"), ("芝麻油", "1 小匙", 5, "ml")],
            [("雞胸肉切成等厚片，所有調味料在冷凍袋內混合。", 300), ("加入雞胸抓勻、排氣壓平後冷凍。", 180), ("冷藏解凍後以中小火煎至中心熟透。", 720)],
            seedViewCount: 1210, seedLikeCount: 4, seedFavoriteCount: 4, publishedDaysAgo: 6),
        Create(
            "下味冷凍日式燒肉牛肉", "異國料理",
            "牛肉片與洋蔥預先醃好，解凍後快炒即可配飯或做便當。", 12, 2, 540, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "牛肉應冷藏解凍並於解凍後儘快烹調；醬汁鈉含量可依需求減量。",
            ["日式料理", "冷凍備料", "十五分鐘", "平底鍋"],
            [("牛肉片", "300 公克", 300, "g"), ("洋蔥", "半顆", 0.5m, "顆"), ("薄鹽醬油", "2 大匙", 30, "ml"), ("白芝麻", "1 小匙", 5, "g")],
            [("洋蔥切絲，牛肉、洋蔥與醬汁一起裝袋。", 240), ("輕揉混合、排出空氣後平放冷凍。", 180), ("冷藏解凍後大火快炒，牛肉達理想熟度即起鍋。", 480)],
            seedViewCount: 1490, seedLikeCount: 4, seedFavoriteCount: 3, publishedDaysAgo: 3),
        Create(
            "下味冷凍甘辛肉燥", "家常菜",
            "日式甘辛絞肉可一次備好多包，退冰加熱後拌飯、拌麵都方便。", 15, 4, 580, false,
            "Kikkoman 下味冷凍特輯", JapaneseFreezerRecipeUrl, "絞肉需完全加熱；煮熟後若再冷凍，應先快速降溫並分裝。",
            ["日式料理", "冷凍備料", "十五分鐘", "一鍋到底"],
            [("豬絞肉", "400 公克", 400, "g"), ("洋蔥", "1 顆", 1, "顆"), ("薄鹽醬油", "3 大匙", 45, "ml"), ("味醂", "2 大匙", 30, "ml")],
            [("洋蔥切末，與絞肉及調味料裝入冷凍袋。", 300), ("隔袋揉散絞肉，薄薄壓平並畫分隔線後冷凍。", 180), ("冷藏解凍後倒入鍋中炒散，煮至肉末全熟。", 600)],
            seedViewCount: 1170, seedLikeCount: 3, seedFavoriteCount: 3, publishedDaysAgo: 7),
        Create(
            "冷凍備料豚汁味噌鍋", "異國料理",
            "根莖蔬菜與豬肉先分裝冷凍，料理時加水煮熟再拌入味噌。", 22, 4, 430, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "食材切薄並排氣冷凍；豬肉煮熟後才加入味噌，避免久煮使風味流失。",
            ["日式料理", "冷凍備料", "一鍋到底", "零剩食"],
            [("豬五花薄片", "250 公克", 250, "g"), ("白蘿蔔", "200 公克", 200, "g"), ("紅蘿蔔", "1 根", 1, "根"), ("味噌", "3 大匙", 45, "g")],
            [("白蘿蔔與紅蘿蔔切薄片，和豬肉分層裝袋冷凍。", 420), ("冷凍備料直接入鍋加水，煮滾後撇除浮沫。", 720), ("確認豬肉與根莖熟透，轉小火溶入味噌。", 300)],
            seedViewCount: 980, seedLikeCount: 3, seedFavoriteCount: 3, publishedDaysAgo: 9),
        Create(
            "冷凍烏龍蔬菜炒麵包", "異國料理",
            "冷凍烏龍麵搭配預切蔬菜包，忙碌時直接下鍋完成一餐。", 12, 2, 610, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "蔬菜洗後須擦乾再冷凍；肉片與蔬菜應分區放置並充分加熱。",
            ["日式料理", "冷凍備料", "十五分鐘", "平底鍋"],
            [("冷凍烏龍麵", "2 包", 400, "g"), ("豬肉片", "150 公克", 150, "g"), ("高麗菜", "150 公克", 150, "g"), ("紅蘿蔔", "半根", 0.5m, "根")],
            [("高麗菜切片、紅蘿蔔切絲，擦乾後與豬肉分區裝袋冷凍。", 420), ("平底鍋先炒熟豬肉與蔬菜，再加入冷凍烏龍麵。", 480), ("加少量水蓋鍋燜軟，淋醬油拌炒均勻。", 240)],
            seedViewCount: 1110, seedLikeCount: 3, seedFavoriteCount: 2, publishedDaysAgo: 10),
        Create(
            "冷凍鮭魚味噌燒", "異國料理",
            "鮭魚與味噌醬分裝冷凍，解凍後烤熟即可搭配白飯與蔬菜。", 20, 2, 490, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "魚片應冷藏解凍並烹調至中心熟透；出現異味、嚴重變色或過多霜粒時勿食用。",
            ["日式料理", "冷凍備料", "高蛋白"],
            [("大西洋鮭魚排", "2 片", 350, "g"), ("味噌", "2 大匙", 30, "g"), ("味醂", "1 大匙", 15, "ml"), ("青蔥", "1 根", 1, "根")],
            [("味噌與味醂拌勻，均勻抹在鮭魚兩面。", 240), ("魚片逐片包好後裝袋，排氣並平放冷凍。", 180), ("冷藏解凍後擦去過多醬料，以烤箱烤至中心熟透。", 900)],
            seedViewCount: 1330, seedLikeCount: 4, seedFavoriteCount: 4, publishedDaysAgo: 5),
        Create(
            "冷凍白菜雞肉奶油煮", "異國料理",
            "白菜與雞肉分裝冷凍後更快煮軟，加入牛奶完成日式家常奶油煮。", 20, 3, 470, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "牛奶不與生雞肉一起冷凍；料理時另行加入並將雞肉完全煮熟。",
            ["日式料理", "冷凍備料", "一鍋到底", "新手友善"],
            [("雞腿肉", "300 公克", 300, "g"), ("大白菜", "300 公克", 300, "g"), ("牛奶", "300 毫升", 300, "ml"), ("無鹽奶油", "20 公克", 20, "g")],
            [("雞腿切塊，白菜切段並擦乾，分區裝袋冷凍。", 420), ("冷凍備料入鍋加少量水，蓋鍋煮至雞肉熟透。", 720), ("加入牛奶與奶油，小火煮至湯汁濃滑。", 300)],
            seedViewCount: 890, seedLikeCount: 3, seedFavoriteCount: 2, publishedDaysAgo: 11),
        Create(
            "冷凍番茄鯖魚咖哩", "異國料理",
            "罐頭鯖魚搭配冷凍洋蔥番茄包，短時間完成有日式風味的咖哩。", 15, 3, 520, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "罐頭開封後未使用完須換容器冷藏；咖哩煮好後應儘快食用。",
            ["日式料理", "冷凍備料", "十五分鐘", "一鍋到底"],
            [("鯖魚罐頭", "1 罐", 190, "g"), ("牛番茄", "2 顆", 2, "顆"), ("洋蔥", "1 顆", 1, "顆"), ("咖哩塊", "2 小塊", 40, "g")],
            [("番茄切塊、洋蔥切絲，裝袋排氣後冷凍。", 300), ("冷凍蔬菜包入鍋加少量水，煮至洋蔥軟化。", 480), ("加入鯖魚與咖哩塊，小火拌煮至濃稠。", 300)],
            seedViewCount: 1020, seedLikeCount: 3, seedFavoriteCount: 3, publishedDaysAgo: 8),
        Create(
            "冷凍飯糰鮭魚茶泡飯", "異國料理",
            "剩飯做成鮭魚飯糰冷凍，忙碌早晨沖入熱高湯即可食用。", 10, 2, 360, false,
            "日本農林水產省 家庭冷凍注意事項", JapaneseFreezerSafetyUrl, "飯糰須趁新鮮快速冷卻後包緊冷凍，食用時加熱至中心冒熱氣。",
            ["日式料理", "冷凍備料", "十五分鐘", "零剩食"],
            [("白飯", "300 公克", 300, "g"), ("熟鮭魚", "80 公克", 80, "g"), ("海苔", "2 片", 2, "片"), ("青蔥", "1 根", 1, "根")],
            [("白飯拌入剝碎熟鮭魚，趁溫熱捏成兩顆飯糰。", 300), ("個別包緊、快速降溫後裝袋冷凍。", 180), ("飯糰加熱至中心冒熱氣，放入碗中沖入熱高湯並撒海苔。", 240)],
            seedViewCount: 1260, seedLikeCount: 4, seedFavoriteCount: 3, publishedDaysAgo: 3)
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
        string? youtubeVideoId = null,
        string? coverImageFileName = null,
        int? seedViewCount = null,
        int? seedLikeCount = null,
        int? seedFavoriteCount = null,
        int? publishedDaysAgo = null)
    {
        var imageCode = Math.Abs(title.Aggregate(17, (hash, character) => hash * 31 + character)) % 1000;
        var resolvedCoverImageFileName = coverImageFileName;
        if (resolvedCoverImageFileName is null)
        {
            CoverImageFileNames.TryGetValue(title, out resolvedCoverImageFileName);
        }

        var coverImageUrl = resolvedCoverImageFileName is null
            ? "/images/recipes/recipe-cover-fallback.svg"
            : $"/images/recipes/{resolvedCoverImageFileName}";

        return new RecipeSeedDefinition(
            title,
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
