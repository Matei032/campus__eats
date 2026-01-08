FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln .
COPY CampusEats.Backend/*.csproj ./CampusEats.Backend/
COPY CampusEats.Frontend/*.csproj ./CampusEats.Frontend/

RUN dotnet restore "CampusEats.Frontend/CampusEats.Frontend.csproj"

COPY . .

WORKDIR "/src/CampusEats.Frontend"
RUN dotnet publish "CampusEats.Frontend.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

COPY --from=build /app/publish/wwwroot .
COPY CampusEats.Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

ENTRYPOINT ["nginx", "-g", "daemon off;"]
