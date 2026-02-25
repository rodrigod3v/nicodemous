#!/bin/bash

echo "--- Iniciando nicodemouse (macOS Workflow) ---"

# 1. Install frontend dependencies if needed
if [ ! -d "frontend/node_modules" ]; then
    echo "--- Instalando dependencias do frontend... ---"
    cd frontend && npm install && cd ..
fi

# 2. Start Signaling Server in a separate terminal window
echo "--- Iniciando Signaling Server (.NET) na porta 5219... ---"
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'/server\" && dotnet run --urls http://0.0.0.0:5219"'

# 3. Start Frontend in a separate terminal window
echo "--- Iniciando Servidor Vite (Frontend)... ---"
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'/frontend\" && npm run dev"'

# 4. Wait for initialization
echo "--- Aguardando inicialização (5s)... ---"
sleep 5

# 5. Start Backend in current window
echo "--- Iniciando Backend (.NET)... ---"
cd backend && dotnet run
