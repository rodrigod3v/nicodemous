# 🛠️ Nicodemous: Guia de Desenvolvimento & Execução Local

Este guia detalha como configurar seu ambiente para rodar o **Nicodemous** em modo de desenvolvimento, permitindo que você faça alterações e veja os resultados instantaneamente.

---

## 📋 1. Pré-requisitos

Antes de começar, verifique se você tem as seguintes ferramentas instaladas:

1.  **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**: Necessário para compilar o backend C#.
2.  **[Node.js (v18+)](https://nodejs.org/)**: Necessário para rodar o ambiente de desenvolvimento do frontend (Vite).
3.  **Git**: Para gerenciar as versões do código.
4.  **VS Code** ou **Visual Studio 2022**: Recomendados para edição de código.

---

## 📂 2. Estrutura do Projeto

*   `/backend`: API e serviços em C# (.NET 8) que gerenciam entrada, áudio e rede.
*   `/frontend`: Interface visual em React (Vite) com design Premium.

---

## 🚀 3. Rodando em Modo de Desenvolvimento

Para rodar o Nicodemous localmente, você precisa de **dois terminais** abertos.

### Passo A: Frontend (Reload Instantâneo)
Abra o primeiro terminal no diretório raiz e rode:
```bash
cd frontend
npm install
npm run dev
```
O frontend ficará disponível em `http://localhost:5173`. O backend já está configurado para ler esta URL em modo de depuração.

### Passo B: Backend (O "Cérebro")
Abra o segundo terminal no diretório raiz e rode:
```bash
cd backend
dotnet run
```
*   **No Windows**: O comando usará automaticamente o perfil de Windows para habilitar captura de áudio e simulação de entrada.
*   **No macOS**: O comando rodará a versão multiplataforma otimizada.

---

## 🏗️ 4. Build de Produção Local

Se você quiser gerar o executável final na sua máquina sem usar o GitHub:

### Para Windows (.exe standalone):
```bash
dotnet publish backend/nicodemous_backend.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -f net8.0-windows
```

### Para macOS (Binário nativo):
```bash
dotnet publish backend/nicodemous_backend.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -f net8.0
```
O arquivo final estará na pasta `backend/bin/Release/.../publish`.

---

## 🍎 5. Notas Específicas para macOS (Permissões)

Para que o Nicodemous consiga injetar movimentos de mouse e teclado no Mac, você **precisa** conceder permissões de acessibilidade:

1. Vá em **Ajustes do Sistema** > **Privacidade e Segurança** > **Acessibilidade**.
2. Clique no ícone de `+` e adicione o seu Terminal (ex: `iTerm` ou `Terminal`) ou o executável do `Nicodemous`.
3. Certifique-se de que a chave está **Ativada**.

> [!IMPORTANT]
> Sem essa permissão, o sistema de controle remoto não funcionará no macOS devido às proteções de segurança nativas da Apple.

---

## 🔍 6. Dicas de Debug e Logs

1.  **Console do Chrome**: Como a interface é baseada em Photino, você pode clicar com o botão direito na janela do app e selecionar "Inspecionar" para ver os logs do React.
2.  **Logs do Terminal**: O backend imprime logs de descoberta e conexão diretamente no terminal onde você rodou o `dotnet run`.
3.  **Portas de Rede**: O app utiliza a porta **8888** para descoberta (UDP) e portas aleatórias para os streams. Certifique-se de que seu Firewall não está bloqueando o binário.

---
*Nicodemous — Universal Control Project.*
