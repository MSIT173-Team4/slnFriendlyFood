using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Recipe;
using prjFriendlyFoodWebAPI.Services.Common;

namespace prjFriendlyFoodWebAPI.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> FromServiceResult<T>(ServiceResult<T> result)
    {
        var response = result.IsSuccess
            ? ApiResponse<T>.Ok(result.Data!, result.Message)
            : ApiResponse<T>.Fail(result.Message, result.Errors);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(response),
            ServiceResultStatus.Created => StatusCode(StatusCodes.Status201Created, response),
            ServiceResultStatus.ValidationError => BadRequest(response),
            ServiceResultStatus.NotFound => NotFound(response),
            ServiceResultStatus.Conflict => Conflict(response),
            _ => StatusCode(StatusCodes.Status500InternalServerError, response)
        };
    }
}
