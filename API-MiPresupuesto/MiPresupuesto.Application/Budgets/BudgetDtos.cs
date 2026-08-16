namespace MiPresupuesto.Application.Budgets;

public sealed record CreateBudgetRequest(
    int Year,
    int Month,
    Guid CategoryId,
    decimal Amount);

public sealed record UpdateBudgetRequest(
    int Year,
    int Month,
    Guid CategoryId,
    decimal Amount);

public sealed record BudgetResponse(
    Guid Id,
    int Year,
    int Month,
    decimal Amount,
    decimal Spent,
    decimal Remaining,
    decimal PercentageUsed,
    string AlertLevel,
    bool IsExceeded,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    DateTime CreatedAtUtc);
