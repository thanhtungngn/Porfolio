namespace Portfolio.Api.Features.Portfolio.Contracts;

public sealed record PortfolioResponse(
    string Name,
    string Role,
    string Summary,
    string[] Technologies,
    string[] Highlights,
    ContactResponse Contact);
