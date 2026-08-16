using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Reports;

namespace MiPresupuesto.Infrastructure.Files;

public sealed class ReportExporter : IReportExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ReportFile Export(MonthlyReportResponse report, string format) => format switch
    {
        "json" => Json(report),
        "txt" => Text(report),
        "xlsx" or "excel" => Excel(report),
        _ => throw new ValidationException(
            "Formato de exportación no válido.",
            new Dictionary<string, string[]> { ["format"] = ["Usa json, txt o xlsx."] })
    };

    private static ReportFile Json(MonthlyReportResponse report) => new(
        JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions),
        "application/json",
        FileName(report, "json"));

    private static ReportFile Text(MonthlyReportResponse report)
    {
        var text = new StringBuilder()
            .AppendLine("MI PRESUPUESTO - REPORTE MENSUAL")
            .AppendLine($"Período: {report.Month:00}/{report.Year}")
            .AppendLine(new string('=', 48))
            .AppendLine($"Total gastado: {Money(report.TotalSpent)}")
            .AppendLine($"Cantidad de gastos: {report.TransactionCount}")
            .AppendLine($"Gasto promedio: {Money(report.AverageExpense)}")
            .AppendLine($"Mes anterior: {Money(report.PreviousMonthTotal)}")
            .AppendLine($"Diferencia: {Money(report.DifferenceFromPreviousMonth)}")
            .AppendLine($"Variación: {(report.PercentageChange.HasValue ? $"{report.PercentageChange:0.##}%" : "Sin base de comparación")}")
            .AppendLine()
            .AppendLine("DESGLOSE POR CATEGORÍA")
            .AppendLine(new string('-', 48));

        foreach (var category in report.CategoryBreakdown)
        {
            text.AppendLine($"{category.CategoryName}: {Money(category.Total)} | {category.Percentage:0.##}% | {category.TransactionCount} gastos");
        }

        return new ReportFile(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray(),
            "text/plain; charset=utf-8",
            FileName(report, "txt"));
    }

    private static ReportFile Excel(MonthlyReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Resumen");
        summary.Cell("A1").Value = "MI PRESUPUESTO - REPORTE MENSUAL";
        summary.Range("A1:F1").Merge();
        summary.Range("A1:F1").Style.Font.SetBold().Font.SetFontSize(18)
            .Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#4F46E5"));
        summary.Cell("A3").Value = "Período";
        summary.Cell("B3").Value = $"{report.Month:00}/{report.Year}";
        summary.Cell("A4").Value = "Total gastado";
        summary.Cell("B4").Value = report.TotalSpent;
        summary.Cell("A5").Value = "Cantidad de gastos";
        summary.Cell("B5").Value = report.TransactionCount;
        summary.Cell("A6").Value = "Gasto promedio";
        summary.Cell("B6").Value = report.AverageExpense;
        summary.Cell("A7").Value = "Mes anterior";
        summary.Cell("B7").Value = report.PreviousMonthTotal;
        summary.Cell("A8").Value = "Diferencia";
        summary.Cell("B8").Value = report.DifferenceFromPreviousMonth;
        summary.Cell("A9").Value = "Variación";
        if (report.PercentageChange.HasValue) summary.Cell("B9").Value = report.PercentageChange.Value / 100;
        summary.Range("B4:B8").Style.NumberFormat.Format = "$#,##0.00";
        summary.Cell("B9").Style.NumberFormat.Format = "0.00%";
        summary.Columns("A:B").AdjustToContents();

        var categories = workbook.Worksheets.Add("Categorías");
        string[] headers = ["Categoría", "Total", "Porcentaje", "Cantidad", "Color"];
        for (var column = 0; column < headers.Length; column++) categories.Cell(1, column + 1).Value = headers[column];
        StyleHeader(categories.Range(1, 1, 1, headers.Length));
        for (var index = 0; index < report.CategoryBreakdown.Count; index++)
        {
            var item = report.CategoryBreakdown[index];
            var row = index + 2;
            categories.Cell(row, 1).Value = item.CategoryName;
            categories.Cell(row, 2).Value = item.Total;
            categories.Cell(row, 3).Value = item.Percentage / 100;
            categories.Cell(row, 4).Value = item.TransactionCount;
            categories.Cell(row, 5).Value = item.CategoryColor;
        }
        categories.Column(2).Style.NumberFormat.Format = "$#,##0.00";
        categories.Column(3).Style.NumberFormat.Format = "0.00%";
        categories.Columns().AdjustToContents();

        var daily = workbook.Worksheets.Add("Datos diarios");
        daily.Cell("A1").Value = "Fecha";
        daily.Cell("B1").Value = "Total";
        StyleHeader(daily.Range("A1:B1"));
        for (var index = 0; index < report.DailyTotals.Count; index++)
        {
            daily.Cell(index + 2, 1).Value = report.DailyTotals[index].Date.ToDateTime(TimeOnly.MinValue);
            daily.Cell(index + 2, 2).Value = report.DailyTotals[index].Total;
        }
        daily.Column(1).Style.DateFormat.Format = "dd/MM/yyyy";
        daily.Column(2).Style.NumberFormat.Format = "$#,##0.00";
        daily.Columns().AdjustToContents();
        daily.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ReportFile(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName(report, "xlsx"));
    }

    private static void StyleHeader(IXLRange range) => range.Style.Font.SetBold()
        .Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#4F46E5"));

    private static string FileName(MonthlyReportResponse report, string extension)
        => $"reporte-gastos-{report.Year}-{report.Month:00}.{extension}";

    private static string Money(decimal value)
        => value.ToString("C2", CultureInfo.GetCultureInfo("es-DO"));
}
