<#
.SYNOPSIS
    Configura el entorno de desarrollo para SecretsClient.
.DESCRIPTION
    Crea carpetas necesarias, copia appsettings de ejemplo,
    y restaura paquetes NuGet.
#>

Write-Host "=== Configurando SecretsClient ===" -ForegroundColor Cyan

# Crear carpetas de datos
$dataDir = Join-Path $PSScriptRoot "data"
$blobsDir = Join-Path $dataDir "blobs"
if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir | Out-Null }
if (-not (Test-Path $blobsDir)) { New-Item -ItemType Directory -Path $blobsDir | Out-Null }
Write-Host "✓ Carpetas de datos creadas: $dataDir, $blobsDir"

# Restaurar paquetes
Write-Host "`nRestaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore SecretsClient.sln

# Build inicial
Write-Host "`nCompilando solución..." -ForegroundColor Yellow
dotnet build -c Debug

Write-Host "`n=== Configuración completada ===" -ForegroundColor Green
Write-Host "Ejecutar API: dotnet run --project src/SecretsClient.API"
Write-Host "Ejecutar tests: dotnet test"
