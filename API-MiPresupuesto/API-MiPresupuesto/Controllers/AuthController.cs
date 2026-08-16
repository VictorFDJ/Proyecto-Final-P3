using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Application.Auth;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(
    IAuthService authService,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return response is null
            ? Conflict(new
            {
                success = false,
                error = new
                {
                    code = "conflict",
                    message = "Ya existe una cuenta con este correo electrónico.",
                    errors = (object?)null,
                    traceId = HttpContext.TraceIdentifier
                }
            })
            : StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "unauthorized",
                    message = "El correo o la contraseña son incorrectos.",
                    errors = (object?)null,
                    traceId = HttpContext.TraceIdentifier
                }
            })
            : Ok(response);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType<ForgotPasswordResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var token = await authService.RequestPasswordResetAsync(request, cancellationToken);
        var exposeToken = environment.IsDevelopment() ||
            configuration.GetValue<bool>("PasswordReset:ExposeToken");
        return Ok(new ForgotPasswordResponse(
            exposeToken
                ? token is null
                    ? "No encontramos una cuenta registrada con ese correo."
                    : "Código temporal generado correctamente."
                : "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña.",
            exposeToken ? token : null));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var changed = await authService.ResetPasswordAsync(request, cancellationToken);
        if (changed)
        {
            return NoContent();
        }

        return BadRequest(new
        {
            success = false,
            error = new
            {
                code = "invalid_reset_token",
                message = "El código de recuperación no es válido o ya expiró.",
                errors = (object?)null,
                traceId = HttpContext.TraceIdentifier
            }
        });
    }
}
