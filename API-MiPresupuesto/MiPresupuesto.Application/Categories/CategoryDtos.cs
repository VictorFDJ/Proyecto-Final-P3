namespace MiPresupuesto.Application.Categories;

public sealed record CreateCategoryRequest(string Name, string? Color);
public sealed record UpdateCategoryRequest(string Name, string? Color, bool IsActive);
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Color,
    bool IsActive,
    DateTime CreatedAtUtc);
