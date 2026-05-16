# SecretsClient — Backend Cliente

Backend en **C# .NET 8 + ASP.NET Core** para el Sistema Distribuido de Gestión de Secretos.
Corre **localmente en el PC del empleado** y guarda el **Fragmento A (F1)** de cada secreto en **SQL Server**.

> Parte del proyecto **Sincro-Vault** — Universidad Manuela Beltrán, Sistemas Distribuidos UMB 2026-1.
> Cliente real: **Aldeamo S.A.S.** (Bogotá D.C.)

---

## Instalación (5 minutos)

### Requisitos previos

1. [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. **SQL Server Express 2022+** ([descargar](https://www.microsoft.com/en-us/sql-server/sql-server-downloads), instalación "Basic")
3. El **servidor Python** corriendo (este backend cliente lo necesita para guardar F2). Ver [repo del servidor](https://github.com/Sincro-Vault/servidor).

### Setup automático

```powershell
.\setup.ps1
```

Eso hace **TODO** por ti:
1. Verifica .NET 8 SDK + instala `dotnet-ef`
2. Restaura paquetes y compila
3. Aplica las migraciones EF Core → crea la BD `SecretsClient` en tu SQL Server

### Levantar el backend

```powershell
dotnet run --project src/SecretsClient.API
```

- **Swagger UI:** http://localhost:8080/swagger
- **Health:** http://localhost:8080/api/health

---

## Arquitectura

```
[ Frontend React :5173 ]
        │ REST + JWT (10 min)
        ▼
┌─────────────────────────────────────┐
│  SecretsClient (.NET 8)             │
│  ┌─────────────┐  ┌────────────────┐│
│  │ ASP.NET API │  │ SecretManager  ││
│  │   :8080     │  │ - Shamir 2/2   ││
│  └─────────────┘  │ - AES-256-GCM  ││
│         │         │ - PBKDF2 100k  ││
│         ▼         └────────────────┘│
│  ┌─────────────┐         │          │
│  │ SQL Server  │ F1      │          │
│  │ SecretsClient│         │          │
│  └─────────────┘         │          │
└──────────────────────────┼──────────┘
                           │ HTTP (gRPC en producción)
                           ▼ F2
                  ┌─────────────────┐
                  │ Servidor Python │
                  │   :9000 / :50051│
                  └─────────────────┘
```

---

## Endpoints REST principales

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/auth/register` | Registro de usuario |
| `POST` | `/api/auth/login` | Login → devuelve `{user, token}` |
| `POST` | `/api/auth/logout` | Revoca el token |
| `GET` | `/api/secrets?page=1&limit=10&search=&category=` | Lista paginada |
| `POST` | `/api/secrets` | Crear secreto (fragmenta + envía F2 al servidor) |
| `GET` | `/api/secrets/{id}` | Metadata del secreto |
| `POST` | `/api/secrets/{id}/reveal` | **Reconstruir y devolver el valor** (con geofencing opcional) |
| `PUT` | `/api/secrets/{id}` | Editar nombre/descripción |
| `DELETE` | `/api/secrets/{id}` | Eliminar (borra F1 local + F2 remoto) |
| `GET` | `/api/secrets/stats` | Estadísticas para el dashboard |
| `POST` | `/api/certificates/upload` | Cargar y validar certificado RSA |
| `GET` | `/api/health` | Health check |

---

## Lo que hace el `SecretManager`

Cuando creas un secreto:
1. Genera clave AES-256 aleatoria
2. Cifra el valor con **AES-256-GCM** → `EncryptedPayload`
3. Divide la clave AES con **Shamir 2-de-2** → F1, F2
4. Guarda **F1 en SQL Server local**
5. Envía **F2 al servidor Python** vía HTTP (con token compartido `X-Internal-Token`)
6. Si el servidor falla, hace **rollback** del secreto local

Cuando lees un secreto (`/reveal`):
1. Valida JWT (10 min de expiración)
2. Valida geofencing si el secreto tiene `GeoPolicies` (Haversine + margen `AccuracyMeters`)
3. Lee F1 local, pide F2 al servidor
4. Reconstruye clave AES con Shamir, descifra payload
5. Devuelve el plaintext en RAM volátil (el frontend lo blurea en 10s)

---

## Configuración

### `appsettings.json` — URL del servidor central

```json
"Server": {
  "CentralUrl": "http://localhost:9000",
  "InternalToken": "shared-secret-cliente-servidor-2026"
}
```

Si el servidor Python está en otra PC, cambia `localhost` por la IP real:
```json
"CentralUrl": "http://192.168.1.50:9000"
```

O usa variable de entorno (override sin tocar el archivo):
```powershell
$env:Server__CentralUrl = "http://192.168.1.50:9000"
dotnet run --project src/SecretsClient.API
```

### Connection string SQL Server

Por defecto apunta a la instancia `MSSQLSERVER` (default) con Windows Auth:
```
Server=localhost;Database=SecretsClient;Trusted_Connection=True;TrustServerCertificate=True
```

Si tu instancia tiene otro nombre (`SQLEXPRESS`, etc.), edita `appsettings.json`.

---

## Tecnologías

- **.NET 8** + ASP.NET Core Web API
- **Entity Framework Core 8** con SQL Server
- **JWT** (10 min de expiración — `Microsoft.IdentityModel.Tokens`)
- **PBKDF2-SHA256** (100k iteraciones) para passwords
- **AES-256-GCM** + **Shamir Secret Sharing** (implementación propia)
- **Polly** para retry HTTP hacia el servidor central
- **xUnit + Moq** para tests

---

## Estructura

```
proyecto distrubuidos/
├── src/
│   ├── SecretsClient.Core/          # Dominio + interfaces
│   │   ├── Domain/Entities/         # User, Secret, SecretFragment, GeoPolicy
│   │   ├── DTOs/
│   │   └── Services/                # Todas las interfaces
│   ├── SecretsClient.Infrastructure/
│   │   ├── Application/             # SecretManager, HealthCheckService
│   │   ├── Auth/                    # AuthService, RsaSignatureService
│   │   ├── Crypto/                  # CryptoService (AES-256-GCM)
│   │   ├── Data/                    # SecretsDbContext
│   │   ├── Geo/                     # GeoValidator + LocationProviders
│   │   ├── Repositories/            # User/Secret/Fragment repositories
│   │   ├── Shamir/                  # ShamirService
│   │   ├── Storage/                 # SecureStorage
│   │   └── Sync/                    # FragmentSyncClient (HTTP → servidor)
│   └── SecretsClient.API/
│       └── Controllers/             # Auth, Secrets, Health, Sync, Certificates
├── tests/
│   ├── SecretsClient.UnitTests/
│   └── SecretsClient.IntegrationTests/
└── setup.ps1                        # Setup con un solo comando
```

---

## Docker (opcional — para deploy en AWS / Cloud Run / cualquier servidor)

Levanta el backend cliente + SQL Server juntos con un solo comando:

```bash
# Build + run del cliente .NET + SQL Server 2022 en containers
docker compose up -d --build

# Ver logs
docker compose logs -f client

# Detener
docker compose down

# Detener y borrar la BD persistente
docker compose down -v
```

El backend queda en `localhost:8080`. **Las migraciones EF Core se aplican automaticamente** al startup (variable `RUN_MIGRATIONS_ON_STARTUP=true` ya seteada en el compose).

### Conectarse al servidor Python desde el container

Por defecto el cliente apunta a `http://host.docker.internal:9000` — eso significa: el servidor Python corre en el HOST (tu PC o el host del cloud), fuera del container.

Si el servidor Python corre **en otra PC en la LAN**:
```bash
SERVER_CENTRAL_URL=http://192.168.1.50:9000 docker compose up -d
```

Si el servidor Python tambien corre en Docker en la misma maquina, agrega ambos servicios al mismo compose (combinar `docker-compose.yml` de ambos repos).

### Variables de entorno disponibles
- `ConnectionStrings__DefaultConnection` — connection string SQL Server
- `Server__CentralUrl` — URL del servidor Python
- `Server__InternalToken` — token compartido cliente↔servidor
- `Jwt__SecretKey` — clave de firma JWT (cambiar en produccion)
- `RUN_MIGRATIONS_ON_STARTUP` — si `true`, aplica migraciones al arrancar (recomendado en Docker)

**Imagen final pesa ~220MB** (multi-stage build con `dotnet/aspnet:8.0` runtime).

## Tests

```powershell
dotnet test
```

---

## Deploy en otra PC

Si quieres que el cliente esté en una PC distinta al servidor:

1. Instala SQL Server Express en **esta** PC (donde corre el cliente).
2. Cambia `Server.CentralUrl` en `appsettings.json` a la IP de la PC del servidor.
3. Abre el firewall puerto 8080 para que el frontend pueda hablarte (solo si el frontend está en otra PC también).

Ver guía completa **`DEPLOY_MULTI_PC.md`** en el directorio padre.

---

## Equipo

| Nombre | Rol |
|---|---|
| Harold Camargo | Líder |
| Samuel Ortiz | API e Integración (este repo) |
| Michael Ramírez | Seguridad y Criptografía |
| Juan Stiven Castro | Desarrollo |
| Jose | Datos y Persistencia |
