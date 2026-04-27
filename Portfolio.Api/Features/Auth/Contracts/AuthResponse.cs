namespace Portfolio.Api.Features.Auth.Contracts;

public sealed record AuthResponse(string Token, string Username, DateTime ExpiresAtUtc);