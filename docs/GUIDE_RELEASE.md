# 🚀 nicodemouse: Guia de Release & Distribuição

Este documento descreve como gerenciar o ciclo de vida do nicodemouse, desde a codificação até a entrega automática dos binários (`.exe` e `.app`) para os usuários finais.

---

## 🛠️ 1. Como Funciona a Automação (CI/CD)

Configuramos uma **Pipeline de CI/CD** no GitHub Actions que elimina a necessidade de compilar o código manualmente.

- **Build Contínuo**: Sempre que você faz um `git push` nas branches `main` ou `server/client`, o GitHub verifica se o código compila corretamente para Windows e Mac.
- **Release Automático**: Quando você cria uma **Tag de Versão** (como `v1.0.0`), a pipeline:
    1. Compila o backend em modo **Release**.
    2. Empacota tudo em um único binário standalone (independente).
    3. Cria uma página de "Release" no seu repositório GitHub.
    4. Sobe os arquivos `.exe` (Windows) e o binário do Mac para lá.

---

## 📦 2. Passo a Passo para Lançar uma Versão Nova

Sempre que você estiver feliz com as mudanças e quiser que o "Botão de Download" no site ofereça a versão nova, siga estes passos no terminal:

### Passo A: Envie as alterações para o GitHub
```bash
git add .
git commit -m "feat: descrição da nova funcionalidade"
git push origin server/client
```

### Passo B: Crie uma Tag de Versão
As tags dizem ao GitHub: "Este ponto específico do código é uma versão oficial".
```bash
# Crie a tag (use v1.0, v1.1, etc)
git tag -a v1.0.0 -m "Versão 1.0.0: Implementação de Protocolo Binário"

# Envie a tag para o GitHub
git push origin v1.0.0
```

---

## 🌐 3. Atualização no "Site" (Dashboard)

Você não precisa mexer no código do Dashboard para atualizar o link de download.

- O botão **"Download Latest App"** no Dashboard aponta para: `https://github.com/rodrigod3v/nicodemouse/releases/latest`.
- O GitHub redireciona esse link automaticamente para a **Tag mais recente** que você criou.
- **Resultado**: Assim que a Action de Build terminar (leva cerca de 2-3 minutos), qualquer pessoa que clicar no botão já baixará a versão nova.

---

## 🖥️ 4. Como Testar / Ver o Progresso

1. Vá até o seu repositório no navegador: `github.com/rodrigod3v/nicodemouse`.
2. Clique na aba **Actions**. Lá você verá o progresso do build (ícone amarelo = rodando, verde = sucesso).
3. Quando terminar, os arquivos estarão na aba **Releases** (no lado direito da página inicial do repositório).

---

## ⚠️ 5. FAQ / Solução de Problemas

**Q: Criei a tag mas o arquivo não apareceu no Release.**
R: Verifique a aba **Actions**. Se o build falhar (ícone vermelho), o release não será criado. Geralmente é algum erro de sintaxe no código ou falta de dependência.

**Q: Posso deletar uma versão lançada?**
R: Sim. Vá em "Releases" no GitHub, clique em "Edit" e depois em "Delete". Lembre-se de deletar a tag localmente também com `git tag -d v1.0.0`.

**Q: Como altero o nome dos arquivos gerados?**
R: Isso é controlado no arquivo `.github/workflows/main.yml`.

---
*Documentação gerada para nicodemouse — Universal Control.*
