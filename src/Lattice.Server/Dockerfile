# Lattice Server Dockerfile
# Multi-stage build for .NET application with cross-compilation support

# Build stage - always runs on native architecture
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj files and restore for target architecture
COPY Lattice.Core/Lattice.Core.csproj Lattice.Core/
COPY Lattice.Server/Lattice.Server.csproj Lattice.Server/
RUN dotnet restore Lattice.Server/Lattice.Server.csproj -a $TARGETARCH

# Copy source code and build for target architecture
COPY Lattice.Core/ Lattice.Core/
COPY Lattice.Server/ Lattice.Server/
RUN dotnet publish Lattice.Server/Lattice.Server.csproj -c Release -f net10.0 -a $TARGETARCH -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create directories for data persistence
RUN mkdir -p /app/logs /app/documents /app/data

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8000

# Run the application
ENTRYPOINT ["dotnet", "Lattice.Server.dll"]
