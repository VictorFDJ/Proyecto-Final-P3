using System.Diagnostics.CodeAnalysis;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Common.Models;
using MiPresupuesto.Application.Common.Validation;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Expenses;

public sealed class ExpenseService(
    IExpenseRepository expenses,
    IUnitOfWork unitOfWork) : IExpenseService
{
    public async Task<PagedResponse<ExpenseResponse>> GetAllAsync(
        Guid userId,
        ExpenseQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeQuery(query);
        var result = await expenses.GetPageAsync(userId, normalized, cancellationToken);
        return new PagedResponse<ExpenseResponse>(
            result.Items.Select(Map).ToArray(),
            normalized.Page,
            normalized.PageSize,
            result.TotalCount,
            (int)Math.Ceiling(result.TotalCount / (double)normalized.PageSize));
    }

    public async Task<ExpenseResponse> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => Map(await GetOwnedAsync(userId, id, cancellationToken));

    public async Task<ExpenseResponse> CreateAsync(
        Guid userId,
        CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var description = ValidateValues(request.Amount, request.Date, request.Description);
        var (category, paymentMethod) = await GetReferencesAsync(
            userId, request.CategoryId, request.PaymentMethodId, cancellationToken);

        var expense = new Expense
        {
            UserId = userId,
            Amount = request.Amount,
            Date = request.Date,
            Description = description,
            CategoryId = category.Id,
            Category = category,
            PaymentMethodId = paymentMethod.Id,
            PaymentMethod = paymentMethod
        };

        expenses.Add(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var expense = await GetOwnedAsync(userId, id, cancellationToken);
        var description = ValidateValues(request.Amount, request.Date, request.Description);
        var (category, paymentMethod) = await GetReferencesAsync(
            userId, request.CategoryId, request.PaymentMethodId, cancellationToken);

        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.Description = description;
        expense.CategoryId = category.Id;
        expense.Category = category;
        expense.PaymentMethodId = paymentMethod.Id;
        expense.PaymentMethod = paymentMethod;
        expense.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(expense);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var expense = await GetOwnedAsync(userId, id, cancellationToken);
        expenses.Remove(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Expense> GetOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => await expenses.GetByIdAsync(userId, id, cancellationToken)
           ?? throw new NotFoundException("El gasto no existe.");

    private async Task<(Category Category, PaymentMethod PaymentMethod)> GetReferencesAsync(
        Guid userId,
        Guid categoryId,
        Guid paymentMethodId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            FieldError("categoryId", "Selecciona una categoría.");
        }

        if (paymentMethodId == Guid.Empty)
        {
            FieldError("paymentMethodId", "Selecciona un método de pago.");
        }

        var category = await expenses.GetActiveCategoryAsync(userId, categoryId, cancellationToken);
        if (category is null)
        {
            FieldError("categoryId", "La categoría no existe, está inactiva o no pertenece al usuario.");
        }

        var paymentMethod = await expenses.GetActivePaymentMethodAsync(userId, paymentMethodId, cancellationToken);
        if (paymentMethod is null)
        {
            FieldError("paymentMethodId", "El método de pago no existe, está inactivo o no pertenece al usuario.");
        }

        return (category, paymentMethod);
    }

    private static string? ValidateValues(decimal amount, DateOnly date, string? description)
    {
        if (amount <= 0)
        {
            FieldError("amount", "El monto debe ser mayor que cero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            FieldError("amount", "El monto solo puede tener dos decimales.");
        }

        if (date == default)
        {
            FieldError("date", "Selecciona una fecha válida.");
        }

        if (date > DateOnly.FromDateTime(DateTime.Today))
        {
            FieldError("date", "La fecha del gasto no puede estar en el futuro.");
        }

        return InputValidator.Optional(description, "description", 300);
    }

    private static ExpenseQuery NormalizeQuery(ExpenseQuery? query)
    {
        query ??= new ExpenseQuery();
        if (query.Page < 1)
        {
            FieldError("page", "La página debe ser mayor o igual a 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            FieldError("pageSize", "El tamaño de página debe estar entre 1 y 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            FieldError("fromDate", "La fecha inicial no puede ser posterior a la fecha final.");
        }

        return new ExpenseQuery
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            CategoryId = query.CategoryId,
            PaymentMethodId = query.PaymentMethodId,
            Search = InputValidator.Optional(query.Search, "search", 100),
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    private static ExpenseResponse Map(Expense expense) => new(
        expense.Id,
        expense.Amount,
        expense.Date,
        expense.Description,
        expense.CategoryId,
        expense.Category.Name,
        expense.Category.Color ?? "#6366F1",
        expense.PaymentMethodId,
        expense.PaymentMethod.Name,
        expense.PaymentMethod.Icon,
        expense.CreatedAtUtc);

    [DoesNotReturn]
    private static void FieldError(string field, string message)
        => throw new ValidationException(
            "Revisa los datos enviados.",
            new Dictionary<string, string[]> { [field] = [message] });
}
