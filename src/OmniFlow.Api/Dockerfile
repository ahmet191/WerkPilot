# ── Build stage ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the csproj graph first so layer caching survives code edits.
COPY OmniFlow.slnx ./
COPY src/OmniFlow.Domain/OmniFlow.Domain.csproj src/OmniFlow.Domain/
COPY src/OmniFlow.Application/OmniFlow.Application.csproj src/OmniFlow.Application/
COPY src/OmniFlow.Infrastructure/OmniFlow.Infrastructure.csproj src/OmniFlow.Infrastructure/
COPY src/OmniFlow.Api/OmniFlow.Api.csproj src/OmniFlow.Api/
RUN dotnet restore src/OmniFlow.Api/OmniFlow.Api.csproj

COPY src/ src/
RUN dotnet publish src/OmniFlow.Api/OmniFlow.Api.csproj -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OmniFlow.Api.dll"]
