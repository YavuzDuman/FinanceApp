## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

## Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
# Render PORT env'i sağlar; local için 8080 fallback
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
CMD ["sh","-c","dotnet ApiGateway.dll --urls http://0.0.0.0:${PORT:-8080}"]


