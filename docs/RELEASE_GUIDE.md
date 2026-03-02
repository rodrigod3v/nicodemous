# 📦 nicodemouse Release Guide

This document outlines the process for building and packaging `nicodemouse` for Windows and macOS.

## Overview
`nicodemouse` uses **.NET 8** and **Photino** for the backend, and **Vite + React** for the frontend.
Because Photino relies on the operating system's native web controls (WebView2 on Windows, WebKit on macOS), **no additional installers or dependencies are required for the end user.** The application is built as a **self-contained** executable.

---

## 🪟 Windows & 🍎 macOS Release

We now have a unified build script that generates both the Windows Executable and the macOS `.app` bundle straight from Windows!

### 1. Prerequisites
- [Node.js](https://nodejs.org/) (for building the React frontend)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PowerShell

### 2. Build Process
Run the automated PowerShell script from the `scripts` directory:

```powershell
cd scripts
.\release.ps1
```

### 3. Output
The script will output two timestamped `.zip` files in the `releases/` folder (e.g., `releases/nicodemouse-windows-x64_20260226_220430.zip` and `releases/nicodemouse-macos-universal_20260226_220443.zip`).

- **Windows ZIP contains:**
  - `nicodemouse.exe` (Self-contained executable)
  - `wwwroot/` folder and `Assets/` (Marked as Hidden)
- **macOS ZIP contains:**
  - `nicodemouse.app` (macOS Application Bundle automatically structured with `Info.plist` and Icons)

Users simply extract the ZIPs.

### 4. macOS Permissions (Important)
Because macOS strictly controls input monitoring and disk access, users must grant `nicodemouse` permissions. **Please refer to `MACOS_SETUP.md`** for the specific instructions you should provide to users regarding Accessibility and Full Disk Access.

---

## 🛠️ Modifying the Build
If you add new dependencies to the `.csproj` or modify the Photino initialization in `Program.cs`, ensure that:
1. Native libraries are handled correctly (the build scripts use `-p:IncludeNativeLibrariesForSelfExtract=true`).
2. The `wwwroot` directory is correctly copied alongside the `.exe` (Windows) or inside the `.app/Contents/MacOS` directory (macOS).

---

## 🚀 Publishing to GitHub

To make these binaries available to your users, you need to create a GitHub Release:

1. Go to your repository on GitHub (e.g., `https://github.com/rodrigod3v/nicodemous`).
2. On the right side menu, look for the **Releases** section and click on it.
3. Click the **"Draft a new release"** button.
4. Click **"Choose a tag"** and type a version number (like `v1.0.0`), then select "Create new tag: v1.0.0 on publish".
5. Set the **Release title** (e.g., `v1.0.0 - Initial Release`).
6. Add a description of what is included in this version.
7. Under the **"Attach binaries by dropping them here or selecting them"** box, upload the generated ZIP files:
   - `releases/nicodemouse-windows-x64.zip`
   - `releases/nicodemouse-macos-universal.zip`
8. Click **"Publish release"**.

Users can now download the `.zip` files directly from your GitHub repository!
