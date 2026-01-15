# MSIX Packaging

## For Maintainers: Creating the Signing Certificate

Generate a self-signed certificate for code signing:

```powershell
# Create certificate (run as Administrator)
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=Carbonix" `
    -KeyUsage DigitalSignature `
    -FriendlyName "Carbonix Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

# Export as PFX with password
$password = ConvertTo-SecureString -String "YOUR_PASSWORD_HERE" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "Carbonix.pfx" -Password $password

# Export public certificate (for distribution to users)
Export-Certificate -Cert $cert -FilePath "Carbonix.cer"

# Base64 encode PFX for GitHub secret
[Convert]::ToBase64String([IO.File]::ReadAllBytes("Carbonix.pfx")) | Set-Clipboard
```

Add these GitHub secrets:

- `SIGNING_CERTIFICATE_BASE64`: The base64-encoded PFX (copied to clipboard above)
- `SIGNING_CERTIFICATE_PASSWORD`: The password used when creating the PFX

## For Users: Installing the Certificate

Before installing the MSIX package, you need to trust the Carbonix signing certificate.

### Option 1: PowerShell (Recommended)

Run PowerShell as Administrator:

```powershell
# Import the certificate to Trusted People store
Import-Certificate -FilePath "Carbonix.cer" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
```

### Option 2: GUI

1. Double-click `Carbonix.cer`
2. Click "Install Certificate..."
3. Select "Local Machine" and click Next
4. Select "Place all certificates in the following store"
5. Click Browse and select "Trusted People"
6. Click Next, then Finish

### Installing the App

After trusting the certificate, double-click the `.msix` file to install.

## Building Locally

Prerequisites: Windows SDK

```powershell
cd Packaging
.\build-msix.ps1 -Version 1.0.0 -CertPath path\to\Carbonix.pfx -CertPassword yourpassword
```

This script handles publishing, PRI generation, packaging, and signing.

## Regenerating Assets

If the logo changes, regenerate the PNG assets from the SVG source:

```powershell
cd Packaging
.\generate-assets.ps1  # Requires Inkscape
git add Assets/
git commit -m "chore: regenerate MSIX assets"
```

The `Assets/` folder is committed to the repo so CI doesn't need Inkscape.

## Release Process

1. Tag a release: `git tag v1.0.0 && git push --tags`
2. GitHub Actions will build, sign, and create a release with the MSIX attached
3. Distribute `Carbonix.cer` to new users (one-time setup)
