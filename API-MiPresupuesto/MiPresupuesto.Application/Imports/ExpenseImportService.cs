using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Reports;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Imports;

public sealed class ExpenseImportService(
    IExpenseSpreadsheet spreadsheet,
    IExpenseImportRepository repository,
    IUnitOfWork unitOfWork) : IExpenseImportService
{
    private const int MaxRows = 5_000;

    public async Task<ExpenseImportResponse> ImportAsync(
        Guid userId,
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Archivo no válido.",
                new Dictionary<string, string[]> { ["file"] = ["Solo se permiten archivos .xlsx."] });
        }

        var rows = spreadsheet.Read(stream);
        if (rows.Count == 0)
        {
            throw new ValidationException(
                "El archivo no contiene gastos.",
                new Dictionary<string, string[]> { ["file"] = ["Agrega al menos una fila de datos."] });
        }

        if (rows.Count > MaxRows)
        {
            throw new ValidationException(
                "El archivo supera el límite permitido.",
                new Dictionary<string, string[]> { ["file"] = [$"El máximo es de {MaxRows} filas."] });
        }

        var categories = (await repository.GetActiveCategoriesAsync(userId, cancellationToken))
            .ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
        var paymentMethods = (await repository.GetActivePaymentMethodsAsync(userId, cancellationToken))
            .ToDictionary(method => method.Name, StringComparer.OrdinalIgnoreCase);
        var errors = new List<ImportRowError>();
        var validExpenses = new List<Expense>();

        foreach (var row in rows)
        {
            var rowErrors = ValidateRow(row, categories, paymentMethods);
            if (rowErrors.Count > 0)
            {
                errors.Add(new ImportRowError(row.RowNumber, string.Join(" ", rowErrors)));
                continue;
            }

            var category = categories[row.CategoryName!.Trim()];
            var paymentMethod = paymentMethods[row.PaymentMethodName!.Trim()];
            validExpenses.Add(new Expense
            {
                UserId = userId,
                Date = row.Date!.Value,
                Amount = row.Amount!.Value,
                Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
                CategoryId = category.Id,
                Category = category,
                PaymentMethodId = paymentMethod.Id,
                PaymentMethod = paymentMethod
            });
        }

        if (validExpenses.Count > 0)
        {
            repository.AddRange(validExpenses);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ExpenseImportResponse(
            rows.Count,
            validExpenses.Count,
            errors.Count,
            errors);
    }

    public ReportFile GetTemplate() => spreadsheet.CreateTemplate();

    private static List<string> ValidateRow(
        ExpenseImportRow row,
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlyDictionary<string, PaymentMethod> paymentMethods)
    {
        var errors = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.ParseError)) errors.Add(row.ParseError);
        if (!row.Date.HasValue) errors.Add("La fecha es obligatoria.");
        else if (row.Date > DateOnly.FromDateTime(DateTime.Today)) errors.Add("La fecha no puede estar en el futuro.");
        if (!row.Amount.HasValue) errors.Add("El monto es obligatorio.");
        else if (row.Amount <= 0) errors.Add("El monto debe ser mayor que cero.");
        else if (decimal.Round(row.Amount.Value, 2) != row.Amount) errors.Add("El monto solo puede tener dos decimales.");

        var categoryName = row.CategoryName?.Trim();
        if (string.IsNullOrEmpty(categoryName)) errors.Add("La categoría es obligatoria.");
        else if (!categories.ContainsKey(categoryName)) errors.Add($"La categoría '{categoryName}' no existe o está inactiva.");

        var methodName = row.PaymentMethodName?.Trim();
        if (string.IsNullOrEmpty(methodName)) errors.Add("El método de pago es obligatorio.");
        else if (!paymentMethods.ContainsKey(methodName)) errors.Add($"El método de pago '{methodName}' no existe o está inactivo.");
        if (row.Description?.Trim().Length > 300) errors.Add("La descripción supera los 300 caracteres.");
        return errors;
    }
}
