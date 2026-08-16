using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Imports;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class ExpenseImportRepository(AppDbContext dbContext) : IExpenseImportRepository
{
    public async Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .Where(category => category.UserId == userId && category.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentMethod>> GetActivePaymentMethodsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await dbContext.PaymentMethods
            .Where(method => method.UserId == userId && method.IsActive)
            .ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<Expense> expenses) => dbContext.Expenses.AddRange(expenses);
}
