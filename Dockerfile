
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# сертифікати для TLS/SSL

RUN apt-get update && apt-get install -y ca-certificates libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LeveLEO.csproj", "."]
RUN dotnet restore "./LeveLEO.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./LeveLEO.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LeveLEO.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeveLEO.dll"]
