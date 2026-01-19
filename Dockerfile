# Multi-stage Docker build
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/MockDelivery.Api/MockDelivery.Api.csproj", "MockDelivery.Api/"]
COPY ["src/Common/Common.csproj", "Common/"]

# Restore dependencies
RUN dotnet restore "MockDelivery.Api/MockDelivery.Api.csproj"

# Copy source code
COPY src/MockDelivery.Api/. MockDelivery.Api/
COPY src/Common/. Common/

# Build
WORKDIR "/src/MockDelivery.Api"
RUN dotnet build "MockDelivery.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MockDelivery.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MockDelivery.Api.dll"]
