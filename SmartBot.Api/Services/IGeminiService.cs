namespace SmartBot.Api.Services;

public interface IGeminiService
{
    Task<string> GenerateReplyAsync(string userMessage);

    Task<string> GenerateReplyWithAgentAsync(
        string systemInstruction,
        string userMessage,
        double temperature = 0.2);

    Task<string> AnalyzeImageAsync(
        string systemInstruction,
        string userMessage,
        ReadOnlyMemory<byte> imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default);
}
