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

# curl is only here to back the container HEALTHCHECK below; keep the layer
# minimal and drop the apt lists so nothing lingers in the image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The app runs as the image's built-in non-root user (APP_UID=1654). Create the
# local-storage path and hand it to that uid *before* switching users, so a
# named volume mounted here inherits writable ownership (Docker seeds an empty
# volume from the mountpoint's permissions). Only matters for Storage:Provider=Local.
RUN mkdir -p /app/storage/uploads && chown -R $APP_UID:$APP_UID /app/storage
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Generous start period: first boot applies EF migrations and seeds before the
# app answers. /health/live is dependency-free (no DB), so it flips ready fast
# once Kestrel is up.
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "OmniFlow.Api.dll"]
