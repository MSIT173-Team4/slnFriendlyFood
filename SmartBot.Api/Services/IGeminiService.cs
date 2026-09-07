namespace SmartBot.Api.Services
{
    public interface IGeminiService
    {
        // 傳入使用者文字，非同步回傳 AI 回覆文字
        Task<string> GenerateReplyAsync(string userMessage);
        // Agent人設與溫度調控
        Task<string> GenerateReplyWithAgentAsync(string systemInstruction, string userMessage, double temperature = 0.2);
    }
}
