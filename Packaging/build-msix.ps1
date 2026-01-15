# Build MSIX package locally
# Usage: .\build-msix.ps1 [-Version 1.0.0] [-CertPath path\to\cert.pfx] [-CertPassword password]

param(
    [string]$Version = "1.0.0",
    [string]$CertPath,
    [string]$CertPassword
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path "$PSScriptRoot\.."

# Find Windows SDK tools
$sdkRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
if (-not (Test-Path $sdkRoot)) {
    Write-Error "Windows SDK not found at $sdkRoot. Install Windows SDK from https://developer.microsoft.com/windows/downloads/windows-sdk/"
}

$sdkVersions = Get-ChildItem $sdkRoot -Directory | Where-Object { $_.Name -match "^10\.\d+\.\d+\.\d+$" } | Sort-Object Name -Descending
if (-not $sdkVersions) {
    Write-Error "No SDK versions found in $sdkRoot"
}

$sdkBin = Join-Path $sdkVersions[0].FullName "x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
$signtool = Join-Path $sdkBin "signtool.exe"
$makepri = Join-Path $sdkBin "makepri.exe"

if (-not (Test-Path $makeappx)) {
    Write-Error "makeappx.exe not found at $makeappx"
}

Write-Host "Using SDK: $($sdkVersions[0].Name)" -ForegroundColor Cyan

# Paths
$publishDir = Join-Path $repoRoot "publish"
$layoutDir = Join-Path $repoRoot "msix-layout"
$msixVersion = "$Version.0"
$msixPath = Join-Path $repoRoot "SITLLauncher-$Version.msix"

# Clean previous build
if (Test-Path $layoutDir) { Remove-Item $layoutDir -Recurse -Force }
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }

# Publish application
Write-Host "`nPublishing application..." -ForegroundColor Cyan
dotnet publish "$repoRoot\src\SITLLauncher.Desktop\SITLLauncher.Desktop.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Check assets exist
$assetsDir = Join-Path $PSScriptRoot "Assets"
if (-not (Test-Path "$assetsDir\Square150x150Logo.png")) {
    Write-Host "`nGenerating MSIX assets..." -ForegroundColor Cyan
    & "$PSScriptRoot\generate-assets.ps1"
}

# Create layout directory
Write-Host "`nCreating package layout..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $layoutDir -Force | Out-Null

# Copy application files
Copy-Item -Path "$publishDir\*" -Destination $layoutDir -Recurse

# Copy assets
Copy-Item -Path $assetsDir -Destination $layoutDir -Recurse

# Copy and update manifest
$manifest = Get-Content "$PSScriptRoot\Package.appxmanifest" -Raw
$manifest = $manifest -replace 'Version="1\.0\.0\.0"', "Version=`"$msixVersion`""
Set-Content "$layoutDir\AppxManifest.xml" $manifest

# Generate resources.pri for asset discovery (required for unplated icons)
Write-Host "`nGenerating resources.pri..." -ForegroundColor Cyan
$priConfig = Join-Path $layoutDir "priconfig.xml"
$priFile = Join-Path $layoutDir "resources.pri"

# Create MakePri config
& $makepri createconfig /cf $priConfig /dq en-US /o
if ($LASTEXITCODE -ne 0) { Write-Error "makepri createconfig failed" }

# Generate PRI file
& $makepri new /pr $layoutDir /cf $priConfig /of $priFile /o
if ($LASTEXITCODE -ne 0) { Write-Error "makepri new failed" }

# Clean up config file
Remove-Item $priConfig -Force

# Create MSIX
Write-Host "`nCreating MSIX package..." -ForegroundColor Cyan
& $makeappx pack /d $layoutDir /p $msixPath /o

if ($LASTEXITCODE -ne 0) {
    Write-Error "makeappx failed"
}

# Sign if certificate provided
if ($CertPath) {
    if (-not (Test-Path $CertPath)) {
        Write-Error "Certificate not found: $CertPath"
    }

    Write-Host "`nSigning package..." -ForegroundColor Cyan

    $signArgs = @("sign", "/fd", "SHA256", "/f", $CertPath)
    if ($CertPassword) {
        $signArgs += @("/p", $CertPassword)
    }
    $signArgs += $msixPath

    & $signtool @signArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "signtool failed"
    }

    Write-Host "`nPackage signed successfully" -ForegroundColor Green
} else {
    Write-Host "`nSkipping signing (no certificate provided)" -ForegroundColor Yellow
    Write-Host "To sign: .\build-msix.ps1 -CertPath path\to\cert.pfx -CertPassword yourpassword"
}

# Clean up
Remove-Item $layoutDir -Recurse -Force

Write-Host "`nOutput: $msixPath" -ForegroundColor Green
