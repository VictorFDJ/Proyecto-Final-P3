namespace MiPresupuesto.Application.Reports;

public sealed record CategoryReportItem(
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal Total,
    decimal Percentage,
    int TransactionCount);

public sealed record DailyReportItem(DateOnly Date, decimal Total);

public sealed record MonthlyReportResponse(
    int Year,
    int Month,
    decimal TotalSpent,
    int TransactionCount,
    decimal AverageExpense,
    decimal PreviousMonthTotal,
    decimal DifferenceFromPreviousMonth,
    decimal? PercentageChange,
    string Trend,
    IReadOnlyList<CategoryReportItem> CategoryBreakdown,
    IReadOnlyList<CategoryReportItem> TopCategories,
    IReadOnlyList<DailyReportItem> DailyTotals);

public sealed record ReportFile(byte[] Content, string ContentType, string FileName);
