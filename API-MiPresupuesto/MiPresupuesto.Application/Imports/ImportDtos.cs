namespace MiPresupuesto.Application.Imports;

public sealed record ExpenseImportRow(
    int RowNumber,
    DateOnly? Date,
    decimal? Amount,
    string? CategoryName,
    string? PaymentMethodName,
    string? Description,
    string? ParseError);

public sealed record ImportRowError(int RowNumber, string Message);

public sealed record ExpenseImportResponse(
    int TotalRows,
    int ImportedRows,
    int FailedRows,
    IReadOnlyList<ImportRowError> Errors);
