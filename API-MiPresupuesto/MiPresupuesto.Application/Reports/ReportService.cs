using System.Diagnostics.CodeAnalysis;
using MiPresupuesto.Application.Common.Exceptions;

namespace MiPresupuesto.Application.Reports;

public sealed class ReportService(
    IReportRepository reports,
    IReportExporter exporter) : IReportService
{
    public async Task<MonthlyReportResponse> GetMonthlyAsync(
        Guid userId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var period = NormalizePeriod(year, month);
        var start = new DateOnly(period.Year, period.Month, 1);
        var end = start.AddMonths(1);
        var previousStart = start.AddMonths(-1);

        var current = await reports.GetExpensesAsync(userId, start, end, cancellationToken);
        var previous = await reports.GetExpensesAsync(userId, previousStart, start, cancellationToken);
        var total = current.Sum(expense => expense.Amount);
        var previousTotal = previous.Sum(expense => expense.Amount);
        var difference = total - previousTotal;
        decimal? percentageChange = previousTotal == 0
            ? null
            : Math.Round(difference / previousTotal * 100, 2);

        var breakdown = current
            .GroupBy(expense => new
            {
                expense.CategoryId,
                expense.CategoryName,
                expense.CategoryColor
            })
            .Select(group => new CategoryReportItem(
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Key.CategoryColor,
                group.Sum(expense => expense.Amount),
                total == 0 ? 0 : Math.Round(group.Sum(expense => expense.Amount) / total * 100, 2),
                group.Count()))
            .OrderByDescending(category => category.Total)
            .ToArray();

        var byDay = current
            .GroupBy(expense => expense.Date.Day)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount));
        var dailyTotals = Enumerable.Range(1, DateTime.DaysInMonth(period.Year, period.Month))
            .Select(day => new DailyReportItem(
                new DateOnly(period.Year, period.Month, day),
                byDay.GetValueOrDefault(day)))
            .ToArray();

        return new MonthlyReportResponse(
            period.Year,
            period.Month,
            total,
            current.Count,
            current.Count == 0 ? 0 : Math.Round(total / current.Count, 2),
            previousTotal,
            difference,
            percentageChange,
            difference switch { > 0 => "up", < 0 => "down", _ => "same" },
            breakdown,
            breakdown.Take(5).ToArray(),
            dailyTotals);
    }

    public async Task<ReportFile> ExportMonthlyAsync(
        Guid userId,
        int? year,
        int? month,
        string format,
        CancellationToken cancellationToken = default)
        => exporter.Export(
            await GetMonthlyAsync(userId, year, month, cancellationToken),
            format?.Trim().ToLowerInvariant() ?? string.Empty);

    private static (int Year, int Month) NormalizePeriod(int? year, int? month)
    {
        var today = DateTime.Today;
        var normalizedYear = year ?? today.Year;
        var normalizedMonth = month ?? today.Month;
        if (normalizedYear is < 2000 or > 2100)
        {
            FieldError("year", "El año debe estar entre 2000 y 2100.");
        }

        if (normalizedMonth is < 1 or > 12)
        {
            FieldError("month", "El mes debe estar entre 1 y 12.");
        }

        return (normalizedYear, normalizedMonth);
    }

    [DoesNotReturn]
    private static void FieldError(string field, string message)
        => throw new ValidationException(
            "Revisa los datos enviados.",
            new Dictionary<string, string[]> { [field] = [message] });
}
