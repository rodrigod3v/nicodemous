# nicodemouse Unified Test Script
# Este script inicia o Frontend e o Backend simultaneamente para testes.

Write-Host "--- Iniciando nicodemouse (Ambiente de Teste) ---" -ForegroundColor Cyan

$FrontendPath = Join-Path $PSScriptRoot "../frontend"
$BackendPath = Join-Path $PSScriptRoot "../backend"
$ServerPath = Join-Path $PSScriptRoot "../server"

# 1. Verificar dependencias do Frontend
if (!(Test-Path "$FrontendPath/node_modules")) {
    Write-Host "--- Instalando dependencias do frontend... ---" -ForegroundColor Yellow
    Push-Location $FrontendPath
    npm install
    Pop-Location
}

# 2. Iniciar o Signaling Server em uma nova janela
Write-Host "--- Iniciando Signaling Server (.NET)... ---" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ServerPath'; dotnet run --launch-profile http"

# 3. Iniciar o Frontend em uma nova janela
Write-Host "--- Iniciando Servidor Vite (Frontend)... ---" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$FrontendPath'; npm run dev"

# 4. Aguardar inicialização (5s)...
Write-Host "--- Aguardando inicialização (5s)... ---" -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 5. Iniciar o Backend na janela atual
Write-Host "--- Iniciando Backend (.NET)... ---" -ForegroundColor Green
Push-Location $BackendPath
dotnet run -f net8.0-windows
Pop-Location
