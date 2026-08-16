using MiPresupuesto.Application.Reports;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Imports;

public interface IExpenseImportService
{
    Task<ExpenseImportResponse> ImportAsync(Guid userId, Stream stream, string fileName, CancellationToken cancellationToken = default);
    ReportFile GetTemplate();
}

public interface IExpenseSpreadsheet
{
    IReadOnlyList<ExpenseImportRow> Read(Stream stream);
    ReportFile CreateTemplate();
}

public interface IExpenseImportRepository
{
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethod>> GetActivePaymentMethodsAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<Expense> expenses);
}
