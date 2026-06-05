# Stage 1: Build the WebAssembly static files
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
   
COPY ["RepoPulse.Client/RepoPulse.Client.csproj", "RepoPulse.Client/"]
RUN dotnet restore "RepoPulse.Client/RepoPulse.Client.csproj"
   
COPY . .
WORKDIR "/src/RepoPulse.Client"
RUN dotnet publish "RepoPulse.Client.csproj" -c Release -o /app/publish

# Stage 2: Serve the static files with Nginx
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
   
# Nuke the default Nginx welcome page so it doesn't conflict
RUN rm -rf ./*
   
# Copy the compiled Blazor files from the publish/wwwroot directory
COPY --from=build /app/publish/wwwroot .
# Overwrite the default routing configuration
COPY RepoPulse.Client/nginx.conf /etc/nginx/nginx.conf
   
EXPOSE 80
