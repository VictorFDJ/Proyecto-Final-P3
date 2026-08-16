using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Common.Validation;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Categories;

public sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
        => (await categories.GetAllAsync(userId, includeInactive, cancellationToken))
            .Select(Map)
            .ToArray();

    public async Task<CategoryResponse> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        => Map(await GetOwnedAsync(userId, id, cancellationToken));

    public async Task<CategoryResponse> CreateAsync(
        Guid userId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = InputValidator.Required(request.Name, "name", 80);
        if (await categories.NameExistsAsync(userId, name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe una categoría con este nombre.");
        }

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Color = InputValidator.HexColor(request.Color)
        };

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await GetOwnedAsync(userId, id, cancellationToken);
        var name = InputValidator.Required(request.Name, "name", 80);
        if (await categories.NameExistsAsync(userId, name, id, cancellationToken))
        {
            throw new ConflictException("Ya existe una categoría con este nombre.");
        }

        category.Name = name;
        category.Color = InputValidator.HexColor(request.Color);
        category.IsActive = request.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await GetOwnedAsync(userId, id, cancellationToken);
        if (await categories.HasRelatedRecordsAsync(userId, id, cancellationToken))
        {
            throw new ConflictException(
                "No se puede eliminar la categoría porque tiene gastos o presupuestos asociados. Puedes desactivarla.");
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => await categories.GetByIdAsync(userId, id, cancellationToken)
           ?? throw new NotFoundException("La categoría no existe.");

    private static CategoryResponse Map(Category category) => new(
        category.Id,
        category.Name,
        category.Color ?? "#6366F1",
        category.IsActive,
        category.CreatedAtUtc);
}
