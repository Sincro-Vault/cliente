# =====================================================
# Dockerfile multi-stage para el SecretsClient (.NET 8)
# =====================================================

# ----------- Stage 1: build --------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
RUN echo "building with diagnostic logging v4"

# Copiar csproj primero para cachear el restore en una capa separada
COPY src/SecretsClient.Core/SecretsClient.Core.csproj           src/SecretsClient.Core/
COPY src/SecretsClient.Infrastructure/SecretsClient.Infrastructure.csproj src/SecretsClient.Infrastructure/
COPY src/SecretsClient.API/SecretsClient.API.csproj             src/SecretsClient.API/

RUN dotnet restore src/SecretsClient.API/SecretsClient.API.csproj

# Copiar el resto del codigo y publicar release
COPY src/ src/
RUN dotnet publish src/SecretsClient.API/SecretsClient.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ----------- Stage 2: runtime ------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Instalar curl para healthchecks (la imagen base no lo incluye)
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Crear usuario no-root para mejor seguridad
RUN groupadd --system --gid 1000 secretsclient \
 && useradd --system --uid 1000 --gid secretsclient --create-home secretsclient

WORKDIR /app
COPY --from=build /app/publish .

# Carpeta /app/data para el archivo SQLite (montada como volumen en docker-compose)
RUN mkdir -p /app/data \
 && chown -R secretsclient:secretsclient /app
USER secretsclient

EXPOSE 8080

# Kestrel escucha en 0.0.0.0:8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Aplicar migraciones automaticamente al startup (solo en Docker)
    RUN_MIGRATIONS_ON_STARTUP=true

ENTRYPOINT ["dotnet", "SecretsClient.API.dll"]
