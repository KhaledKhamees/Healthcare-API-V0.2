# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source files
COPY . .

# Restore dependencies inside Linux container
RUN dotnet restore Healthcare-API_V0.2_API/Healthcare-API_V0.2_API.csproj

# Publish API
RUN dotnet publish Healthcare-API_V0.2_API/Healthcare-API_V0.2_API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Healthcare-API_V0.2_API.dll"]