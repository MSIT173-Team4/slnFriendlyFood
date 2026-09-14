using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartBot.Api.DTOs;
using SmartBot.Api.Services;

namespace SmartBot.Api.Controllers;

[ApiController]
[Route("api/pantry")]
public sealed class PantryApiController(IGeminiService geminiService) : ControllerBase
{
    private const long MaximumImageSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const string DiagnosticSystemInstruction = """
        你是熟悉台灣食材名稱與食品保存原則的多模態食材診斷助理。
        只分析照片中最主要的一種可食用食材；若無法辨識，ingredientName 回傳「無法辨識」。
        ingredientName 必須使用台灣常用標準名稱，例如西紅柿改為牛番茄、土豆改為馬鈴薯。
        freshnessStatus 應描述照片可觀察到的外觀狀態，不可宣稱已完成食品安全檢驗。
        recommendedLocation 只能是冷藏、冷凍或常溫。
        estimatedDays 必須是 1 到 30 的整數，採保守估計。
        storageTip 提供簡短、可執行的科學保鮮方法，並提醒異味、黏液或發霉時應丟棄。
        嚴格只回傳符合指定結構的合法 JSON，不得包含 Markdown、程式碼圍欄或額外說明。
        """;

    [HttpPost("diagnose-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize)]
    public async Task<ActionResult<ApiResponse<PantryAiDiagnosticDto>>> DiagnoseImage(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateImage(file);
        if (validationMessage is not null)
        {
            return BadRequest(ApiResponse<PantryAiDiagnosticDto>.Fail(validationMessage));
        }

        try
        {
            await using var memoryStream = new MemoryStream();
            await file!.CopyToAsync(memoryStream, cancellationToken);
            var rawResult = await geminiService.AnalyzeImageAsync(
                DiagnosticSystemInstruction,
                "評估照片中主要食材的名稱、可見新鮮度與建議保存方式。",
                memoryStream.ToArray(),
                file.ContentType,
                cancellationToken);

            var diagnostic = DeserializeDiagnostic(rawResult);
            return Ok(ApiResponse<PantryAiDiagnosticDto>.Ok(diagnostic, "AI 診斷完成"));
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<PantryAiDiagnosticDto>.Fail("AI 回傳格式不完整，請重新拍攝後再試一次。"));
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<PantryAiDiagnosticDto>.Fail(exception.Message));
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<PantryAiDiagnosticDto>.Fail("AI 診斷服務目前無法回應，請稍後再試。"));
        }
    }

    private static string? ValidateImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "請選擇要診斷的食材照片。";
        }

        if (file.Length > MaximumImageSize)
        {
            return "照片不可超過 10 MB。";
        }

        return AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant())
            ? null
            : "僅支援 JPEG、PNG 或 WebP 圖片。";
    }

    private static PantryAiDiagnosticDto DeserializeDiagnostic(string rawResult)
    {
        var cleanedJson = ExtractJsonObject(rawResult);
        var diagnostic = JsonSerializer.Deserialize<PantryAiDiagnosticDto>(
            cleanedJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("AI 診斷結果為空。");

        if (string.IsNullOrWhiteSpace(diagnostic.IngredientName) ||
            string.IsNullOrWhiteSpace(diagnostic.FreshnessStatus) ||
            string.IsNullOrWhiteSpace(diagnostic.StorageTip))
        {
            throw new JsonException("AI 診斷缺少必要欄位。");
        }

        diagnostic.IngredientName = diagnostic.IngredientName.Trim();
        diagnostic.FreshnessStatus = diagnostic.FreshnessStatus.Trim();
        diagnostic.StorageTip = diagnostic.StorageTip.Trim();
        diagnostic.RecommendedLocation = diagnostic.RecommendedLocation.Trim();
        diagnostic.EstimatedDays = Math.Clamp(diagnostic.EstimatedDays, 1, 30);

        if (diagnostic.RecommendedLocation is not ("冷藏" or "冷凍" or "常溫"))
        {
            diagnostic.RecommendedLocation = "冷藏";
        }

        return diagnostic;
    }

    private static string ExtractJsonObject(string rawResult)
    {
        var trimmed = rawResult.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new JsonException("AI 回傳內容不是 JSON 物件。");
        }

        return trimmed[firstBrace..(lastBrace + 1)];
    }
}
