# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MicroDocuments.sln .
COPY MicroDocuments.Domain/MicroDocuments.Domain.csproj MicroDocuments.Domain/
COPY MicroDocuments.Application/MicroDocuments.Application.csproj MicroDocuments.Application/
COPY MicroDocuments.Infrastructure/MicroDocuments.Infrastructure.csproj MicroDocuments.Infrastructure/
COPY MicroDocuments.Api/MicroDocuments.Api.csproj MicroDocuments.Api/

# Restore dependencies for API project (which includes all necessary dependencies)
RUN dotnet restore MicroDocuments.Api/MicroDocuments.Api.csproj

# Copy all source files
COPY MicroDocuments.Domain/ MicroDocuments.Domain/
COPY MicroDocuments.Application/ MicroDocuments.Application/
COPY MicroDocuments.Infrastructure/ MicroDocuments.Infrastructure/
COPY MicroDocuments.Api/ MicroDocuments.Api/

# Build the application
WORKDIR /src/MicroDocuments.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install wget for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*

# Create directories for database and temp uploads
RUN mkdir -p /app/db /app/temp_uploads

# Copy published application
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "MicroDocuments.Api.dll"]

