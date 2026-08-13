namespace MiPresupuesto.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public required string Key { get; init; }
    public string Issuer { get; init; } = "MiPresupuesto.Api";
    public string Audience { get; init; } = "MiPresupuesto.Client";
    public int ExpirationMinutes { get; init; } = 120;
}
