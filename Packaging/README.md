# MSIX Packaging

## Building Locally

Prerequisites: Windows SDK

```powershell
cd Packaging
.\build-msix.ps1 -Version 1.0.0 -CertThumbprint (Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=Carbonix" }).Thumbprint
```

This script handles publishing, PRI generation, packaging, and signing.

## Regenerating Assets

The logo source is `logo.svg` (`logo-wide.svg` embeds `logo.svg` via a link). If the logo changes, simply edit `logo.svg` in Inkscape and regenerate the PNG assets from the SVG source:

```powershell
cd Packaging
.\generate-assets.ps1  # Requires Inkscape
git add Assets/
git commit -m "chore: regenerate MSIX assets"
```

The `Assets/` folder is committed to the repo so CI doesn't need Inkscape.

## Files

- `Package.appxmanifest` - MSIX manifest (identity, capabilities, visual assets)
- `SITLLauncher.appinstaller` - App Installer file for auto-updates
- `build-msix.ps1` - Local build script
- `generate-assets.ps1` - Asset generation from SVG
- `Assets/` - PNG icons in required sizes

## Release Process

1. Create a new GitHub release with the version tag (e.g. `v1.0.0`)
2. GitHub Actions will build and publish the MSIX package to GitHub Releases and update the App Installer page on GitHub Pages
3. Users can install/update via the [App Installer URL](https://carbonixuav.github.io/SITL-Launcher/SITLLauncher.appinstaller)
