using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.DTOs.Recipe.Responses;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Controllers.Recipe;

[Route("api/recipe/assets")]
public sealed class RecipeAssetController(IRecipeAssetService assetService) : BaseController
{
    [HttpPost("cover")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<RecipeAssetResponseDto>>> UploadCover(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var result = await assetService.SaveCoverAsync(file, cancellationToken);
        return FromServiceResult(result);
    }
}
