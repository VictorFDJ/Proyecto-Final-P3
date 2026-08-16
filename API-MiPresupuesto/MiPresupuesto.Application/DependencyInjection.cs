using Microsoft.Extensions.DependencyInjection;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Categories;
using MiPresupuesto.Application.Expenses;
using MiPresupuesto.Application.PaymentMethods;
using MiPresupuesto.Application.Profile;

namespace MiPresupuesto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        return services;
    }
}
