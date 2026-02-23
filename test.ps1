# Nicodemous Unified Test Script
# Este script inicia o Frontend e o Backend simultaneamente para testes.

Write-Host "--- Iniciando Nicodemous (Ambiente de Teste) ---" -ForegroundColor Cyan

# 1. Verificar dependencias do Frontend
if (!(Test-Path "frontend/node_modules")) {
    Write-Host "--- Instalando dependencias do frontend... ---" -ForegroundColor Yellow
    Set-Location frontend
    npm install
    Set-Location ..
}

# 2. Iniciar o Frontend em uma nova janela
Write-Host "--- Iniciando Servidor Vite (Frontend)... ---" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd frontend; npm run dev"

# 3. Pequena pausa para o Vite preparar o servidor
Start-Sleep -Seconds 2

# 4. Iniciar o Backend na janela atual
Write-Host "--- Iniciando Backend (.NET)... ---" -ForegroundColor Green
Set-Location backend
dotnet run -f net8.0-windows
