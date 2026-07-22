using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

/// <summary>
/// Base for medication, NFC, and other user-owned resources.
/// Requires JWT Bearer auth and exposes the authenticated user id for scoping queries.
/// </summary>
[Authorize]
[ApiController]
public abstract class AuthorizedApiControllerBase : ApiControllerBase
{
    private readonly ICurrentUserService _currentUser;

    protected AuthorizedApiControllerBase(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    protected Guid CurrentUserId => _currentUser.UserId;

    protected string? CurrentUserEmail => _currentUser.Email;
}
