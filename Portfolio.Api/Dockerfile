FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Portfolio.slnx ./
COPY Portfolio.Api/Portfolio.Api.csproj Portfolio.Api/
COPY Portfolio.Tests/Portfolio.Tests.csproj Portfolio.Tests/
RUN dotnet restore Portfolio.Api/Portfolio.Api.csproj

COPY . .
RUN dotnet publish Portfolio.Api/Portfolio.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
