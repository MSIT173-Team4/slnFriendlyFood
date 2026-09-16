using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartBot.Api.Configurations;

namespace SmartBot.Api.Services;

public sealed class GeminiService(
    HttpClient httpClient,
    IOptions<GeminiAgentOptions> options) : IGeminiService
{
    private readonly GeminiAgentOptions agentOptions = options.Value;

    public Task<string> GenerateReplyAsync(string userMessage) =>
        GenerateReplyWithAgentAsync(string.Empty, userMessage, 0.7);

    public async Task<string> GenerateReplyWithAgentAsync(
        string systemInstruction,
        string userMessage,
        double temperature = 0.2)
    {
        object? systemInstructionPayload = string.IsNullOrWhiteSpace(systemInstruction)
            ? null
            : new { parts = new[] { new { text = systemInstruction } } };

        var requestPayload = new
        {
            system_instruction = systemInstructionPayload,
            contents = new[]
            {
                new { parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { temperature }
        };

        return await SendAsync(requestPayload, CancellationToken.None);
    }

    public async Task<string> AnalyzeImageAsync(
        string systemInstruction,
        string userMessage,
        ReadOnlyMemory<byte> imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var requestPayload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = userMessage },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = Convert.ToBase64String(imageBytes.Span)
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        ingredientName = new { type = "STRING" },
                        freshnessStatus = new { type = "STRING" },
                        recommendedLocation = new
                        {
                            type = "STRING",
                            @enum = new[] { "冷藏", "冷凍", "常溫" }
                        },
                        storageTip = new { type = "STRING" },
                        estimatedDays = new { type = "INTEGER" }
                    },
                    required = new[]
                    {
                        "ingredientName",
                        "freshnessStatus",
                        "recommendedLocation",
                        "storageTip",
                        "estimatedDays"
                    }
                }
            }
        };

        return await SendAsync(requestPayload, cancellationToken);
    }

    private async Task<string> SendAsync(object requestPayload, CancellationToken cancellationToken)
    {
        var apiKey = agentOptions.ApiKey.Trim();
        var modelName = agentOptions.ModelName.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"未找到 {GeminiAgentOptions.SectionName}:ApiKey，請確認 User Secrets 設定。");
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new InvalidOperationException(
                $"未找到 {GeminiAgentOptions.SectionName}:ModelName，請確認 User Secrets 設定。");
        }

        var encodedModelName = Uri.EscapeDataString(modelName);
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{encodedModelName}:generateContent";
        var jsonBody = JsonSerializer.Serialize(requestPayload);
        using var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = httpContent
        };
        requestMessage.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API 呼叫失敗 [{response.StatusCode}]: {responseJson}",
                null,
                response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;
        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.GetArrayLength() > 0 &&
            parts[0].TryGetProperty("text", out var text))
        {
            return text.GetString() ?? throw new JsonException("Gemini 回傳文字為空。");
        }

        throw new JsonException("Gemini 回應缺少 candidates.content.parts.text。");
    }
}
