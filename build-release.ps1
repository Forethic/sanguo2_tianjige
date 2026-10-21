param(
    [string]$Version = "v1.0.0"
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$distDir = Join-Path $root "dist"
$targetDir = Join-Path $distDir "Tianjige-$Version-win-x64"
$zipPath = Join-Path $distDir "Tianjige-$Version-win-x64.zip"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Tianjige Release Packager - Win-x64 Self-Contained" -ForegroundColor Cyan
Write-Host "   Version: $Version" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean previous build
if (Test-Path $targetDir) {
    Write-Host "[1/5] Cleaning target directory: $targetDir" -ForegroundColor Yellow
    Remove-Item -Recurse -Force $targetDir
}
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

# 2. Publish self-contained executable with embedded .NET 9 desktop runtime
Write-Host "[2/5] Publishing self-contained single-file binary..." -ForegroundColor Green
$publishArgs = @(
    "publish",
    "$root/src/Tianjige.UI/Tianjige.UI.csproj",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    "-o", $targetDir
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# 3. Ensure data directory
Write-Host "[3/5] Verifying data and assets..." -ForegroundColor Green
$exePath = Join-Path $targetDir "Tianjige.exe"
$dataPath = Join-Path $targetDir "data"

if (-not (Test-Path $exePath)) {
    Write-Error "Tianjige.exe not found at $exePath"
    exit 1
}

if (-not (Test-Path $dataPath)) {
    Write-Host "Copying data folder..." -ForegroundColor Yellow
    Copy-Item -Recurse -Force "$root/data" $dataPath
}

# 4. Copy Release Guide
Write-Host "[4/5] Copying release guide..." -ForegroundColor Green
$readmeSrc = Join-Path $root "README_RELEASE.txt"
if (Test-Path $readmeSrc) {
    Copy-Item -Force $readmeSrc (Join-Path $targetDir "使用说明.txt")
}

# 5. Compress to ZIP archive
Write-Host "[5/5] Compressing to $zipPath..." -ForegroundColor Green
Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -Force

$zipItem = Get-Item $zipPath
$zipSizeMb = [math]::Round($zipItem.Length / 1MB, 2)
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   SUCCESS: Release package created!" -ForegroundColor Green
Write-Host "   Archive: $zipPath ($zipSizeMb MB)" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
