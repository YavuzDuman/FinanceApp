## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Python scriptini build stage'e kopyala (run stage'de kullanmak için)
COPY StockService/StockService/fetch_bist_data.py /tmp/fetch_bist_data.py
# Sadece ilgili projeyi restore/publish et (diğer projelerdeki sürüm farkları restore'u etkilemesin)
RUN dotnet restore StockService/StockService/StockService.csproj
RUN dotnet publish StockService/StockService/StockService.csproj -c Release -o /app/out

## Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Python ve gerekli kütüphaneleri yükle
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    pip3 install yfinance && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/out .
# Python scriptini build stage'den kopyala
COPY --from=build /tmp/fetch_bist_data.py /app/fetch_bist_data.py
# Script'in çalıştırılabilir olduğundan emin ol
RUN chmod +x /app/fetch_bist_data.py || true

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
CMD ["sh","-c","dotnet StockService.dll --urls http://0.0.0.0:${PORT:-8080}"]


