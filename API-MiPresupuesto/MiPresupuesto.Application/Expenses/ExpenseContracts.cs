using MiPresupuesto.Application.Common.Models;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Expenses;

public interface IExpenseService
{
    Task<PagedResponse<ExpenseResponse>> GetAllAsync(Guid userId, ExpenseQuery query, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> CreateAsync(Guid userId, CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> UpdateAsync(Guid userId, Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface IExpenseRepository
{
    Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetPageAsync(
        Guid userId,
        ExpenseQuery query,
        CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetActiveCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetActivePaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken = default);
    void Add(Expense expense);
    void Remove(Expense expense);
}
