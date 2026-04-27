using Portfolio.Api.Features.Portfolio.Contracts;

namespace Portfolio.Api.Features.Portfolio.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/portfolio", () =>
        {
            var info = new PortfolioResponse(
                "Thanh Tung Nguyen",
                "Software Engineer",
                "I build practical full-stack products and AI-assisted workflows.",
                [".NET 10 / ASP.NET", "React", "Cloud Automation", "LLM Agents (OpenAI + Ollama)"],
                [
                    "Develop scalable backend services with ASP.NET.",
                    "Create responsive UI experiences in React.",
                    "Integrate AI copilots that explain implementation details on demand."
                ],
                new ContactResponse("thanhtungngn@example.com", "https://github.com/thanhtungngn", "https://www.linkedin.com/in/thanhtungngn")
            );

            return TypedResults.Ok(info);
        });

        return endpoints;
    }
}
