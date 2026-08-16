using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Imports;
using MiPresupuesto.Application.Reports;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Files;
using MiPresupuesto.Infrastructure.Persistence;

namespace MiPresupuesto.Tests.Reports;

public sealed class ReportAndImportTests
{
    [Fact]
    public async Task MonthlyReport_CalculatesTotalsComparisonBreakdownAndDailyData()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var secondCategory = new Category
        {
            UserId = setup.UserId,
            Name = "Transporte",
            Color = "#2563EB"
        };
        db.Categories.Add(secondCategory);
        db.Expenses.AddRange(
            Expense(setup, setup.Category, 100, new DateOnly(2026, 7, 15)),
            Expense(setup, setup.Category, 200, new DateOnly(2026, 8, 5)),
            Expense(setup, secondCategory, 100, new DateOnly(2026, 8, 10)));
        await db.SaveChangesAsync();
        var service = CreateReportService(db);

        var report = await service.GetMonthlyAsync(setup.UserId, 2026, 8);

        Assert.Equal(300, report.TotalSpent);
        Assert.Equal(2, report.TransactionCount);
        Assert.Equal(150, report.AverageExpense);
        Assert.Equal(100, report.PreviousMonthTotal);
        Assert.Equal(200, report.DifferenceFromPreviousMonth);
        Assert.Equal(200, report.PercentageChange);
        Assert.Equal("up", report.Trend);
        Assert.Equal(2, report.CategoryBreakdown.Count);
        Assert.Equal("Alimentación", report.TopCategories[0].CategoryName);
        Assert.Equal(31, report.DailyTotals.Count);
        Assert.Equal(200, report.DailyTotals.Single(day => day.Date.Day == 5).Total);
    }

    [Fact]
    public async Task MonthlyReport_DoesNotIncludeAnotherUsersExpenses()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Otro", "Tarjeta");
        db.Expenses.AddRange(
            Expense(owner, owner.Category, 50, new DateOnly(2026, 8, 1)),
            Expense(other, other.Category, 9_999, new DateOnly(2026, 8, 1)));
        await db.SaveChangesAsync();

        var report = await CreateReportService(db).GetMonthlyAsync(owner.UserId, 2026, 8);

        Assert.Equal(50, report.TotalSpent);
        Assert.Single(report.CategoryBreakdown);
    }

    [Fact]
    public async Task ReportExporter_GeneratesValidJsonTxtAndExcelFiles()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        db.Expenses.Add(Expense(setup, setup.Category, 125, new DateOnly(2026, 8, 1)));
        await db.SaveChangesAsync();
        var service = CreateReportService(db);

        var json = await service.ExportMonthlyAsync(setup.UserId, 2026, 8, "json");
        var txt = await service.ExportMonthlyAsync(setup.UserId, 2026, 8, "txt");
        var excel = await service.ExportMonthlyAsync(setup.UserId, 2026, 8, "xlsx");

        using var jsonDocument = JsonDocument.Parse(json.Content);
        Assert.Equal(125, jsonDocument.RootElement.GetProperty("totalSpent").GetDecimal());
        Assert.Contains("REPORTE MENSUAL", System.Text.Encoding.UTF8.GetString(txt.Content));
        using var workbook = new XLWorkbook(new MemoryStream(excel.Content));
        Assert.Contains(workbook.Worksheets, sheet => sheet.Name == "Resumen");
        Assert.Contains(workbook.Worksheets, sheet => sheet.Name == "Categorías");
        Assert.Contains(workbook.Worksheets, sheet => sheet.Name == "Datos diarios");
    }

    [Fact]
    public async Task Import_InsertsValidRowsAndReportsEveryInvalidRow()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        using var stream = CreateImportWorkbook(
            (DateTime.Today, 250m, "Alimentación", "Efectivo", "Válida"),
            (DateTime.Today, 0m, "Alimentación", "Efectivo", "Monto inválido"),
            (DateTime.Today, 100m, "No existe", "Efectivo", "Categoría inválida"));
        var service = CreateImportService(db);

        var result = await service.ImportAsync(setup.UserId, stream, "gastos.xlsx");

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ImportedRows);
        Assert.Equal(2, result.FailedRows);
        Assert.Equal([3, 4], result.Errors.Select(error => error.RowNumber));
        var expense = Assert.Single(db.Expenses);
        Assert.Equal(250, expense.Amount);
        Assert.Equal("Válida", expense.Description);
    }

    [Fact]
    public async Task Import_CannotResolveAnotherUsersResources()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Privada", "Privado");
        using var stream = CreateImportWorkbook(
            (DateTime.Today, 100m, other.Category.Name, other.Method.Name, "Intento"));

        var result = await CreateImportService(db).ImportAsync(owner.UserId, stream, "gastos.xlsx");

        Assert.Equal(0, result.ImportedRows);
        Assert.Equal(1, result.FailedRows);
        Assert.Empty(db.Expenses);
    }

    [Fact]
    public async Task Import_RejectsWrongExtension()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        using var stream = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateImportService(db).ImportAsync(setup.UserId, stream, "gastos.csv"));

        Assert.Contains("file", exception.Errors!.Keys);
    }

    [Fact]
    public void SpreadsheetTemplate_IsAValidWorkbookWithInstructions()
    {
        var file = new ExpenseSpreadsheet().CreateTemplate();

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal("plantilla-importacion-gastos.xlsx", file.FileName);
        Assert.Contains(workbook.Worksheets, sheet => sheet.Name == "Gastos");
        Assert.Contains(workbook.Worksheets, sheet => sheet.Name == "Instrucciones");
        Assert.Equal("Fecha", workbook.Worksheet("Gastos").Cell("A1").GetString());
    }

    private static ReportService CreateReportService(AppDbContext db)
        => new(new ReportRepository(db), new ReportExporter());

    private static ExpenseImportService CreateImportService(AppDbContext db)
        => new(new ExpenseSpreadsheet(), new ExpenseImportRepository(db), db);

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<ResourceSetup> AddResourcesAsync(
        AppDbContext db,
        string categoryName = "Alimentación",
        string methodName = "Efectivo")
    {
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = categoryName, Color = "#F97316" };
        var method = new PaymentMethod { UserId = userId, Name = methodName, Icon = "wallet" };
        db.Categories.Add(category);
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync();
        return new ResourceSetup(userId, category, method);
    }

    private static Expense Expense(ResourceSetup setup, Category category, decimal amount, DateOnly date) => new()
    {
        UserId = setup.UserId,
        CategoryId = category.Id,
        Category = category,
        PaymentMethodId = setup.Method.Id,
        PaymentMethod = setup.Method,
        Amount = amount,
        Date = date
    };

    private static MemoryStream CreateImportWorkbook(params (DateTime Date, decimal Amount, string Category, string Method, string Description)[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Gastos");
        string[] headers = ["Fecha", "Monto", "Categoría", "Método de pago", "Descripción"];
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var number = index + 2;
            sheet.Cell(number, 1).Value = row.Date;
            sheet.Cell(number, 2).Value = row.Amount;
            sheet.Cell(number, 3).Value = row.Category;
            sheet.Cell(number, 4).Value = row.Method;
            sheet.Cell(number, 5).Value = row.Description;
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed record ResourceSetup(Guid UserId, Category Category, PaymentMethod Method);
}
