# TimeShots

> A transparent, tamper-evident screenshot utility that embeds verifiable timestamps and metadata into every capture.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)
![Status](https://img.shields.io/badge/status-In%20Development-orange.svg)

---

## What is TimeShots?

TimeShots is an open-source screenshot application that overlays **date, time, and timezone information** on the bottom-right corner of every screenshot. But it goes further than a simple overlay — every capture is **cryptographically signed and hash-chained**, producing an immutable, verifiable record that proves:

- **When** the screenshot was taken (NTP-synced UTC + local time)
- **What** was captured (pixel-perfect, no post-processing ambiguity)
- **That it hasn't been altered** since capture (SHA-256 hash chain + HMAC signature)

---

## Features

- 🖥️ **Full screen, window, or region** capture modes
- 🕐 **Timestamped overlay** — date, time, timezone, and monotonic tick rendered on the bottom-right
- 🔐 **SHA-256 hash chain** — each screenshot's hash includes the previous screenshot's hash, creating an unbreakable sequence
- ✍️ **HMAC signing** — every capture is signed with a user-controlled secret key
- 📋 **Metadata sidecar** — JSON metadata file alongside each image (hash, timestamp, system info, chain position)
- 🗄️ **Local-first storage** — all data stays on your filesystem; no cloud dependency
- 🔍 **Built-in verification tool** — independently verify any screenshot or the entire chain
- ⌨️ **Global hotkey** support
- 🖱️ **System tray** integration
- 📦 **Export** — bundle screenshots + metadata + verification script as a self-contained archive

---

## Architecture Overview

TimeShots is organized into four layers, each developed on its own branch:

```
┌─────────────────────────────────────────────────────────┐
│                    FRONTEND (UI)                        │
│          Electron + React + TypeScript                  │
│   branch: frontend                                     │
│                                                        │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────┐    │
│  │ Tray Icon│  │ Capture  │  │ Gallery / Viewer   │    │
│  │ & Menu   │  │ Overlay  │  │ & Chain Inspector  │    │
│  └──────────┘  └──────────┘  └────────────────────┘    │
└────────────────────────┬────────────────────────────────┘
                         │ IPC (contextBridge)
┌────────────────────────▼────────────────────────────────┐
│                   BACKEND (Core)                        │
│            Node.js + native modules                     │
│   branch: backend                                      │
│                                                        │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────┐    │
│  │ Screen   │  │ NTP Sync │  │ Hotkey / Tray      │    │
│  │ Capture  │  │ & Clock  │  │ Manager            │    │
│  └──────────┘  └──────────┘  └────────────────────┘    │
└────────────────────────┬────────────────────────────────┘
                         │ function calls
┌────────────────────────▼────────────────────────────────┐
│               LOGIC (Integrity Engine)                  │
│              Pure TypeScript library                    │
│   branch: logic                                        │
│                                                        │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────┐    │
│  │ Overlay  │  │ Hash     │  │ HMAC Signer /      │    │
│  │ Renderer │  │ Chainer  │  │ Verifier           │    │
│  └──────────┘  └──────────┘  └────────────────────┘    │
│  ┌──────────┐  ┌──────────────────────────────────┐    │
│  │ Metadata │  │ Export / Archive Bundler         │    │
│  │ Builder  │  │                                  │    │
│  └──────────┘  └──────────────────────────────────┘    │
└────────────────────────┬────────────────────────────────┘
                         │ fs / SQLite calls
┌────────────────────────▼────────────────────────────────┐
│            DB / STORAGE (Local Filesystem)              │
│           SQLite + structured file store                │
│   branch: storage                                      │
│                                                        │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────┐    │
│  │ SQLite   │  │ Image    │  │ Config / Keystore  │    │
│  │ Index DB │  │ Store    │  │                    │    │
│  └──────────┘  └──────────┘  └────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## Verification

TimeShots includes a standalone **`timeshots` CLI** that is added to your system `PATH` during installation. It works independently — no running app required. All it needs is the screenshot, its JSON sidecar, and the signing key.

```bash
# Verify a single screenshot
timeshots verify ./timeshot_20261026_094402_042.png

# Verify an entire chain
timeshots verify --chain ~/.timeshots/captures/

# Verify an exported archive
timeshots verify --archive export_20261026.zip

# Trigger a capture from the command line
timeshots capture

# Export a date range
timeshots export --from 2026-10-01 --to 2026-10-26
```

**What verification checks:**
1. ✅ Image hash matches the hash in the sidecar
2. ✅ Chain hash correctly links to the previous capture
3. ✅ HMAC signature is valid (requires the signing key)
4. ✅ No gaps in the sequence numbering
5. ✅ Timestamps are monotonically increasing
6. ✅ Overlay text matches sidecar values

---

## Getting Started

### Prerequisites

- Node.js 20+
- npm 10+ or yarn 1.22+
- Git

### Installation (Development)

```bash
# Clone the repository
git clone https://github.com/yourusername/TimeShots.git
cd TimeShots

# Install dependencies
npm install

# Start in development mode
npm run dev
```

### Build & Install

```bash
# Build for your platform
npm run build

# Package as installable .exe / .msi (Windows), .dmg (macOS), .AppImage (Linux)
npm run package
```

The installer registers two components:

| Component | Description |
|-----------|-------------|
| **TimeShots App** | Electron GUI — system tray, capture, gallery, chain inspector |
| **`timeshots` CLI** | Standalone binary added to system `PATH` — verify, capture, and export from any terminal |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+S` | Capture full screen |
| `Ctrl+Shift+W` | Capture active window |
| `Ctrl+Shift+R` | Capture selected region |
| `Ctrl+Shift+G` | Open gallery |
| `Ctrl+Shift+V` | Verify last capture |

*All shortcuts are configurable in Settings.*

---

## Roadmap

- [x] Project architecture & documentation
- [ ] Core integrity engine (hash chain, HMAC, verifier)
- [ ] Storage layer (SQLite + filesystem)
- [ ] Backend services (capture, NTP, hotkeys)
- [ ] Frontend (Electron + React UI)
- [ ] Export & standalone verification tool
- [ ] Multi-monitor support
- [ ] Annotation tools (post-capture, non-destructive)
- [ ] CI/CD & automated testing
- [ ] Installers (Windows `.msi`, macOS `.dmg`, Linux `.AppImage`)

---

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting a PR.

1. Fork the repository
2. Create your feature branch
3. Commit your changes with clear, descriptive messages
4. Push to your fork and submit a pull request

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [Electron](https://www.electronjs.org/) — cross-platform desktop apps
- [Sharp](https://sharp.pixelplumbing.com/) — high-performance image processing
- [better-sqlite3](https://github.com/WiseLibs/better-sqlite3) — synchronous SQLite for Node.js
- [ntp-client](https://www.npmjs.com/package/ntp-client) — NTP time synchronization

---

<p align="center">
  <b>TimeShots</b> — Screenshots you can trust. 📸🔒
</p>
