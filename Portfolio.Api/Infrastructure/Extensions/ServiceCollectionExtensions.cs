using System.Threading.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Features.Auth.Services;
using Portfolio.Api.Features.Chat.Options;
using Portfolio.Api.Features.Chat.Services;
using Portfolio.Api.Features.Rag.Options;
using Portfolio.Api.Features.Rag.Services;

namespace Portfolio.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly string[] DefaultFrontendOrigins = ["http://localhost:5173"];

    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddHttpClient();

        services.Configure<ApiSecurityOptions>(configuration.GetSection("Security"));
        services.Configure<OpenAiOptions>(configuration.GetSection("ChatProviders:OpenAI"));
        services.Configure<OllamaOptions>(configuration.GetSection("ChatProviders:Ollama"));
        services.Configure<McpOptions>(configuration.GetSection("Mcp"));
        services.Configure<OpenAiEmbeddingOptions>(configuration.GetSection("Rag:OpenAiEmbedding"));
        services.Configure<QdrantOptions>(configuration.GetSection("Rag:Qdrant"));

        var securityOptions = configuration.GetSection("Security").Get<ApiSecurityOptions>() ?? new ApiSecurityOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = securityOptions.JwtIssuer,
                    ValidAudience = securityOptions.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityOptions.ResolveJwtSigningKey())),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("chat", limiter =>
            {
                limiter.PermitLimit = 20;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
                limiter.AutoReplenishment = true;
            });
        });

        services.AddSingleton<IVectorStore, QdrantVectorStore>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<DocumentChunker>();
        services.AddScoped<PdfDocumentLoader>();
        services.AddScoped<IngestionService>();
        services.AddScoped<RagRetrievalService>();
        services.AddScoped<IChatProvider, OpenAiChatProvider>();
        services.AddScoped<IChatProvider, OllamaChatProvider>();
        services.AddScoped<IMcpToolGateway, McpToolGateway>();
        services.AddScoped<ChatAgentService>();

        services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
            {
                policy.WithOrigins(ResolveFrontendOrigins(configuration))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static string[] ResolveFrontendOrigins(IConfiguration configuration)
    {
        var configuredOrigins = configuration["FRONTEND_ORIGINS"];

        if (string.IsNullOrWhiteSpace(configuredOrigins))
        {
            return DefaultFrontendOrigins;
        }

        var origins = configuredOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        return origins.Length > 0 ? origins : DefaultFrontendOrigins;
    }
}
