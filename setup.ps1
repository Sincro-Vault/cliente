# Setup automatico del Backend Cliente (.NET 8 + SQL Server)
# Uso: .\setup.ps1
# Requiere: .NET 8 SDK, dotnet-ef tool, SQL Server (Express o LocalDB)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptRoot

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " SecretsClient - Setup automatico" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar .NET 8 SDK
Write-Host "[1/4] Verificando .NET 8 SDK..." -ForegroundColor Yellow
try {
    $dotnetVer = dotnet --version 2>&1
    Write-Host "      .NET $dotnetVer" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET 8 SDK no instalado. Descarga de dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# 2. Verificar / instalar dotnet-ef
Write-Host ""
Write-Host "[2/4] Verificando dotnet-ef..." -ForegroundColor Yellow
$efOk = (dotnet ef --version 2>$null) -ne $null
if (-not $efOk) {
    Write-Host "      Instalando dotnet-ef..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}
Write-Host "      dotnet-ef OK" -ForegroundColor Green

# 3. Restaurar y compilar
Write-Host ""
Write-Host "[3/4] Restaurando paquetes y compilando..." -ForegroundColor Yellow
dotnet restore src/SecretsClient.API/SecretsClient.API.csproj | Out-Null
dotnet build src/SecretsClient.API/SecretsClient.API.csproj -c Debug --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: build fallo" -ForegroundColor Red
    exit 1
}
Write-Host "      Compilado" -ForegroundColor Green

# 4. Aplicar migraciones a SQL Server
Write-Host ""
Write-Host "[4/4] Aplicando migraciones a SQL Server..." -ForegroundColor Yellow
Write-Host "      (asegurate de tener SQL Server corriendo en localhost)" -ForegroundColor Gray
dotnet ef database update --project src/SecretsClient.Infrastructure --startup-project src/SecretsClient.API
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: no se pudo conectar a SQL Server." -ForegroundColor Red
    Write-Host "  Verifica que SQL Server este instalado y corriendo:" -ForegroundColor Yellow
    Write-Host "    sc query MSSQLSERVER     (debe decir RUNNING)" -ForegroundColor Cyan
    Write-Host "  Si tu instancia se llama distinto, edita appsettings.json:" -ForegroundColor Yellow
    Write-Host "    Server=localhost\NOMBRE_INSTANCIA;Database=SecretsClient;..." -ForegroundColor Cyan
    exit 1
}
Write-Host "      Base de datos SecretsClient creada" -ForegroundColor Green

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
Write-Host ""
Write-Host "IMPORTANTE: el servidor central Python debe estar corriendo en otra terminal" -ForegroundColor Yellow
Write-Host "antes de crear secretos. Por defecto se asume http://localhost:9000" -ForegroundColor Yellow
Write-Host "(configurable en src/SecretsClient.API/appsettings.json -> Server.CentralUrl)" -ForegroundColor Yellow
Write-Host ""
