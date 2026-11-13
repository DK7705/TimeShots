# TimeShots

TimeShots is a Windows screenshot utility with timestamp overlays and a local,
tamper-evident capture chain.

## Current implementation

- Windows desktop app built with .NET 8, WPF, and Windows Forms.
- Full-screen, active-window, and selected-region capture.
- PNG captures with a UTC/local timestamp and sequence overlay.
- JSON sidecars with image SHA-256, hash-chain linkage, and HMAC-SHA256 signature.
- Local capture gallery with image, chain-formula, timestamp, and signature checks.
- Tray controls and global shortcuts for capture and gallery access.
- `timeshots` CLI for single-capture/chain verification and signing-key generation.

The app uses the local system clock. NTP synchronization, cross-platform builds,
export archives, and installers are not implemented yet.

## Requirements

- Windows
- .NET 8 SDK

## Build and run

```powershell
dotnet build TimeShots.sln
dotnet run --project src/TimeShots.App/TimeShots.App.csproj
```

The app stores settings, its signing key, and captures under
`%LOCALAPPDATA%\TimeShots` by default. The capture folder and overlay style can
be changed in Settings.

## CLI

Build the CLI:

```powershell
dotnet build src/TimeShots.Cli/TimeShots.Cli.csproj
```

Generate a signing key and verify a capture:

```powershell
dotnet run --project src/TimeShots.Cli/TimeShots.Cli.csproj -- keygen --out signing.key
dotnet run --project src/TimeShots.Cli/TimeShots.Cli.csproj -- verify capture.png --key signing.key
dotnet run --project src/TimeShots.Cli/TimeShots.Cli.csproj -- verify --chain captures --key signing.key
```

Verification requires the signing key. Without it, image and chain hashes can
be inspected, but verification fails rather than reporting full integrity.

## Download a release

Releases are published for Windows x64 when a version tag is pushed. Download
`TimeShots.exe` for the desktop app or `timeshots-cli.exe` for the CLI from the
GitHub Release, along with `SHA256SUMS.txt` to verify the downloads. Both are
self-contained single-file executables and do not require a separate .NET
runtime.

Run `TimeShots.exe` to start the desktop app. From PowerShell, invoke the CLI
with its normal arguments, for example:

```powershell
.\timeshots-cli.exe verify capture.png --key signing.key
```

To publish a release, push a version tag such as `v1.0.0`:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

These releases are not code-signed. Windows may show an unknown-publisher or
Microsoft Defender SmartScreen warning when you run a downloaded executable.

## Development status

The current frontend and core are a Windows/.NET implementation. The
architecture and setup instructions for the previously proposed Electron and
Node.js application do not apply to this codebase.
