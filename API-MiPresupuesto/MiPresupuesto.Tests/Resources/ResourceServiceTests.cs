using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Categories;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.PaymentMethods;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Persistence;

namespace MiPresupuesto.Tests.Resources;

public sealed class ResourceServiceTests
{
    [Fact]
    public async Task Categories_GetAll_ReturnsOnlyActiveCategoriesOwnedByUser()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        db.Categories.AddRange(
            Category(ownerId, "Alimentación", true),
            Category(ownerId, "Inactiva", false),
            Category(Guid.NewGuid(), "Categoría ajena", true));
        await db.SaveChangesAsync();
        var service = new CategoryService(new CategoryRepository(db), db);

        var result = await service.GetAllAsync(ownerId, includeInactive: false);

        var category = Assert.Single(result);
        Assert.Equal("Alimentación", category.Name);
    }

    [Fact]
    public async Task Categories_Create_RejectsDuplicateForSameUserButAllowsOtherUser()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        db.Categories.Add(Category(ownerId, "Transporte"));
        await db.SaveChangesAsync();
        var service = new CategoryService(new CategoryRepository(db), db);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            ownerId,
            new CreateCategoryRequest("Transporte", "#2563EB")));

        var otherUserCategory = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateCategoryRequest("Transporte", "#2563EB"));
        Assert.Equal("Transporte", otherUserCategory.Name);
    }

    [Fact]
    public async Task Categories_Update_CannotAccessAnotherUsersCategory()
    {
        await using var db = CreateContext();
        var category = Category(Guid.NewGuid(), "Privada");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = new CategoryService(new CategoryRepository(db), db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            category.Id,
            new UpdateCategoryRequest("Intento", "#000000", true)));
    }

    [Fact]
    public async Task Categories_Create_RejectsInvalidColor()
    {
        await using var db = CreateContext();
        var service = new CategoryService(new CategoryRepository(db), db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            Guid.NewGuid(),
            new CreateCategoryRequest("Hogar", "rojo")));

        Assert.Contains("color", exception.Errors!.Keys);
    }

    [Fact]
    public async Task Categories_Delete_RejectsCategoryWithExpenses()
    {
        await using var db = CreateContext();
        var userId = Guid.NewGuid();
        var category = Category(userId, "Comida");
        var method = PaymentMethod(userId, "Efectivo");
        db.Categories.Add(category);
        db.PaymentMethods.Add(method);
        db.Expenses.Add(new Expense
        {
            UserId = userId,
            CategoryId = category.Id,
            PaymentMethodId = method.Id,
            Amount = 100,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });
        await db.SaveChangesAsync();
        var service = new CategoryService(new CategoryRepository(db), db);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(userId, category.Id));
    }

    [Fact]
    public async Task PaymentMethods_GetAll_ReturnsOnlyActiveMethodsOwnedByUser()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        db.PaymentMethods.AddRange(
            PaymentMethod(ownerId, "Efectivo", true),
            PaymentMethod(ownerId, "Viejo", false),
            PaymentMethod(Guid.NewGuid(), "Método ajeno", true));
        await db.SaveChangesAsync();
        var service = new PaymentMethodService(new PaymentMethodRepository(db), db);

        var result = await service.GetAllAsync(ownerId, includeInactive: false);

        var method = Assert.Single(result);
        Assert.Equal("Efectivo", method.Name);
    }

    [Fact]
    public async Task PaymentMethods_Update_CannotAccessAnotherUsersMethod()
    {
        await using var db = CreateContext();
        var method = PaymentMethod(Guid.NewGuid(), "Privado");
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync();
        var service = new PaymentMethodService(new PaymentMethodRepository(db), db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            method.Id,
            new UpdatePaymentMethodRequest("Intento", "card", true)));
    }

    [Fact]
    public async Task PaymentMethods_Delete_RejectsMethodWithExpenses()
    {
        await using var db = CreateContext();
        var userId = Guid.NewGuid();
        var category = Category(userId, "Comida");
        var method = PaymentMethod(userId, "Tarjeta");
        db.Categories.Add(category);
        db.PaymentMethods.Add(method);
        db.Expenses.Add(new Expense
        {
            UserId = userId,
            CategoryId = category.Id,
            PaymentMethodId = method.Id,
            Amount = 250,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });
        await db.SaveChangesAsync();
        var service = new PaymentMethodService(new PaymentMethodRepository(db), db);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(userId, method.Id));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Category Category(Guid userId, string name, bool isActive = true) => new()
    {
        UserId = userId,
        Name = name,
        Color = "#6366F1",
        IsActive = isActive
    };

    private static PaymentMethod PaymentMethod(Guid userId, string name, bool isActive = true) => new()
    {
        UserId = userId,
        Name = name,
        Icon = "wallet",
        IsActive = isActive
    };
}
