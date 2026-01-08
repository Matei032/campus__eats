# Dockerfile pentru Render.com (Backend)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY CampusEats.Backend/*.csproj ./CampusEats.Backend/
COPY CampusEats.Frontend/*.csproj ./CampusEats.Frontend/

# Restore dependencies
RUN dotnet restore "CampusEats.Backend/CampusEats.Backend.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/CampusEats.Backend"
RUN dotnet publish "CampusEats.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "CampusEats.Backend.dll"]
