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
