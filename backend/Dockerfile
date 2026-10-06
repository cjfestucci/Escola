# API do Rotina Escola. Contexto de build: a pasta backend/  →  docker build -f backend/Dockerfile backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaura primeiro (cache de camada): só os .csproj
COPY Escola.sln ./
COPY Escola.Domain/Escola.Domain.csproj Escola.Domain/
COPY Escola.Infrastructure/Escola.Infrastructure.csproj Escola.Infrastructure/
COPY Escola.Api/Escola.Api.csproj Escola.Api/
COPY Escola.Tests/Escola.Tests.csproj Escola.Tests/
RUN dotnet restore Escola.Api/Escola.Api.csproj

COPY . .
RUN dotnet publish Escola.Api/Escola.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

# curl só pro HEALTHCHECK
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Fotos enviadas e documentos de saúde ficam em disco: monte volumes nestes dois caminhos (ver docker-compose.yml).
RUN mkdir -p /app/wwwroot/uploads /app/dados-privados && chown -R $APP_UID /app/wwwroot/uploads /app/dados-privados
VOLUME ["/app/wwwroot/uploads", "/app/dados-privados"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 CMD curl -fs http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Escola.Api.dll"]
