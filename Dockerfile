# ==============================================================================
# Multi-Stage Production Dockerfile for LogisticsSystem.Api
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Base Runtime Environment
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# ------------------------------------------------------------------------------
# Stage 2: Build & Restore with Layer Caching
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files individually to maximize Docker restore caching
COPY ["src/LogisticsSystem.Api/LogisticsSystem.Api.csproj", "src/LogisticsSystem.Api/"]
COPY ["src/LogisticsSystem.Application/LogisticsSystem.Application.csproj", "src/LogisticsSystem.Application/"]
COPY ["src/LogisticsSystem.Domain/LogisticsSystem.Domain.csproj", "src/LogisticsSystem.Domain/"]
COPY ["src/LogisticsSystem.Infrastructure/LogisticsSystem.Infrastructure.csproj", "src/LogisticsSystem.Infrastructure/"]

# Restore NuGet dependencies
RUN dotnet restore "src/LogisticsSystem.Api/LogisticsSystem.Api.csproj"

# Copy all remaining source code
COPY . .

# Build application binaries
WORKDIR "/src/src/LogisticsSystem.Api"
RUN dotnet build "LogisticsSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ------------------------------------------------------------------------------
# Stage 3: Publish Standalone Artifacts
# ------------------------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LogisticsSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 4: Production Final Runtime Image
# ------------------------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set container runtime defaults
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Container health check probe
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LogisticsSystem.Api.dll"]
