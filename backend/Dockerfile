# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/*.csproj backend/
RUN dotnet restore backend/*.csproj

# Copy everything and build
COPY backend/ backend/
RUN dotnet publish backend/*.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Environment
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-5000}
# Configure data directory under /app/data
ENV DOTNET_EnableDiagnostics=0

# Create data directory for SQLite (mounted volume in production)
RUN mkdir -p /app/data
VOLUME ["/app/data"]

# Copy published output
COPY --from=build /app/publish .

# Expose default port (platforms often override with PORT env)
EXPOSE 5000

# Run the app
ENTRYPOINT ["dotnet", "backend.dll"]
