using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.PaymentMethods;

public interface IPaymentMethodService
{
    Task<IReadOnlyList<PaymentMethodResponse>> GetAllAsync(Guid userId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> CreateAsync(Guid userId, CreatePaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> UpdateAsync(Guid userId, Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(Guid userId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid userId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasExpensesAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    void Add(PaymentMethod paymentMethod);
    void Remove(PaymentMethod paymentMethod);
}
