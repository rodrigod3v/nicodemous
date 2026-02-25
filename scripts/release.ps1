# Nicodemouse Local App Release Script (Windows)

$ReleaseName = "nicodemouse-release"
$OutputDir = "..\releases\$ReleaseName"

echo "[RELEASE] Cleaning up..."
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
mkdir $OutputDir

echo "[RELEASE] Building Backend..."
cd backend
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OutputDir/bin"

echo "[RELEASE] Copying Frontend Assets..."
# Photino looks for wwwroot by default in published apps
mkdir "$OutputDir/bin/wwwroot"
cd ../frontend
npm run build
cp -r dist/* "$OutputDir/bin/wwwroot/"

echo "[RELEASE] Packaging..."
cd "$OutputDir/.."
Compress-Archive -Path "$ReleaseName" -DestinationPath "$ReleaseName.zip" -Force

echo "----------------------------------------------------"
echo "RELEASE COMPLETED: releases/$ReleaseName.zip"
echo "This zip contains everything needed to run nicodemouse."
echo "----------------------------------------------------"
