# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies to leverage Docker layer caching
COPY ["RepoPulse.API/RepoPulse.API.csproj", "RepoPulse.API/"]
RUN dotnet restore "RepoPulse.API/RepoPulse.API.csproj"

# Copy the rest of the code and compile
COPY . .
WORKDIR "/src/RepoPulse.API"
RUN dotnet publish "RepoPulse.API.csproj" -c Release -o /app/publish

# Stage 2: Serve the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Install curl for the docker-compose healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Set up the persistent volume directory for SQLite
RUN mkdir -p /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/repopulse.db"
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080
ENTRYPOINT ["dotnet", "RepoPulse.API.dll"]
