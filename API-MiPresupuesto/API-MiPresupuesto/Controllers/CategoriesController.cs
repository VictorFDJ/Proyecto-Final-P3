using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Categories;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await categoryService.GetAllAsync(User.GetUserId(), includeInactive, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await categoryService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await categoryService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
