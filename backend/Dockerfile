# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Configure data directory under /app/data
ENV DOTNET_EnableDiagnostics=0

# Create data directory for SQLite (mounted volume in production)
RUN mkdir -p /app/data
VOLUME ["/app/data"]

# Copy published output
COPY --from=build /app/publish .

# Expose ports (Render uses 8080 by default)
EXPOSE 80
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "backend.dll"]
