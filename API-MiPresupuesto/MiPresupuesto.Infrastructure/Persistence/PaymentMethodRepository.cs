using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.PaymentMethods;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class PaymentMethodRepository(AppDbContext dbContext) : IPaymentMethodRepository
{
    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
        => await dbContext.PaymentMethods
            .AsNoTracking()
            .Where(method => method.UserId == userId && (includeInactive || method.IsActive))
            .OrderBy(method => method.Name)
            .ToListAsync(cancellationToken);

    public Task<PaymentMethod?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => dbContext.PaymentMethods.SingleOrDefaultAsync(
            method => method.UserId == userId && method.Id == id,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
        => dbContext.PaymentMethods.AnyAsync(
            method => method.UserId == userId &&
                      method.Name == name &&
                      (!excludingId.HasValue || method.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> HasExpensesAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => dbContext.Expenses.AnyAsync(
            expense => expense.UserId == userId && expense.PaymentMethodId == id,
            cancellationToken);

    public void Add(PaymentMethod paymentMethod) => dbContext.PaymentMethods.Add(paymentMethod);
    public void Remove(PaymentMethod paymentMethod) => dbContext.PaymentMethods.Remove(paymentMethod);
}
