#!/bin/bash

echo "--- Iniciando nicodemouse (macOS Workflow) ---"

# 1. Install frontend dependencies if needed
if [ ! -d "frontend/node_modules" ]; then
    echo "--- Instalando dependencias do frontend... ---"
    cd frontend && npm install && cd ..
fi

# 2. Start Signaling Server in background
echo "--- Iniciando Signaling Server (.NET)... ---"
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'/server\" && dotnet run --launch-profile http"'

# 3. Start Frontend in background
echo "--- Iniciando Servidor Vite (Frontend)... ---"
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'/frontend\" && npm run dev"'

# 4. Wait for initialization
echo "--- Aguardando inicialização (5s)... ---"
sleep 5

# 5. Start Backend in current window
echo "--- Iniciando Backend (.NET)... ---"
cd backend && dotnet run
