# syntax=docker/dockerfile:1

# ---- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching): only the csproj is needed for restore.
COPY web/web.csproj web/
RUN dotnet restore web/web.csproj

# Copy the rest of the source and publish.
COPY web/ web/
RUN dotnet publish web/web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Run as a non-root user. App_dbs/App_files are created here (owned by the
# app user) so that Docker seeds any named volume mounted over them with the
# same ownership on first start — see docker-compose.yml.
# The base image already ships a non-root "app" user/group on some tags, so
# creation is made idempotent instead of assuming a clean slate.
RUN (getent group app >/dev/null || groupadd -r app) \
    && (id -u app >/dev/null 2>&1 || useradd -r -g app -m app) \
    && mkdir -p /app/App_dbs /app/App_files \
    && chown -R app:app /app

COPY --from=build --chown=app:app /app/publish .

USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "web.dll"]
