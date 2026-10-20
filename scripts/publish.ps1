# Sango2 Tianjige - Single File Release Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "======================================================" -ForegroundColor DarkYellow
Write-Host "     《群英策·天机阁》 单文件便携版自动化打包构建     " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor DarkYellow

$rootDir = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $rootDir "src\Tianjige.UI\Tianjige.UI.csproj"
$distDir = Join-Path $rootDir "dist\Tianjige"

Write-Host ">>> 清理旧构建目录: $distDir" -ForegroundColor Gray
if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}

Write-Host ">>> 开始执行 dotnet publish (win-x64, PublishSingleFile=true)..." -ForegroundColor Yellow

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -o $distDir

if ($LASTEXITCODE -eq 0) {
    $exePath = Join-Path $distDir "Tianjige.exe"
    $dataPath = Join-Path $rootDir "data"
    $distDataPath = Join-Path $distDir "data"
    if (Test-Path $dataPath) {
        if (-not (Test-Path $distDataPath)) {
            New-Item -Path $distDataPath -ItemType Directory -Force | Out-Null
        }
        Copy-Item -Path "$dataPath\*" -Destination $distDataPath -Recurse -Force
        Write-Host ">>> 已同步外置典籍数据目录: $distDataPath" -ForegroundColor Cyan
    }
    if (Test-Path $exePath) {
        $sizeMB = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
        Write-Host ">>> 构建发布大吉！产物: $exePath ($sizeMB MB)" -ForegroundColor Green
    }
} else {
    Write-Error ">>> 构建失败，退出码: $LASTEXITCODE"
}
