using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Common.Validation;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.PaymentMethods;

public sealed class PaymentMethodService(
    IPaymentMethodRepository paymentMethods,
    IUnitOfWork unitOfWork) : IPaymentMethodService
{
    public async Task<IReadOnlyList<PaymentMethodResponse>> GetAllAsync(
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
        => (await paymentMethods.GetAllAsync(userId, includeInactive, cancellationToken))
            .Select(Map)
            .ToArray();

    public async Task<PaymentMethodResponse> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => Map(await GetOwnedAsync(userId, id, cancellationToken));

    public async Task<PaymentMethodResponse> CreateAsync(
        Guid userId,
        CreatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = InputValidator.Required(request.Name, "name", 80);
        if (await paymentMethods.NameExistsAsync(userId, name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe un método de pago con este nombre.");
        }

        var paymentMethod = new PaymentMethod
        {
            UserId = userId,
            Name = name,
            Icon = InputValidator.Optional(request.Icon, "icon", 50)
        };

        paymentMethods.Add(paymentMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(paymentMethod);
    }

    public async Task<PaymentMethodResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = await GetOwnedAsync(userId, id, cancellationToken);
        var name = InputValidator.Required(request.Name, "name", 80);
        if (await paymentMethods.NameExistsAsync(userId, name, id, cancellationToken))
        {
            throw new ConflictException("Ya existe un método de pago con este nombre.");
        }

        paymentMethod.Name = name;
        paymentMethod.Icon = InputValidator.Optional(request.Icon, "icon", 50);
        paymentMethod.IsActive = request.IsActive;
        paymentMethod.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(paymentMethod);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = await GetOwnedAsync(userId, id, cancellationToken);
        if (await paymentMethods.HasExpensesAsync(userId, id, cancellationToken))
        {
            throw new ConflictException(
                "No se puede eliminar el método de pago porque tiene gastos asociados. Puedes desactivarlo.");
        }

        paymentMethods.Remove(paymentMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<PaymentMethod> GetOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => await paymentMethods.GetByIdAsync(userId, id, cancellationToken)
           ?? throw new NotFoundException("El método de pago no existe.");

    private static PaymentMethodResponse Map(PaymentMethod paymentMethod) => new(
        paymentMethod.Id,
        paymentMethod.Name,
        paymentMethod.Icon,
        paymentMethod.IsActive,
        paymentMethod.CreatedAtUtc);
}
