FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln .
COPY CampusEats.Backend/*.csproj ./CampusEats.Backend/
COPY CampusEats.Frontend/*.csproj ./CampusEats.Frontend/

RUN dotnet restore "./CampusEats.Backend/CampusEats.Backend.csproj"

COPY . .

WORKDIR "/src/CampusEats.Backend"
RUN dotnet publish "CampusEats.Backend.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CampusEats.Backend.dll"]