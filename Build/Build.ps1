# ScreenNap Build Script

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

# Build folder is the current location, solution root is parent
$BuildDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = Split-Path -Parent $BuildDir
Set-Location $SolutionDir

$Framework = "net10.0-windows"
$OutputDir = Join-Path $BuildDir "ScreenNap"
$PublishDir = Join-Path $BuildDir "publish_temp"
$ProjectPath = "ScreenNap\ScreenNap.csproj"

Write-Host "`n=== Building ScreenNap ===" -ForegroundColor Green

if (-not (Test-Path $ProjectPath)) {
    Write-Host "   [ERROR] ScreenNap project not found at $ProjectPath" -ForegroundColor Red
    exit 1
}

# Cleanup temp folder
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
# Clean existing output folder
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }

# Create output folder
New-Item -ItemType Directory -Path $OutputDir -Force -ErrorAction SilentlyContinue | Out-Null

Write-Host "Building ScreenNap..." -ForegroundColor Cyan
dotnet publish $ProjectPath `
    -c $Configuration `
    -f $Framework `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebuggerSupport=false `
    -o "$PublishDir"

if ($LASTEXITCODE -eq 0) {
    Copy-Item "$PublishDir\*" "$OutputDir\" -Force
    Remove-Item $PublishDir -Recurse -Force

    Write-Host "   [OK] ScreenNap.exe deployed" -ForegroundColor Green
    Write-Host "`n   Output: $OutputDir" -ForegroundColor Cyan
    Write-Host "`n=== Build Completed Successfully ===" -ForegroundColor Green
    exit 0
} else {
    Write-Host "   [ERROR] ScreenNap build failed" -ForegroundColor Red
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    Write-Host "`n=== Build Failed ===" -ForegroundColor Red
    exit 1
}
