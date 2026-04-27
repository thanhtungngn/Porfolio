namespace Portfolio.Api.Infrastructure.Extensions;

public sealed class ApiSecurityOptions
{
    public string JwtIssuer { get; init; } = "Portfolio.Api";
    public string JwtAudience { get; init; } = "Portfolio.Web";
    public string JwtSigningKey { get; init; } = string.Empty;
    public int JwtExpiresMinutes { get; init; } = 60;

    public string ResolveJwtSigningKey() =>
        string.IsNullOrWhiteSpace(JwtSigningKey)
            ? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? string.Empty
            : JwtSigningKey;
}
