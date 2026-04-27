using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Features.Auth.Contracts;
using Portfolio.Api.Features.Auth.Services;
using Portfolio.Api.Infrastructure.Persistence;
using Portfolio.Api.Infrastructure.Persistence.Entities;

namespace Portfolio.Api.Features.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", async (
            RegisterRequest request,
            AppDbContext dbContext,
            IPasswordHasher<UserAccount> passwordHasher,
            JwtTokenService jwtTokenService,
            CancellationToken cancellationToken) =>
        {
            var username = request.Username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Username and password are required." });
            }

            if (request.Password.Length < 8)
            {
                return Results.BadRequest(new { error = "Password must be at least 8 characters." });
            }

            var normalizedUsername = username.ToLowerInvariant();
            var exists = await dbContext.Users.AnyAsync(
                user => user.Username.ToLower() == normalizedUsername,
                cancellationToken);

            if (exists)
            {
                return Results.Conflict(new { error = "Username already exists." });
            }

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Username = username,
                Role = "User",
                CreatedAtUtc = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            var expiresAtUtc = jwtTokenService.GetExpiryUtc();
            var token = jwtTokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Username, expiresAtUtc));
        });

        endpoints.MapPost("/api/auth/login", async (
            LoginRequest request,
            AppDbContext dbContext,
            IPasswordHasher<UserAccount> passwordHasher,
            JwtTokenService jwtTokenService,
            CancellationToken cancellationToken) =>
        {
            var username = request.Username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Username and password are required." });
            }

            var normalizedUsername = username.ToLowerInvariant();
            var user = await dbContext.Users.SingleOrDefaultAsync(
                item => item.Username.ToLower() == normalizedUsername,
                cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            var expiresAtUtc = jwtTokenService.GetExpiryUtc();
            var token = jwtTokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Username, expiresAtUtc));
        });

        endpoints.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                var username = httpContext.User.Identity?.Name ?? string.Empty;
                return Results.Ok(new { authenticated = true, userId, username });
            }

            return Results.Ok(new { authenticated = false, userId = string.Empty, username = string.Empty });
        });

        return endpoints;
    }
}