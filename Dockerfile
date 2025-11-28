# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiar todo el código
COPY . .

# Restaurar dependencias y publicar
RUN dotnet restore "CruptoPriceTracker.sln"
RUN dotnet publish "CryptoPriceTracker.Api/CryptoPriceTracker.Api.csproj" -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Copiar la app publicada
COPY --from=build /app/publish .

# Copiar la base de datos existente
COPY CryptoPriceTracker.Api/crypto.db /app/crypto.db

EXPOSE 5000
ENTRYPOINT ["dotnet", "CryptoPriceTracker.Api.dll"]
