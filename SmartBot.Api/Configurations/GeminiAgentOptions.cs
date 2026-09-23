namespace SmartBot.Api.Configurations;

public class GeminiAgentOptions
{
    public const string SectionName = "GeminiAgent";

    public string ApiKey { get; set; } = string.Empty;

    public string ModelName { get; set; } = "gemini-3.5-flash-lite";
}
