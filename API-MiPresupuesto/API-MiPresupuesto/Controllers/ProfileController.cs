using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Profile;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get(CancellationToken cancellationToken)
        => Ok(await profileService.GetAsync(User.GetUserId(), cancellationToken));

    [HttpPut("name")]
    public async Task<ActionResult<UserResponse>> UpdateName(
        UpdateNameRequest request,
        CancellationToken cancellationToken)
        => Ok(await profileService.UpdateNameAsync(User.GetUserId(), request, cancellationToken));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await profileService.ChangePasswordAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }
}
