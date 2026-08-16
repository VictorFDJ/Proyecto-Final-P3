using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Common.Models;
using MiPresupuesto.Application.Expenses;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> GetAll(
        [FromQuery] ExpenseQuery query,
        CancellationToken cancellationToken)
        => Ok(await expenseService.GetAllAsync(User.GetUserId(), query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await expenseService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var expense = await expenseService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken)
        => Ok(await expenseService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await expenseService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
