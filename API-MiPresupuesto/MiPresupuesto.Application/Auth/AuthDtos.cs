namespace MiPresupuesto.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
public sealed record UserResponse(Guid Id, string Name, string Email);
