using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartBot.Api.DTOS;
using SmartBot.Api.Services;

namespace SmartBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IGeminiService _geminiService;

        // Agent 1 提示詞：在地化審查
        private const string LocalizerPrompt = """
        你是一位專業的台灣食材詞彙審查員。
        任務：將任何異國用語、中國大陸用語、俗稱、簡體或外來語，轉換為台灣常見標準繁體中文食材名稱。
        規則：
        1. 輸出必須完全是合法的 JSON 格式，嚴禁包含 Markdown 標籤（如 ```json ）。
        2. JSON 格式規格：
           {"rawInput": "原始輸入", "standardTaiwaneseName": "標準名稱", "category": "分類(蔬菜/肉類/海鮮/水果/調味料/其他)"}
        3. 若輸入並非食材，standardTaiwaneseName 請填寫 null，category 填寫 "非食材"，除了上述外，不要加入任何其他說明文字。。
        """;

        // Agent 2 提示詞：食譜解析
        private const string RecipeParserPrompt = """
        你是一位食譜結構化資料助手。
        任務：將使用者隨手輸入的食譜文字，拆解為標準化食材計量與依序排列的步驟。
        規則：
        1. 單位盡量換算為公制（公克 g、毫升 ml、根、顆、匙）。
        2. 嚴格輸出 JSON 格式，禁止任何額外文字或 Markdown 標籤。
        3. JSON 格式規格：
           {
             "recipeTitle": "菜名",
             "ingredients": [
               {"name": "食材名稱", "amount": 100, "unit": "g"}
             ],
             "steps": [
               {"stepNumber": 1, "description": "動作說明"}
             ]
           }
        不要加入任何其他說明文字。
        """;

        // Agent 3 提示詞：智慧主廚
        private const string ChefPrompt = """
        你是一位精通台灣家庭料理的資深主廚。
        任務：根據使用者提供的剩餘食材清單，發想 1 道料理，並提供實用烹調撇步。
        規則：
        1. 優先使用現有食材，並明確標記需要額外補充的基本調味料。
        2. 輸出格式必須嚴格如下：
           【推薦料理名稱】
           * 現有食材運用：
           * 需補充調味：
           * 簡易作法：
           * 主廚私房小撇步：（例如去腥、保鮮、火候技巧）
        3. 禁止出現非繁體中文字詞，不回答料理與食材以外的話題。
        """;

        public ChatController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        /// <summary>
        /// 原本的通用測試端點
        /// </summary>
        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponseDto>> AskAi([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "提問內容不可為空。" });
            }

            string result = await _geminiService.GenerateReplyAsync(request.Message);
            return Ok(new ChatResponseDto { Reply = result, CreatedAt = DateTime.UtcNow });
        }

        /// <summary>
        /// Agent 1：食材名稱在地化
        /// </summary>
        [HttpPost("localize-ingredient")]
        public async Task<ActionResult<ChatResponseDto>> LocalizeIngredient([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "食材名稱不可為空。" });
            }

            string result = await _geminiService.GenerateReplyWithAgentAsync(LocalizerPrompt, request.Message, temperature: 0.1);
            return Ok(new ChatResponseDto { Reply = result, CreatedAt = DateTime.UtcNow });
        }

        /// <summary>
        /// Agent 2：食譜結構化解析
        /// </summary>
        [HttpPost("parse-recipe")]
        public async Task<ActionResult<ChatResponseDto>> ParseRecipe([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "食譜內容不可為空。" });
            }

            string result = await _geminiService.GenerateReplyWithAgentAsync(RecipeParserPrompt, request.Message, temperature: 0.1);
            return Ok(new ChatResponseDto { Reply = result, CreatedAt = DateTime.UtcNow });
        }

        /// <summary>
        /// Agent 3：清冰箱智慧主廚
        /// </summary>
        [HttpPost("chef-recommend")]
        public async Task<ActionResult<ChatResponseDto>> ChefRecommend([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "剩餘食材清單不可為空。" });
            }

            string result = await _geminiService.GenerateReplyWithAgentAsync(ChefPrompt, request.Message, temperature: 0.7);
            return Ok(new ChatResponseDto { Reply = result, CreatedAt = DateTime.UtcNow });
        }
    }
}
