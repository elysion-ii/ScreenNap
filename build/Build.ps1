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

if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }

New-Item -ItemType Directory -Path $OutputDir -Force -ErrorAction SilentlyContinue | Out-Null

# Only the placeholder template of a configuration file belongs in the repository:
# the real file carries credentials and environment-specific values
# (rule: docs/rules/dotnet.md, CONFIGFILE)
Write-Host "Verifying configuration files..." -ForegroundColor Cyan
# Configuration extensions only, so a source-code template never trips the check
$ConfigTemplate = '\.template\.(json|ya?ml|xml|ini|config|toml|env)$'
$TrackedFiles = @()
if (Get-Command git -ErrorAction SilentlyContinue) {
    $TrackedFiles = @(git ls-files 2>$null)
    if ($LASTEXITCODE -ne 0) { $TrackedFiles = @() }
}
foreach ($Template in $TrackedFiles | Where-Object { $_ -match $ConfigTemplate }) {
    $RealName = $Template -replace $ConfigTemplate, '.$1'
    if ($TrackedFiles -contains $RealName) {
        Write-Host "   [ERROR] $RealName is tracked by git - only $Template belongs in the repository" -ForegroundColor Red
        Write-Host "           Run 'git rm --cached $RealName' and add it to .gitignore" -ForegroundColor Red
        Write-Host "`n=== Build Failed ===" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Verifying code format..." -ForegroundColor Cyan
dotnet format ScreenNap.slnx --verify-no-changes
if ($LASTEXITCODE -ne 0) {
    Write-Host "   [ERROR] Unformatted code detected - run 'dotnet format ScreenNap.slnx' and rebuild" -ForegroundColor Red
    Write-Host "`n=== Build Failed ===" -ForegroundColor Red
    exit 1
}

Write-Host "Running ScreenNap tests..." -ForegroundColor Cyan
dotnet test ScreenNap.Tests\ScreenNap.Tests.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "   [ERROR] Tests failed - build aborted" -ForegroundColor Red
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    Write-Host "`n=== Build Failed ===" -ForegroundColor Red
    exit 1
}

Write-Host "Building ScreenNap..." -ForegroundColor Cyan
dotnet publish $ProjectPath `
    -c $Configuration `
    -f $Framework `
    -r $Runtime `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:DebuggerSupport=false `
    -o "$PublishDir"

if ($LASTEXITCODE -eq 0) {
    Copy-Item "$PublishDir\*" "$OutputDir\" -Force -Recurse
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
