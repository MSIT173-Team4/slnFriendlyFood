using System.Net.Http.Json;
using System.Text.Json;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Common;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.ExternalServices.SmartBot;

public sealed class RecipeAiClient(
    HttpClient httpClient,
    IIngredientNameNormalizer ingredientNameNormalizer) : IRecipeAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ServiceResult<IngredientLocalizationDto>> LocalizeIngredientAsync(
        string ingredientName,
        CancellationToken cancellationToken)
    {
        var replyResult = await SendChatRequestAsync(
            "api/chat/localize-ingredient",
            ingredientName.Trim(),
            cancellationToken);
        var localizationResult = DeserializeJsonReply<IngredientLocalizationDto>(
            replyResult,
            "AI 食材名稱在地化完成。");

        if (!localizationResult.IsSuccess ||
            localizationResult.Data?.StandardTaiwaneseName is null)
        {
            return localizationResult;
        }

        var standardizedName = ingredientNameNormalizer.Normalize(
            localizationResult.Data.StandardTaiwaneseName);
        return ServiceResult<IngredientLocalizationDto>.Success(
            localizationResult.Data with { StandardTaiwaneseName = standardizedName },
            localizationResult.Message);
    }

    public async Task<ServiceResult<ParsedRecipeDto>> ParseRecipeAsync(
        string content,
        CancellationToken cancellationToken)
    {
        var replyResult = await SendChatRequestAsync(
            "api/chat/parse-recipe",
            content.Trim(),
            cancellationToken);
        return DeserializeJsonReply<ParsedRecipeDto>(
            replyResult,
            "AI 食譜結構化解析完成。");
    }

    public async Task<ServiceResult<ChefRecommendationDto>> RecommendRecipeAsync(
        IReadOnlyCollection<string> ingredientNames,
        CancellationToken cancellationToken)
    {
        var cleanedNames = ingredientNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (cleanedNames.Length == 0)
        {
            return ServiceResult<ChefRecommendationDto>.Validation(
                "至少需要提供一項有效食材。");
        }

        var replyResult = await SendChatRequestAsync(
            "api/chat/chef-recommend",
            string.Join("、", cleanedNames),
            cancellationToken);
        if (!replyResult.IsSuccess || replyResult.Data is null)
        {
            return ForwardFailure<ChefRecommendationDto>(replyResult);
        }

        return ServiceResult<ChefRecommendationDto>.Success(
            new ChefRecommendationDto(replyResult.Data.Reply, replyResult.Data.CreatedAt),
            "AI 主廚建議完成。");
    }

    private async Task<ServiceResult<SmartBotChatResponse>> SendChatRequestAsync(
        string relativeUrl,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                relativeUrl,
                new { message },
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return ServiceResult<SmartBotChatResponse>.Unexpected(
                    errorMessage ?? "SmartBot.Api 無法完成 AI 處理。");
            }

            var payload = await response.Content.ReadFromJsonAsync<SmartBotChatResponse>(
                JsonOptions,
                cancellationToken);
            return payload is null || string.IsNullOrWhiteSpace(payload.Reply)
                ? ServiceResult<SmartBotChatResponse>.Unexpected("SmartBot.Api 回傳空白結果。")
                : ServiceResult<SmartBotChatResponse>.Success(payload);
        }
        catch (HttpRequestException)
        {
            return ServiceResult<SmartBotChatResponse>.Unexpected(
                "無法連線至 SmartBot.Api，請確認 AI 服務已啟動。");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<SmartBotChatResponse>.Unexpected(
                "AI 服務等候逾時，請稍後再試。");
        }
        catch (JsonException)
        {
            return ServiceResult<SmartBotChatResponse>.Unexpected(
                "SmartBot.Api 回傳格式不正確，請稍後再試。");
        }
    }

    private static async Task<string?> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(rawContent);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static ServiceResult<T> DeserializeJsonReply<T>(
        ServiceResult<SmartBotChatResponse> replyResult,
        string successMessage)
    {
        if (!replyResult.IsSuccess || replyResult.Data is null)
        {
            return ForwardFailure<T>(replyResult);
        }

        try
        {
            var cleanedJson = CleanJson(replyResult.Data.Reply);
            var payload = JsonSerializer.Deserialize<T>(cleanedJson, JsonOptions);
            return payload is null
                ? ServiceResult<T>.Unexpected("AI 回傳內容無法轉成預期資料格式。")
                : ServiceResult<T>.Success(payload, successMessage);
        }
        catch (JsonException)
        {
            return ServiceResult<T>.Unexpected("AI 回傳的 JSON 格式不正確，請重新嘗試。");
        }
    }

    private static ServiceResult<T> ForwardFailure<T>(
        ServiceResult<SmartBotChatResponse> result) =>
        result.Status switch
        {
            ServiceResultStatus.ValidationError => ServiceResult<T>.Validation(result.Message),
            ServiceResultStatus.NotFound => ServiceResult<T>.NotFound(result.Message),
            ServiceResultStatus.Conflict => ServiceResult<T>.Conflict(result.Message),
            _ => ServiceResult<T>.Unexpected(result.Message)
        };

    private static string CleanJson(string rawReply)
    {
        var cleaned = rawReply.Trim()
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');
        return firstBrace >= 0 && lastBrace > firstBrace
            ? cleaned[firstBrace..(lastBrace + 1)]
            : cleaned;
    }

    private sealed record SmartBotChatResponse(string Reply, DateTime CreatedAt);

}
