namespace MiPresupuesto.Application.PaymentMethods;

public sealed record CreatePaymentMethodRequest(string Name, string? Icon);
public sealed record UpdatePaymentMethodRequest(string Name, string? Icon, bool IsActive);
public sealed record PaymentMethodResponse(
    Guid Id,
    string Name,
    string? Icon,
    bool IsActive,
    DateTime CreatedAtUtc);
