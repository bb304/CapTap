# CapTap API — multi-stage production image
# Build from repo root:
#   docker build -f server/CapTap.Api/Dockerfile -t captap-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY server/CapTap.Api/CapTap.Api.csproj CapTap.Api/
COPY server/CapTap.Application/CapTap.Application.csproj CapTap.Application/
COPY server/CapTap.Domain/CapTap.Domain.csproj CapTap.Domain/
COPY server/CapTap.Infrastructure/CapTap.Infrastructure.csproj CapTap.Infrastructure/
COPY server/CapTap.Shared/CapTap.Shared.csproj CapTap.Shared/

RUN dotnet restore CapTap.Api/CapTap.Api.csproj

COPY server/CapTap.Api/ CapTap.Api/
COPY server/CapTap.Application/ CapTap.Application/
COPY server/CapTap.Domain/ CapTap.Domain/
COPY server/CapTap.Infrastructure/ CapTap.Infrastructure/
COPY server/CapTap.Shared/ CapTap.Shared/

RUN dotnet publish CapTap.Api/CapTap.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system captap \
    && useradd --system --gid captap --create-home captap

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .
RUN chown -R captap:captap /app
USER captap

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CapTap.Api.dll"]
