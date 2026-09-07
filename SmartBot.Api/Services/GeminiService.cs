using System.Text;
using System.Text.Json;

namespace SmartBot.Api.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // 模型識別代碼常數 (全小寫、連字號，嚴禁帶有空格)
    private const string DefaultModelId = "gemini-3.5-flash-lite";

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    /// <summary>
    /// 通用對話接口 (MVP 核心骨架)
    /// </summary>
    public async Task<string> GenerateReplyAsync(string userMessage)
    {
        return await GenerateReplyWithAgentAsync(string.Empty, userMessage, 0.7);
    }

    /// <summary>
    /// 支援專屬 Agent 系統人設與精準溫度的呼叫接口
    /// </summary>
    public async Task<string> GenerateReplyWithAgentAsync(string systemInstruction, string userMessage, double temperature = 0.2)
    {
        // 1. 取得 API Key 並進行頭尾去空白防禦
        string apiKey = (Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                        ?? _configuration["GEMINI_API_KEY"]
                        ?? "").Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("未找到 GEMINI_API_KEY，請確認 .env 檔案內容或環境變數！");
        }

        // 2. 組裝標準 Google REST API 端點
        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{DefaultModelId}:generateContent";

        // 3. 依據是否有設定 Agent 人設，動態建構 system_instruction 區塊
        object? systemInstructionPayload = null;
        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            systemInstructionPayload = new
            {
                parts = new[] { new { text = systemInstruction } }
            };
        }

        // 4. 打包符合 Google 規範的整體請求主體 (Payload)
        var requestPayload = new
        {
            system_instruction = systemInstructionPayload,
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userMessage } }
                }
            },
            generationConfig = new
            {
                temperature = temperature
            }
        };

        // 5. 序列化為 UTF-8 編碼的 JSON 內容
        string jsonBody = JsonSerializer.Serialize(requestPayload);
        using var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        // 6. 建立 HttpRequestMessage 並指派 x-goog-api-key 標頭
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = httpContent
        };
        requestMessage.Headers.Add("x-goog-api-key", apiKey);

        // 7. 非同步發送請求
        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
        string responseJson = await response.Content.ReadAsStringAsync();

        // 8. 狀態碼非 200 檢查防禦
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API 呼叫失敗 [{response.StatusCode}]: {responseJson}");
        }

        // 9. 具備高容錯度的 JSON 解析
        using JsonDocument doc = JsonDocument.Parse(responseJson);
        var rootElement = doc.RootElement;

        if (rootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                return parts[0].GetProperty("text").GetString() ?? "Gemini 回傳內容為空字串。";
            }
        }

        return "Gemini 服務未產出任何有效文字回應。";
    }
}