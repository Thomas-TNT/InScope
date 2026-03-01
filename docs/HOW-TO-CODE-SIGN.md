# How to Code Sign InScope (Fix SmartScreen Warning)

Microsoft Defender SmartScreen shows "unrecognized app" when the executable is not signed with a valid code signing certificate. To remove this warning, you must sign the application.

## Overview

| Option | Cost | SmartScreen result | Effort |
|--------|------|--------------------|--------|
| No signing | Free | Warning every run | None |
| Standard code signing cert | ~$75–300/year | Warning until reputation builds | Medium |
| EV (Extended Validation) code signing cert | ~$300–500/year | Often trusted immediately | Higher (hardware token) |

## Step 1: Obtain a Code Signing Certificate

### Standard code signing (OV)

- Purchase from: DigiCert, Sectigo, SSL.com, etc.
- Identity verification takes 1–5 days.
- After signing: SmartScreen may show a warning for days or weeks until "reputation" builds from enough downloads.

### EV code signing (recommended for faster trust)

- Same providers; requires stricter verification and a USB hardware token.
- Setup: 1–2 weeks.
- SmartScreen often trusts immediately or within days.

## Step 2: Sign the Installer

Once you have the certificate installed (or the token plugged in):

### Using signtool (Windows SDK)

```powershell
# Locate signtool (comes with Windows SDK)
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"

# Sign the installer (adjust paths and cert as needed)
& $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 `
  /f "path\to\your-certificate.pfx" `
  /p "your-pfx-password" `
  "dist\InScope-Setup.exe"
```

### Common options

- `/tr` — Timestamp server (needed so the signature stays valid after the cert expires).
- `/td sha256 /fd sha256` — Use SHA-256.
- `/f` — PFX file (or use `/n "Subject Name"` for a cert in the store).

## Step 3: Add Signing to the Release Workflow (Optional)

To sign automatically in GitHub Actions, you would:

1. Export your cert as a PFX and store it (base64) in a GitHub secret.
2. Store the PFX password in another secret.
3. Add a signing step after "Build installer" in [.github/workflows/release.yml](.github/workflows/release.yml).

Example step (you must supply the certificate and secrets):

```yaml
      - name: Sign installer
        run: |
          $pfxB64 = "${{ secrets.CODE_SIGN_PFX_BASE64 }}"
          $pfxBytes = [Convert]::FromBase64String($pfxB64)
          [IO.File]::WriteAllBytes("cert.pfx", $pfxBytes)
          & "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign `
            /tr http://timestamp.digicert.com /td sha256 /fd sha256 `
            /f cert.pfx /p "${{ secrets.CODE_SIGN_PFX_PASSWORD }}" `
            dist/InScope-Setup.exe
        shell: pwsh
```

**Security:** Only use GitHub secrets; never commit the PFX or password. Restrict access to those secrets.

## Workaround Without a Certificate

If you cannot purchase a cert yet:

1. On the SmartScreen warning, click **More info**.
2. Click **Run anyway**.

Users can run the app this way, but they must do it for every new download until the app is signed (or reputation builds, which is slow for unsigned apps).

## Links

- [Microsoft: Sign your code](https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [DigiCert code signing](https://www.digicert.com/signing/code-signing-certificates)
- [Sectigo code signing](https://sectigo.com/ssl-certificates-tls/code-signing)
