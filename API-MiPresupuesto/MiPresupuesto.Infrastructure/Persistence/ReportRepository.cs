using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Reports;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class ReportRepository(AppDbContext dbContext) : IReportRepository
{
    public async Task<IReadOnlyList<ReportExpense>> GetExpensesAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDateExclusive,
        CancellationToken cancellationToken = default)
        => await dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId &&
                              expense.Date >= fromDate &&
                              expense.Date < toDateExclusive)
            .Select(expense => new ReportExpense(
                expense.Date,
                expense.Amount,
                expense.CategoryId,
                expense.Category.Name,
                expense.Category.Color ?? "#6366F1"))
            .ToListAsync(cancellationToken);
}
