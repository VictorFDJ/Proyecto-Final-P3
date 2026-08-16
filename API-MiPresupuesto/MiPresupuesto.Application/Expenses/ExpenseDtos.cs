namespace MiPresupuesto.Application.Expenses;

public sealed record CreateExpenseRequest(
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    Guid PaymentMethodId,
    string? Description);

public sealed record UpdateExpenseRequest(
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    Guid PaymentMethodId,
    string? Description);

public sealed class ExpenseQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record ExpenseResponse(
    Guid Id,
    decimal Amount,
    DateOnly Date,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    Guid PaymentMethodId,
    string PaymentMethodName,
    string? PaymentMethodIcon,
    DateTime CreatedAtUtc);
