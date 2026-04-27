using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Auth.Contracts;
using Portfolio.Api.Features.Auth.Services;
using Portfolio.Api.Infrastructure.Extensions;

namespace Portfolio.Api.Features.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", (
            LoginRequest request,
            IOptions<ApiSecurityOptions> securityOptions,
            JwtTokenService jwtTokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Username and password are required." });
            }

            var options = securityOptions.Value;
            var adminPassword = options.ResolveAdminPassword();

            // Constant-time comparison to prevent timing attacks
            var usernameMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(request.Username.Trim()),
                Encoding.UTF8.GetBytes(options.AdminUsername));

            var passwordMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(request.Password),
                Encoding.UTF8.GetBytes(adminPassword));

            if (!usernameMatch || !passwordMatch)
            {
                return Results.Unauthorized();
            }

            var expiresAtUtc = jwtTokenService.GetExpiryUtc();
            var token = jwtTokenService.CreateToken(request.Username.Trim(), "Admin");
            return Results.Ok(new AuthResponse(token, request.Username.Trim(), expiresAtUtc));
        });

        endpoints.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var username = httpContext.User.Identity?.Name ?? string.Empty;
                return Results.Ok(new { authenticated = true, username });
            }

            return Results.Ok(new { authenticated = false, username = string.Empty });
        });

        return endpoints;
    }
}
