using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }
}
