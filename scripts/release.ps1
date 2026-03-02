$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path -Parent $ScriptDir
$ReleaseName = "nicodemouse-windows-x64"
$OutputDir = Join-Path $RootDir "releases\$ReleaseName"

Write-Host "[RELEASE] Cleaning up..." -ForegroundColor Cyan
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "[RELEASE] Building Frontend (React)..." -ForegroundColor Cyan
Set-Location (Join-Path $RootDir "frontend")
npm run build
Write-Host "[RELEASE] Prepping wwwroot..." -ForegroundColor Cyan
Write-Host "-> Skipped. Web resources are now embedded at compile time directly from ../frontend/dist" -ForegroundColor Green

Write-Host "[RELEASE] Building Backend for Windows (Self-Contained)..." -ForegroundColor Cyan
Set-Location (Join-Path $RootDir "backend")
# Publish as a single, self-contained executable for Windows x64 without creating debug symbols
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o "$OutputDir"

Write-Host "[RELEASE] Removing PDB..." -ForegroundColor Cyan
Set-Location "$OutputDir"
# Remove debug file if it still got generated somehow
Remove-Item -Path "*.pdb" -Force -ErrorAction SilentlyContinue

Write-Host "[RELEASE] Packaging WIN ZIP..." -ForegroundColor Cyan
Set-Location (Join-Path $RootDir "releases")
$TimeStamp = Get-Date -Format "yyyyMMdd_HHmmss"
$WinZipPath = "$ReleaseName`_$TimeStamp.zip"
Get-ChildItem -Path "$ReleaseName\*" -Force | Compress-Archive -DestinationPath $WinZipPath -Force

# --- MAC BUILD ---
$MacReleaseName = "nicodemouse-macos-universal"
$MacOutputDir = Join-Path $RootDir "releases\$MacReleaseName"
$MacAppDir = Join-Path $MacOutputDir "nicodemouse.app"
$MacContentsDir = Join-Path $MacAppDir "Contents"
$MacosDir = Join-Path $MacContentsDir "MacOS"
$MacResourcesDir = Join-Path $MacContentsDir "Resources"

Write-Host "`n[RELEASE MAC] Cleaning up..." -ForegroundColor Cyan
if (Test-Path $MacOutputDir) { Remove-Item -Recurse -Force $MacOutputDir }
New-Item -ItemType Directory -Force -Path $MacosDir | Out-Null
New-Item -ItemType Directory -Force -Path $MacResourcesDir | Out-Null

Write-Host "[RELEASE MAC] Building Backend for macOS (Self-Contained)..." -ForegroundColor Cyan
Set-Location (Join-Path $RootDir "backend")
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $MacosDir

# Rename executable to match app name
if (Test-Path "$MacosDir\nicodemouse.Backend") { Rename-Item "$MacosDir\nicodemouse.Backend" "nicodemouse" }
if (Test-Path "$MacosDir\nicodemouse_backend") { Rename-Item "$MacosDir\nicodemouse_backend" "nicodemouse" }

Write-Host "[RELEASE MAC] Creating macOS App Bundle..." -ForegroundColor Cyan
$PlistContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>nicodemouse</string>
    <key>CFBundleIdentifier</key>
    <string>com.rodrigod3v.nicodemouse</string>
    <key>CFBundleName</key>
    <string>nicodemouse</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
</dict>
</plist>
"@
Set-Content -Path "$MacContentsDir\Info.plist" -Value $PlistContent -Encoding UTF8

$IconPath = Join-Path $RootDir "backend\Assets\logo_n.png"
if (Test-Path $IconPath) { Copy-Item $IconPath "$MacResourcesDir\AppIcon.icns" }

Write-Host "[RELEASE MAC] Packaging MAC ZIP..." -ForegroundColor Cyan
Set-Location (Join-Path $RootDir "releases")
$TimeStamp = Get-Date -Format "yyyyMMdd_HHmmss"
$MacZipPath = "$MacReleaseName`_$TimeStamp.zip"
Compress-Archive -Path "$MacReleaseName\nicodemouse.app" -DestinationPath $MacZipPath -Force

Write-Host "----------------------------------------------------" -ForegroundColor Green
Write-Host "ALL RELEASES COMPLETED:" -ForegroundColor Green
Write-Host "- releases/$WinZipPath" -ForegroundColor Green
Write-Host "- releases/$MacZipPath" -ForegroundColor Green
Write-Host "----------------------------------------------------" -ForegroundColor Green

# Return to scripts directory
Set-Location $ScriptDir
