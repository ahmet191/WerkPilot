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

# Pre-create the writable local-storage path here — the chiseled runtime has no
# shell to mkdir/chown it. It's copied into the image with the app uid below.
RUN mkdir -p /app/storage/uploads

# ── Runtime stage (chiseled / distroless) ─────────────────────────────────
# ~110 MB vs ~220 MB, and no shell or package manager = a much smaller attack
# surface. Runs as the built-in non-root 'app' user (uid 1654) by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

COPY --from=build /app/publish .
# Hand the storage tree to the app uid so a mounted volume inherits writable
# ownership (Storage:Provider=Local). Numeric uid because there's no shell to
# resolve $APP_UID in --chown.
COPY --from=build --chown=1654:1654 /app/storage /app/storage
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# No Docker HEALTHCHECK: a chiseled image has no shell/curl to run one. Health is
# the dependency-free /health/live and dependency-checking /health/ready HTTP
# endpoints, probed by the orchestrator (e.g. a Kubernetes liveness/readiness probe).
ENTRYPOINT ["dotnet", "OmniFlow.Api.dll"]
