using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Budgets;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Persistence;

namespace MiPresupuesto.Tests.Budgets;

public sealed class BudgetServiceTests
{
    [Theory]
    [InlineData(49, 49, "normal", false)]
    [InlineData(50, 50, "warning", false)]
    [InlineData(80, 80, "critical", false)]
    [InlineData(100, 100, "limit_reached", false)]
    [InlineData(101, 101, "exceeded", true)]
    public async Task GetAllAsync_CalculatesAlertThresholds(
        int spent,
        int expectedPercentage,
        string expectedAlert,
        bool expectedExceeded)
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        db.Budgets.Add(Budget(setup, 100, 2026, 8));
        db.Expenses.Add(Expense(setup, spent, new DateOnly(2026, 8, 10)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var response = Assert.Single(await service.GetAllAsync(setup.UserId, 2026, 8));

        Assert.Equal(expectedPercentage, response.PercentageUsed);
        Assert.Equal(expectedAlert, response.AlertLevel);
        Assert.Equal(expectedExceeded, response.IsExceeded);
        Assert.Equal(100 - spent, response.Remaining);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCategoryAndPeriod()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var service = CreateService(db);
        var request = new CreateBudgetRequest(2026, 8, setup.Category.Id, 5_000);

        await service.CreateAsync(setup.UserId, request);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(setup.UserId, request));
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
            new CreateBudgetRequest(2026, 8, other.Category.Id, 2_000)));

        Assert.Contains("categoryId", exception.Errors!.Keys);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyRequestedUserAndPeriod()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Otra", "Otro");
        db.Budgets.AddRange(
            Budget(owner, 1_000, 2026, 8),
            Budget(owner, 2_000, 2026, 7),
            Budget(other, 9_000, 2026, 8));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var response = await service.GetAllAsync(owner.UserId, 2026, 8);

        var budget = Assert.Single(response);
        Assert.Equal(1_000, budget.Amount);
        Assert.Equal(owner.Category.Id, budget.CategoryId);
    }

    [Fact]
    public async Task GetExceededAsync_ReturnsOnlyBudgetsAboveOneHundredPercent()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var reachedCategory = new Category
        {
            UserId = setup.UserId,
            Name = "Hogar",
            Color = "#2563EB"
        };
        db.Categories.Add(reachedCategory);
        db.Budgets.AddRange(
            Budget(setup, 100, 2026, 8),
            new Budget
            {
                UserId = setup.UserId,
                CategoryId = reachedCategory.Id,
                Category = reachedCategory,
                Amount = 100,
                Year = 2026,
                Month = 8
            });
        db.Expenses.AddRange(
            Expense(setup, 120, new DateOnly(2026, 8, 1)),
            new Expense
            {
                UserId = setup.UserId,
                CategoryId = reachedCategory.Id,
                Category = reachedCategory,
                PaymentMethodId = setup.Method.Id,
                PaymentMethod = setup.Method,
                Amount = 100,
                Date = new DateOnly(2026, 8, 1)
            });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var response = await service.GetExceededAsync(setup.UserId, 2026, 8);

        var budget = Assert.Single(response);
        Assert.True(budget.IsExceeded);
        Assert.Equal(120, budget.PercentageUsed);
    }

    [Fact]
    public async Task UpdateAsync_CannotAccessAnotherUsersBudget()
    {
        await using var db = CreateContext();
        var owner = await AddResourcesAsync(db);
        var other = await AddResourcesAsync(db, "Otra", "Otro");
        var privateBudget = Budget(other, 1_000, 2026, 8);
        db.Budgets.Add(privateBudget);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            owner.UserId,
            privateBudget.Id,
            new UpdateBudgetRequest(2026, 8, owner.Category.Id, 500)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedBudget()
    {
        await using var db = CreateContext();
        var setup = await AddResourcesAsync(db);
        var budget = Budget(setup, 1_000, 2026, 8);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.DeleteAsync(setup.UserId, budget.Id);

        Assert.Empty(db.Budgets);
    }

    private static BudgetService CreateService(AppDbContext db)
        => new(new BudgetRepository(db), db);

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

    private static Budget Budget(ResourceSetup setup, decimal amount, int year, int month) => new()
    {
        UserId = setup.UserId,
        CategoryId = setup.Category.Id,
        Category = setup.Category,
        Amount = amount,
        Year = year,
        Month = month
    };

    private static Expense Expense(ResourceSetup setup, decimal amount, DateOnly date) => new()
    {
        UserId = setup.UserId,
        CategoryId = setup.Category.Id,
        Category = setup.Category,
        PaymentMethodId = setup.Method.Id,
        PaymentMethod = setup.Method,
        Amount = amount,
        Date = date
    };

    private sealed record ResourceSetup(Guid UserId, Category Category, PaymentMethod Method);
}
