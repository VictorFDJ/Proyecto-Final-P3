using System.Diagnostics.CodeAnalysis;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Budgets;

public sealed class BudgetService(
    IBudgetRepository budgets,
    IUnitOfWork unitOfWork) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetResponse>> GetAllAsync(
        Guid userId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var period = NormalizePeriod(year, month);
        return (await budgets.GetAllWithSpentAsync(userId, period.Year, period.Month, cancellationToken))
            .Select(item => Map(item.Budget, item.Spent))
            .ToArray();
    }

    public async Task<IReadOnlyList<BudgetResponse>> GetExceededAsync(
        Guid userId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
        => (await GetAllAsync(userId, year, month, cancellationToken))
            .Where(budget => budget.IsExceeded)
            .OrderByDescending(budget => budget.PercentageUsed)
            .ToArray();

    public async Task<BudgetResponse> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var budget = await GetOwnedAsync(userId, id, cancellationToken);
        var spent = await budgets.GetSpentAsync(
            userId, budget.CategoryId, budget.Year, budget.Month, cancellationToken);
        return Map(budget, spent);
    }

    public async Task<BudgetResponse> CreateAsync(
        Guid userId,
        CreateBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateValues(request.Year, request.Month, request.Amount);
        var category = await GetCategoryAsync(userId, request.CategoryId, cancellationToken);
        if (await budgets.ExistsAsync(
                userId, category.Id, request.Year, request.Month,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe un presupuesto para esta categoría y mes.");
        }

        var budget = new Budget
        {
            UserId = userId,
            Year = request.Year,
            Month = request.Month,
            CategoryId = category.Id,
            Category = category,
            Amount = request.Amount
        };

        budgets.Add(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var spent = await budgets.GetSpentAsync(
            userId, category.Id, request.Year, request.Month, cancellationToken);
        return Map(budget, spent);
    }

    public async Task<BudgetResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var budget = await GetOwnedAsync(userId, id, cancellationToken);
        ValidateValues(request.Year, request.Month, request.Amount);
        var category = await GetCategoryAsync(userId, request.CategoryId, cancellationToken);
        if (await budgets.ExistsAsync(
                userId, category.Id, request.Year, request.Month, id, cancellationToken))
        {
            throw new ConflictException("Ya existe un presupuesto para esta categoría y mes.");
        }

        budget.Year = request.Year;
        budget.Month = request.Month;
        budget.CategoryId = category.Id;
        budget.Category = category;
        budget.Amount = request.Amount;
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var spent = await budgets.GetSpentAsync(
            userId, category.Id, request.Year, request.Month, cancellationToken);
        return Map(budget, spent);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var budget = await GetOwnedAsync(userId, id, cancellationToken);
        budgets.Remove(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Budget> GetOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => await budgets.GetByIdAsync(userId, id, cancellationToken)
           ?? throw new NotFoundException("El presupuesto no existe.");

    private async Task<Category> GetCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            FieldError("categoryId", "Selecciona una categoría.");
        }

        return await budgets.GetActiveCategoryAsync(userId, categoryId, cancellationToken)
               ?? throw new ValidationException(
                   "Revisa los datos enviados.",
                   new Dictionary<string, string[]>
                   {
                       ["categoryId"] = ["La categoría no existe, está inactiva o no pertenece al usuario."]
                   });
    }

    private static (int Year, int Month) NormalizePeriod(int? year, int? month)
    {
        var today = DateTime.Today;
        var normalizedYear = year ?? today.Year;
        var normalizedMonth = month ?? today.Month;
        ValidatePeriod(normalizedYear, normalizedMonth);
        return (normalizedYear, normalizedMonth);
    }

    private static void ValidateValues(int year, int month, decimal amount)
    {
        ValidatePeriod(year, month);
        if (amount <= 0)
        {
            FieldError("amount", "El presupuesto debe ser mayor que cero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            FieldError("amount", "El presupuesto solo puede tener dos decimales.");
        }
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 2100)
        {
            FieldError("year", "El año debe estar entre 2000 y 2100.");
        }

        if (month is < 1 or > 12)
        {
            FieldError("month", "El mes debe estar entre 1 y 12.");
        }
    }

    private static BudgetResponse Map(Budget budget, decimal spent)
    {
        var percentage = budget.Amount == 0
            ? 0
            : Math.Round(spent / budget.Amount * 100, 2);
        var alertLevel = percentage switch
        {
            > 100 => "exceeded",
            >= 100 => "limit_reached",
            >= 80 => "critical",
            >= 50 => "warning",
            _ => "normal"
        };

        return new BudgetResponse(
            budget.Id,
            budget.Year,
            budget.Month,
            budget.Amount,
            spent,
            budget.Amount - spent,
            percentage,
            alertLevel,
            spent > budget.Amount,
            budget.CategoryId,
            budget.Category.Name,
            budget.Category.Color ?? "#6366F1",
            budget.CreatedAtUtc);
    }

    [DoesNotReturn]
    private static void FieldError(string field, string message)
        => throw new ValidationException(
            "Revisa los datos enviados.",
            new Dictionary<string, string[]> { [field] = [message] });
}
