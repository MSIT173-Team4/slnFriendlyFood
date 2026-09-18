using System.Net.Http.Headers;
using System.Net.Http.Json;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.ExternalServices.SmartBot;

public sealed class SmartBotPantryAiClient(HttpClient httpClient) : IPantryAiClient
{
    public async Task<ServiceResult<IReadOnlyCollection<PantryAiDiagnosticDto>>> DiagnoseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var imageStream = file.OpenReadStream();
            using var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            using var formData = new MultipartFormDataContent
            {
                { imageContent, "file", file.FileName }
            };

            using var response = await httpClient.PostAsync(
                "api/pantry/diagnose-image",
                formData,
                cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<
                ApiResponse<IReadOnlyCollection<PantryAiDiagnosticDto>>>(
                cancellationToken);

            if (response.IsSuccessStatusCode && payload?.Success == true && payload.Data is not null)
            {
                return ServiceResult<IReadOnlyCollection<PantryAiDiagnosticDto>>.Success(
                    payload.Data,
                    payload.Message);
            }

            return ServiceResult<IReadOnlyCollection<PantryAiDiagnosticDto>>.Unexpected(
                payload?.Message ?? "AI 診斷服務目前無法回應。");
        }
        catch (HttpRequestException)
        {
            return ServiceResult<IReadOnlyCollection<PantryAiDiagnosticDto>>.Unexpected(
                "無法連線至 SmartBot.Api，請確認 AI 服務已啟動。");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<IReadOnlyCollection<PantryAiDiagnosticDto>>.Unexpected(
                "AI 診斷等候逾時，請稍後再試。");
        }
    }
}
