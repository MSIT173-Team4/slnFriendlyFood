using Microsoft.AspNetCore.Http;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.ExternalServices.SmartBot;

public interface IPantryAiClient
{
    Task<ServiceResult<PantryAiDiagnosticDto>> DiagnoseImageAsync(
        IFormFile file,
        CancellationToken cancellationToken);
}
