# Generate MSIX package assets from SVG source
# Requires: Inkscape (searches common install locations)
# Run: .\generate-assets.ps1
# After running, commit the generated Assets/ folder

$ErrorActionPreference = "Stop"

$sourceSvg = "$PSScriptRoot\logo.svg"
$assetsDir = "$PSScriptRoot\Assets"

# Find Inkscape
$inkscapePaths = @(
    "$env:ProgramFiles\Inkscape\bin\inkscape.exe"
    "${env:ProgramFiles(x86)}\Inkscape\bin\inkscape.exe"
    "$env:LocalAppData\Programs\Inkscape\bin\inkscape.exe"
)

$inkscape = $inkscapePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $inkscape) {
    Write-Error "Inkscape not found. Searched:`n  $($inkscapePaths -join "`n  ")"
}

Write-Host "Using Inkscape: $inkscape" -ForegroundColor Cyan

if (Test-Path $assetsDir) {
    Remove-Item "$assetsDir\*.png" -Force
} else {
    New-Item -ItemType Directory -Path $assetsDir | Out-Null
}

# MSIX required asset sizes
$assets = @(
    @{ Name = "StoreLogo.png"; Size = 50 }
    @{ Name = "Square44x44Logo.png"; Size = 44 }
    @{ Name = "Square71x71Logo.png"; Size = 71 }
    @{ Name = "Square150x150Logo.png"; Size = 150 }
    @{ Name = "Square310x310Logo.png"; Size = 310 }
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150 }
)

# Target-size assets for taskbar (unplated, true transparency)
# 44 is critical - it's the exact size Windows uses for taskbar
$targetSizes = @(16, 24, 32, 44, 48, 256)

Write-Host "Generating MSIX assets from $sourceSvg"

foreach ($asset in $assets) {
    $output = Join-Path $assetsDir $asset.Name

    if ($asset.Width) {
        # Wide logo - uses separate wide SVG with proper padding
        $wideSvg = "$PSScriptRoot\logo-wide.svg"
        $w = $asset.Width
        $h = $asset.Height
        & $inkscape $wideSvg --export-filename="$output" --export-width=$w --export-height=$h --export-background-opacity=0 2>$null
    } else {
        # Square logo
        $s = $asset.Size
        & $inkscape $sourceSvg --export-filename="$output" --export-width=$s --export-height=$s --export-background-opacity=0 2>$null
    }

    $size = if ($asset.Width) { "$($asset.Width)x$($asset.Height)" } else { "$($asset.Size)x$($asset.Size)" }
    Write-Host "  Created: $($asset.Name) ($size)"
}

# Generate target-size assets (unplated icons for taskbar with true transparency)
foreach ($size in $targetSizes) {
    $output = Join-Path $assetsDir "Square44x44Logo.targetsize-${size}_altform-unplated.png"
    & $inkscape $sourceSvg --export-filename=$output --export-width=$size --export-height=$size --export-background-opacity=0 2>$null
    Write-Host "  Created: Square44x44Logo.targetsize-${size}_altform-unplated.png"
}

Write-Host "`nDone! Assets generated in $assetsDir" -ForegroundColor Green
Write-Host "Remember to commit these files - they are not generated in CI." -ForegroundColor Yellow
