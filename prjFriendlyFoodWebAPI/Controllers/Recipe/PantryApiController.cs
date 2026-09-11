using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Requests;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.ExternalServices.SmartBot;
using prjFriendlyFoodWebAPI.Services.Common;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/pantry")]
public sealed class PantryApiController(
    IPantryAiClient pantryAiClient,
    IPantryIntakeService pantryIntakeService) : BaseController
{
    private const long MaximumImageSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    [HttpPost("diagnose-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PantryAiDiagnosticDto>>> DiagnoseImage(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateImage(file);
        if (validationMessage is not null)
        {
            return FromServiceResult(
                ServiceResult<PantryAiDiagnosticDto>.Validation(
                    validationMessage));
        }

        var result = await pantryAiClient.DiagnoseImageAsync(file!, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPost("add-item")]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> AddItem(
        [FromBody] AddPantryItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await pantryIntakeService.AddItemAsync(request, cancellationToken);
        return FromServiceResult(result);
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
}
