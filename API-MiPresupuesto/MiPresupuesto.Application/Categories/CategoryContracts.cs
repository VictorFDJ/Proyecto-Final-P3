using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(Guid userId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(Guid userId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid userId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedRecordsAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Remove(Category category);
}
