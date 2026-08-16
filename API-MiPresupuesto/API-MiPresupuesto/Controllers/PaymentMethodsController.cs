using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.PaymentMethods;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payment-methods")]
public sealed class PaymentMethodsController(IPaymentMethodService paymentMethodService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await paymentMethodService.GetAllAsync(User.GetUserId(), includeInactive, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentMethodResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await paymentMethodService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PaymentMethodResponse>> Create(
        CreatePaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodService.CreateAsync(
            User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = paymentMethod.Id }, paymentMethod);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PaymentMethodResponse>> Update(
        Guid id,
        UpdatePaymentMethodRequest request,
        CancellationToken cancellationToken)
        => Ok(await paymentMethodService.UpdateAsync(
            User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await paymentMethodService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
