using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Expenses;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Persistence;

namespace MiPresupuesto.Tests.Expenses;

public sealed class ExpenseServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidData_PersistsOwnedExpense()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var service = CreateService(db);
        var date = DateOnly.FromDateTime(DateTime.Today);

        var response = await service.CreateAsync(setup.UserId, new CreateExpenseRequest(
            425.50m, date, setup.Category.Id, setup.Method.Id, "  Compra semanal  "));

        var stored = Assert.Single(db.Expenses);
        Assert.Equal(setup.UserId, stored.UserId);
        Assert.Equal(425.50m, response.Amount);
        Assert.Equal("Compra semanal", response.Description);
        Assert.Equal("Alimentación", response.CategoryName);
        Assert.Equal("Efectivo", response.PaymentMethodName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("1.234")]
    public async Task CreateAsync_WithInvalidAmount_ThrowsValidation(string amountText)
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            setup.UserId,
            new CreateExpenseRequest(
                decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture),
                DateOnly.FromDateTime(DateTime.Today),
                setup.Category.Id,
                setup.Method.Id,
                null)));

        Assert.Contains("amount", exception.Errors!.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithFutureDate_ThrowsValidation()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            setup.UserId,
            new CreateExpenseRequest(
                100,
                DateOnly.FromDateTime(DateTime.Today).AddDays(1),
                setup.Category.Id,
                setup.Method.Id,
                null)));

        Assert.Contains("date", exception.Errors!.Keys);
    }

    [Fact]
    public async Task CreateAsync_CannotUseAnotherUsersCategory()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Transporte", "Tarjeta");
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            owner.UserId,
            new CreateExpenseRequest(
                100,
                DateOnly.FromDateTime(DateTime.Today),
                other.Category.Id,
                owner.Method.Id,
                null)));

        Assert.Contains("categoryId", exception.Errors!.Keys);
    }

    [Fact]
    public async Task GetAllAsync_AppliesOwnerCategoryDateAndSearchFilters()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Otra", "Otro");
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Expenses.AddRange(
            Expense(owner, 100, today.AddDays(-2), "Mercado Nacional"),
            Expense(owner, 250, today.AddDays(-20), "Mercado antiguo"),
            Expense(other, 999, today.AddDays(-2), "Mercado ajeno"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetAllAsync(owner.UserId, new ExpenseQuery
        {
            FromDate = today.AddDays(-7),
            ToDate = today,
            CategoryId = owner.Category.Id,
            Search = "Nacional",
            Page = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(100, item.Amount);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectPaginationMetadata()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Expenses.AddRange(
            Expense(owner, 10, today, "Uno"),
            Expense(owner, 20, today.AddDays(-1), "Dos"),
            Expense(owner, 30, today.AddDays(-2), "Tres"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetAllAsync(owner.UserId, new ExpenseQuery
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task UpdateAsync_CannotAccessAnotherUsersExpense()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Otra", "Otro");
        var expense = Expense(other, 500, DateOnly.FromDateTime(DateTime.Today), "Privado");
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            owner.UserId,
            expense.Id,
            new UpdateExpenseRequest(
                1,
                DateOnly.FromDateTime(DateTime.Today),
                owner.Category.Id,
                owner.Method.Id,
                "Intento")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedExpense()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var expense = Expense(owner, 50, DateOnly.FromDateTime(DateTime.Today), null);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.DeleteAsync(owner.UserId, expense.Id);

        Assert.Empty(db.Expenses);
    }

    private static ExpenseService CreateService(AppDbContext db)
        => new(new ExpenseRepository(db), db);

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
        var category = new Category
        {
            UserId = userId,
            Name = categoryName,
            Color = "#F97316"
        };
        var method = new PaymentMethod
        {
            UserId = userId,
            Name = methodName,
            Icon = "wallet"
        };
        db.Categories.Add(category);
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync();
        return new ResourceSetup(userId, category, method);
    }

    private static Expense Expense(
        ResourceSetup setup,
        decimal amount,
        DateOnly date,
        string? description) => new()
    {
        UserId = setup.UserId,
        Amount = amount,
        Date = date,
        Description = description,
        CategoryId = setup.Category.Id,
        Category = setup.Category,
        PaymentMethodId = setup.Method.Id,
        PaymentMethod = setup.Method
    };

    private sealed record ResourceSetup(Guid UserId, Category Category, PaymentMethod Method);
}
