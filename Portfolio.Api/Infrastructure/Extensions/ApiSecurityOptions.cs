namespace Portfolio.Api.Infrastructure.Extensions;

public sealed class ApiSecurityOptions
{
    public string JwtIssuer { get; init; } = "Portfolio.Api";
    public string JwtAudience { get; init; } = "Portfolio.Web";
    public string JwtSigningKey { get; init; } = string.Empty;
    public int JwtExpiresMinutes { get; init; } = 60;

    public string AdminUsername { get; init; } = "admin";
    public string AdminPassword { get; init; } = string.Empty;

    public string ResolveJwtSigningKey() =>
        string.IsNullOrWhiteSpace(JwtSigningKey)
            ? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? string.Empty
            : JwtSigningKey;

    public string ResolveAdminPassword() =>
        string.IsNullOrWhiteSpace(AdminPassword)
            ? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? string.Empty
            : AdminPassword;
}
