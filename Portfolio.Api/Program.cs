using Portfolio.Api.Features.Auth.Endpoints;
using Portfolio.Api.Features.Chat.Endpoints;
using Portfolio.Api.Features.Portfolio.Endpoints;
using Portfolio.Api.Features.Rag.Endpoints;
using Portfolio.Api.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Portfolio RAG API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapAuthEndpoints();
app.MapPortfolioEndpoints();
app.MapChatEndpoints();
app.MapRagEndpoints();

app.Run();
