namespace MiPresupuesto.Application.Profile;

public sealed record UpdateNameRequest(string Name);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
