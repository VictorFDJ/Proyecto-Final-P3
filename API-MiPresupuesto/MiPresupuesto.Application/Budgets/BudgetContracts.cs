using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Budgets;

public sealed record BudgetWithSpent(Budget Budget, decimal Spent);

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetAllAsync(Guid userId, int? year, int? month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetResponse>> GetExceededAsync(Guid userId, int? year, int? month, CancellationToken cancellationToken = default);
    Task<BudgetResponse> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<BudgetResponse> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<BudgetResponse> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface IBudgetRepository
{
    Task<IReadOnlyList<BudgetWithSpent>> GetAllWithSpentAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    Task<Budget?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<decimal> GetSpentAsync(Guid userId, Guid categoryId, int year, int month, CancellationToken cancellationToken = default);
    Task<Category?> GetActiveCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid categoryId, int year, int month, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void Add(Budget budget);
    void Remove(Budget budget);
}
