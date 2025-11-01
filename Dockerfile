FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution files and Directory.Build.props for restore (single layer)
COPY ["Directory.Build.props", "CinemaList.sln", "./"]

# Copy project files and package lock files (single layer for better caching)
COPY ["src/CinemaList.Api/CinemaList.Api.csproj", "src/CinemaList.Api/packages.lock.json", "src/CinemaList.Api/"]
COPY ["src/CinemaList.Common/CinemaList.Common.csproj", "src/CinemaList.Common/packages.lock.json", "src/CinemaList.Common/"]
COPY ["src/CinemaList.Scraper/CinemaList.Scraper.csproj", "src/CinemaList.Scraper/packages.lock.json", "src/CinemaList.Scraper/"]

# Restore dependencies with locked mode (this layer is cached unless project files or lock files change)
RUN dotnet restore "CinemaList.sln" --locked-mode

# Copy the remaining source code
COPY ["src/", "./src/"]

# Build the application
WORKDIR /src/src/CinemaList.Api
RUN dotnet build "CinemaList.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "CinemaList.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-build /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CinemaList.Api.dll"]
