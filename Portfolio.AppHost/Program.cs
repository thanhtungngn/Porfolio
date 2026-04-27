var builder = DistributedApplication.CreateBuilder(args);

var dashboardHost =
    builder.Configuration["ASPIRE_DASHBOARD_HOST"]
    ?? Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_HOST")
    ?? "localhost";

var dashboardPort =
    builder.Configuration["ASPIRE_DASHBOARD_PORT"]
    ?? Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_PORT")
    ?? "18888";

Console.WriteLine($"[Aspire Dashboard] http://{dashboardHost}:{dashboardPort}");

var qdrant = builder
    .AddQdrant("qdrant")
    .WithDataVolume();

var api = builder
    .AddProject("portfolio-api", "../Portfolio.Api/Portfolio.Api.csproj")
    .WithReference(qdrant)
    .WithExternalHttpEndpoints();

builder.Build().Run();
