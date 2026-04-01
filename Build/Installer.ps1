# ScreenNap Installer Build Script

$BuildDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $BuildDir

# Check Inno Setup installation
$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $IsccPath)) {
    Write-Host "   [ERROR] Inno Setup not found" -ForegroundColor Red
    Write-Host "   Install Inno Setup 6 or later from https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

# Create Installer output folder
$InstallerDir = Join-Path $BuildDir "Installer"
if (-not (Test-Path $InstallerDir)) {
    New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null
}

Write-Host "`n=== Building ScreenNap Installer ===" -ForegroundColor Green

# Check if build output exists
$ExePath = Join-Path $BuildDir "ScreenNap\ScreenNap.exe"
if (-not (Test-Path $ExePath)) {
    Write-Host "   [ERROR] ScreenNap.exe not found in Build\ScreenNap\" -ForegroundColor Red
    Write-Host "   Run 'Build' (option 1) first" -ForegroundColor Yellow
    exit 1
}

# Build installer
Write-Host "Building installer..." -ForegroundColor Cyan
$SetupScriptPath = Join-Path $BuildDir "Setup_ScreenNap.iss"

if (-not (Test-Path $SetupScriptPath)) {
    Write-Host "   [ERROR] Setup script 'Setup_ScreenNap.iss' not found." -ForegroundColor Red
    exit 1
}

& $IsccPath $SetupScriptPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Installer created successfully" -ForegroundColor Green
    Write-Host "`n=== Installer Build Completed ===" -ForegroundColor Green
    Write-Host "`nOutput location: Build\Installer\" -ForegroundColor Cyan
    Write-Host "   - ScreenNap-Setup-1.0.0.exe" -ForegroundColor White
    exit 0
} else {
    Write-Host "   [ERROR] Installer build failed" -ForegroundColor Red
    Write-Host "`n=== Installer Build Failed ===" -ForegroundColor Red
    exit 1
}
