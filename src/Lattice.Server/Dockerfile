# Lattice Server Dockerfile
# Multi-stage build for .NET application

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY Lattice.Core/Lattice.Core.csproj Lattice.Core/
COPY Lattice.Server/Lattice.Server.csproj Lattice.Server/
RUN dotnet restore Lattice.Server/Lattice.Server.csproj

# Copy source code and build
COPY Lattice.Core/ Lattice.Core/
COPY Lattice.Server/ Lattice.Server/
RUN dotnet publish Lattice.Server/Lattice.Server.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create directories for data persistence
RUN mkdir -p /app/logs /app/documents /app/data

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8000

# Run the application
ENTRYPOINT ["dotnet", "Lattice.Server.dll"]
