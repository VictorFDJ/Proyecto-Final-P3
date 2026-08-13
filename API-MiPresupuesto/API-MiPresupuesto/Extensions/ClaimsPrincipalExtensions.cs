using System.Security.Claims;
using MiPresupuesto.Application.Common.Exceptions;

namespace MiPresupuesto.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedException("El token no contiene un usuario válido.");
    }
}
