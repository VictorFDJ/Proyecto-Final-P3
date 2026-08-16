using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Budgets;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public sealed class BudgetsController(IBudgetService budgetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
        => Ok(await budgetService.GetAllAsync(User.GetUserId(), year, month, cancellationToken));

    [HttpGet("exceeded")]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> GetExceeded(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
        => Ok(await budgetService.GetExceededAsync(User.GetUserId(), year, month, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await budgetService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> Create(
        CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var budget = await budgetService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetResponse>> Update(
        Guid id,
        UpdateBudgetRequest request,
        CancellationToken cancellationToken)
        => Ok(await budgetService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await budgetService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
