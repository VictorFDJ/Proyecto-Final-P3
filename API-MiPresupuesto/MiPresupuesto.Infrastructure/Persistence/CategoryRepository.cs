using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Categories;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId && (includeInactive || category.IsActive))
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => dbContext.Categories.SingleOrDefaultAsync(
            category => category.UserId == userId && category.Id == id,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
        => dbContext.Categories.AnyAsync(
            category => category.UserId == userId &&
                        category.Name == name &&
                        (!excludingId.HasValue || category.Id != excludingId.Value),
            cancellationToken);

    public async Task<bool> HasRelatedRecordsAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => await dbContext.Expenses.AnyAsync(
               expense => expense.UserId == userId && expense.CategoryId == id,
               cancellationToken) ||
           await dbContext.Budgets.AnyAsync(
               budget => budget.UserId == userId && budget.CategoryId == id,
               cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);
    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
