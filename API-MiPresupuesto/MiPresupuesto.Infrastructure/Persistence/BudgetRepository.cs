using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Budgets;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class BudgetRepository(AppDbContext dbContext) : IBudgetRepository
{
    public async Task<IReadOnlyList<BudgetWithSpent>> GetAllWithSpentAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId &&
                             budget.Year == year &&
                             budget.Month == month)
            .OrderBy(budget => budget.Category.Name)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return [];
        }

        var (start, end) = GetDateRange(year, month);
        var spentByCategory = await dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId &&
                              expense.Date >= start &&
                              expense.Date < end)
            .GroupBy(expense => expense.CategoryId)
            .Select(group => new { CategoryId = group.Key, Spent = group.Sum(expense => expense.Amount) })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Spent, cancellationToken);

        return budgets
            .Select(budget => new BudgetWithSpent(
                budget,
                spentByCategory.GetValueOrDefault(budget.CategoryId)))
            .ToArray();
    }

    public Task<Budget?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => dbContext.Budgets
            .Include(budget => budget.Category)
            .SingleOrDefaultAsync(
                budget => budget.UserId == userId && budget.Id == id,
                cancellationToken);

    public async Task<decimal> GetSpentAsync(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = GetDateRange(year, month);
        return await dbContext.Expenses
            .Where(expense => expense.UserId == userId &&
                              expense.CategoryId == categoryId &&
                              expense.Date >= start &&
                              expense.Date < end)
            .SumAsync(expense => (decimal?)expense.Amount, cancellationToken) ?? 0;
    }

    public Task<Category?> GetActiveCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => dbContext.Categories.SingleOrDefaultAsync(
            category => category.UserId == userId &&
                        category.Id == categoryId &&
                        category.IsActive,
            cancellationToken);

    public Task<bool> ExistsAsync(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
        => dbContext.Budgets.AnyAsync(
            budget => budget.UserId == userId &&
                      budget.CategoryId == categoryId &&
                      budget.Year == year &&
                      budget.Month == month &&
                      (!excludingId.HasValue || budget.Id != excludingId.Value),
            cancellationToken);

    public void Add(Budget budget) => dbContext.Budgets.Add(budget);
    public void Remove(Budget budget) => dbContext.Budgets.Remove(budget);

    private static (DateOnly Start, DateOnly End) GetDateRange(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1));
    }
}
