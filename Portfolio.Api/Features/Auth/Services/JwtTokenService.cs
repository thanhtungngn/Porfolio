using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Infrastructure.Extensions;
using Portfolio.Api.Infrastructure.Persistence.Entities;

namespace Portfolio.Api.Features.Auth.Services;

public sealed class JwtTokenService(IOptions<ApiSecurityOptions> securityOptions)
{
    private readonly ApiSecurityOptions _securityOptions = securityOptions.Value;

    public DateTime GetExpiryUtc() => DateTime.UtcNow.AddMinutes(_securityOptions.JwtExpiresMinutes);

    public string CreateToken(UserAccount user)
    {
        var signingKey = _securityOptions.ResolveJwtSigningKey();
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = GetExpiryUtc();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            ]),
            Expires = expiresAtUtc,
            Issuer = _securityOptions.JwtIssuer,
            Audience = _securityOptions.JwtAudience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}