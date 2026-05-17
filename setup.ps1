# Setup automatico del Backend Cliente (.NET 8 + SQLite)
# Uso: .\setup.ps1
# Requiere: .NET 8 SDK, dotnet-ef tool

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptRoot

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " SecretsClient - Setup automatico (SQLite)" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar .NET 8 SDK
Write-Host "[1/5] Verificando .NET 8 SDK..." -ForegroundColor Yellow
try {
    $dotnetVer = dotnet --version 2>&1
    Write-Host "      .NET $dotnetVer" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET 8 SDK no instalado. Descarga de dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# 2. Verificar / instalar dotnet-ef
Write-Host ""
Write-Host "[2/5] Verificando dotnet-ef..." -ForegroundColor Yellow
$efOk = (dotnet ef --version 2>$null) -ne $null
if (-not $efOk) {
    Write-Host "      Instalando dotnet-ef..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}
Write-Host "      dotnet-ef OK" -ForegroundColor Green

# 3. PREGUNTAR donde esta el servidor central (otra PC vs misma PC)
Write-Host ""
Write-Host "[3/5] Configurando URL del servidor central..." -ForegroundColor Yellow
Write-Host ""
Write-Host "  El servidor central esta corriendo en..." -ForegroundColor White
Write-Host "    [1] Esta misma PC (localhost)" -ForegroundColor White
Write-Host "    [2] Otra PC en la red LAN" -ForegroundColor White
$choice = Read-Host "  Elige (1 o 2)"

if ($choice -eq "2") {
    $serverIp = Read-Host "  IP de la PC del servidor (ej: 192.168.1.2)"
    if ([string]::IsNullOrWhiteSpace($serverIp)) {
        Write-Host "ERROR: IP requerida" -ForegroundColor Red
        exit 1
    }
    $centralUrl = "http://${serverIp}:9000"
} else {
    $centralUrl = "http://localhost:9000"
}
Write-Host "      Servidor central: $centralUrl" -ForegroundColor Green

# Actualizar appsettings.json
$appsettingsPath = "src\SecretsClient.API\appsettings.json"
$json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$json.Server.CentralUrl = $centralUrl
$json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8
Write-Host "      appsettings.json actualizado" -ForegroundColor Green

# 4. Restaurar y compilar
Write-Host ""
Write-Host "[4/5] Restaurando paquetes y compilando..." -ForegroundColor Yellow
dotnet restore src/SecretsClient.API/SecretsClient.API.csproj | Out-Null
dotnet build src/SecretsClient.API/SecretsClient.API.csproj -c Debug --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: build fallo" -ForegroundColor Red
    exit 1
}
Write-Host "      Compilado" -ForegroundColor Green

# 5. Crear archivo SQLite y aplicar migraciones
Write-Host ""
Write-Host "[5/5] Aplicando migraciones EF Core (SQLite)..." -ForegroundColor Yellow
Write-Host "      (creara secrets.db en la raiz del proyecto si no existe)" -ForegroundColor Gray
dotnet ef database update --project src/SecretsClient.Infrastructure --startup-project src/SecretsClient.API
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: fallo aplicacion de migraciones." -ForegroundColor Red
    Write-Host "  Verifica que exista la carpeta src/SecretsClient.Infrastructure/Migrations/" -ForegroundColor Yellow
    Write-Host "  Si no existe, crearla con:" -ForegroundColor Yellow
    Write-Host "    dotnet ef migrations add InitialCreate --project src/SecretsClient.Infrastructure --startup-project src/SecretsClient.API" -ForegroundColor Cyan
    exit 1
}
Write-Host "      Base de datos secrets.db lista" -ForegroundColor Green

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host " Setup completo." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Para levantar el backend cliente:" -ForegroundColor White
Write-Host "    dotnet run --project src/SecretsClient.API" -ForegroundColor Cyan
Write-Host ""
Write-Host "Swagger : http://localhost:8080/swagger" -ForegroundColor White
Write-Host "Health  : http://localhost:8080/api/health" -ForegroundColor White
Write-Host "Servidor central configurado en: $centralUrl" -ForegroundColor White
Write-Host ""
if ($choice -eq "2") {
    Write-Host "IMPORTANTE: en la PC del servidor ($serverIp), asegurate de que:" -ForegroundColor Yellow
    Write-Host "  1. El servidor este corriendo" -ForegroundColor Cyan
    Write-Host "  2. El firewall tenga abiertos los puertos 9000 y 50051" -ForegroundColor Cyan
    Write-Host "     (correr en esa PC como Admin: scripts\open-firewall.ps1)" -ForegroundColor Cyan
    Write-Host "  3. Verificar conectividad: curl http://${serverIp}:9000/api/health" -ForegroundColor Cyan
    Write-Host ""
}
