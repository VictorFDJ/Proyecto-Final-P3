namespace MiPresupuesto.Application.Reports;

public sealed record ReportExpense(
    DateOnly Date,
    decimal Amount,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor);

public interface IReportService
{
    Task<MonthlyReportResponse> GetMonthlyAsync(Guid userId, int? year, int? month, CancellationToken cancellationToken = default);
    Task<ReportFile> ExportMonthlyAsync(Guid userId, int? year, int? month, string format, CancellationToken cancellationToken = default);
}

public interface IReportRepository
{
    Task<IReadOnlyList<ReportExpense>> GetExpensesAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDateExclusive,
        CancellationToken cancellationToken = default);
}

public interface IReportExporter
{
    ReportFile Export(MonthlyReportResponse report, string format);
}
