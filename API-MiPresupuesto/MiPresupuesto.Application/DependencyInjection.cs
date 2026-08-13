using Microsoft.Extensions.DependencyInjection;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Profile;

namespace MiPresupuesto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        return services;
    }
}
