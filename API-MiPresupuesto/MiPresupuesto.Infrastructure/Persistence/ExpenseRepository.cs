using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Expenses;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class ExpenseRepository(AppDbContext dbContext) : IExpenseRepository
{
    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetPageAsync(
        Guid userId,
        ExpenseQuery query,
        CancellationToken cancellationToken = default)
    {
        var expenses = dbContext.Expenses
            .AsNoTracking()
            .Include(expense => expense.Category)
            .Include(expense => expense.PaymentMethod)
            .Where(expense => expense.UserId == userId);

        if (query.FromDate.HasValue)
        {
            expenses = expenses.Where(expense => expense.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            expenses = expenses.Where(expense => expense.Date <= query.ToDate.Value);
        }

        if (query.CategoryId.HasValue)
        {
            expenses = expenses.Where(expense => expense.CategoryId == query.CategoryId.Value);
        }

        if (query.PaymentMethodId.HasValue)
        {
            expenses = expenses.Where(expense => expense.PaymentMethodId == query.PaymentMethodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            expenses = expenses.Where(expense =>
                expense.Description != null && expense.Description.Contains(query.Search));
        }

        var totalCount = await expenses.CountAsync(cancellationToken);
        var items = await expenses
            .OrderByDescending(expense => expense.Date)
            .ThenByDescending(expense => expense.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Expense?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => dbContext.Expenses
            .Include(expense => expense.Category)
            .Include(expense => expense.PaymentMethod)
            .SingleOrDefaultAsync(
                expense => expense.UserId == userId && expense.Id == id,
                cancellationToken);

    public Task<Category?> GetActiveCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => dbContext.Categories.SingleOrDefaultAsync(
            category => category.UserId == userId &&
                        category.Id == categoryId &&
                        category.IsActive,
            cancellationToken);

    public Task<PaymentMethod?> GetActivePaymentMethodAsync(
        Guid userId,
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
        => dbContext.PaymentMethods.SingleOrDefaultAsync(
            method => method.UserId == userId &&
                      method.Id == paymentMethodId &&
                      method.IsActive,
            cancellationToken);

    public void Add(Expense expense) => dbContext.Expenses.Add(expense);
    public void Remove(Expense expense) => dbContext.Expenses.Remove(expense);
}
