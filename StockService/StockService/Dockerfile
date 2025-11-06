## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Sadece ilgili projeyi restore/publish et (diğer projelerdeki sürüm farkları restore'u etkilemesin)
RUN dotnet restore StockService/StockService/StockService.csproj
RUN dotnet publish StockService/StockService/StockService.csproj -c Release -o /app/out

## Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
CMD ["sh","-c","dotnet StockService.dll --urls http://0.0.0.0:${PORT:-8080}"]


