# SMM2SaveEditor

[![Build and Release](https://github.com/basilicon/SMM2SaveEditor/actions/workflows/build.yml/badge.svg)](https://github.com/basilicon/SMM2SaveEditor/actions/workflows/build.yml)
[![Target .NET](https://img.shields.io/badge/.NET-8.0%20%7C%206.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia%2011-8B44AC)](https://avaloniaui.net/)
[![Platform Support](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)](https://github.com/basilicon/SMM2SaveEditor/releases)

A modern, cross-platform visual course editor, save slot manager, and diagnostic suite for **Super Mario Maker 2** (Nintendo Switch).

Built with **Avalonia UI** and **.NET**, **SMM2SaveEditor** allows level creators and modders to inspect, modify, de-corrupt, and manage SMM2 course files (`.bcd`), course thumbnails (`.btl`), and emulator/hardware save files (`save.dat`).

---

## ✨ Features

### 🗂️ Coursebot Save Slot Manager
- **Visual 60-Slot Coursebot Grid**: View and manage all 60 Coursebot slots arranged exactly like in-game (1-1 through 15-4).
- **Auto-Detection for Emulators**: Automatically detects save directories for Ryujinx, Yuzu, Suyu, and custom emulator paths.
- **Double-Buffer Synchronization**: Fully handles Switch double-buffered save directories (`0/` and `1/`) and synchronizes `save.dat` slot registration tables to prevent courses from disappearing or rolling back.
- **One-Click Save Backup**: Create zipped snapshots of your entire save folder before making changes.
- **Slot Operations**:
  - Direct import and export of decrypted/encrypted `.bcd` course files.
  - "Inject Current Editor Level" straight into any selected Coursebot slot.
  - One-click **Unhide in Coursebot** for courses with disabled slot visibility flags.

### 🩺 Course Integrity Diagnostics & De-Corruption Engine
- **Deep Health Analysis**: Reverse-engineered validation checks for:
  - `save.dat` slot status and registration table consistency.
  - `.bcd` container file size (376,320 bytes), AES-CBC-128 crypto, and CRC32 hash checksums.
  - Game style, game version, and coordinate bounds validation.
  - Object count limits, boundary integrity, and actor initialization flag sanity.
- **Automated De-Corruption**:
  - One-click repair to sanitize corrupt or out-of-spec actor flags that cause game crashes.
  - Fixes orphan `.bcd` files by restoring their registration in `save.dat`.
  - Automatically synthesizes valid placeholder thumbnails if missing or damaged.

### 🖼️ Thumbnail (.btl) Manager & Image Converter
- **Live 16:9 Thumbnail Previews**: Decrypts and renders embedded level thumbnails in real time.
- **Import Custom Images**: Load any standard image (PNG, JPG, BMP); automatically resizes and encodes them into game-compliant AES-encrypted `.btl` containers via SkiaSharp.
- **Export to JPEG**: Extract in-game course thumbnails to standard JPEG images on disk.
- **Clone & Reset**: Copy thumbnails between slots or reset to game-compliant blank container structures (preventing game freeze crashes caused by deleted files).

### ✏️ Visual Level Canvas Editor
- **Interactive 2D Viewport**: Pan, zoom, and fit-to-window controls with real-time coordinate tracking.
- **Drag-and-Drop Entity Manipulation**: Move objects and actors with grid snapping.
- **Selection & Adorners**: Click to select entities with visible selection borders; quick delete with the `Delete` key.
- **Overlapping Entity Stack**: Intuitively cycle or select overlapping elements via an integrated dropdown and stacked property cards.
- **Detailed Object Inspector**: Inspect and adjust entity IDs, coordinates, dimensions, states, and complex path nodes (Snake Blocks, Track Blocks, ! Blocks, Clear Pipes, Piranha Creepers).

### ⚙️ Course Properties & Metadata
- Edit Course Name (up to 66 characters) and Description (up to 202 characters).
- Configure Game Style:
  - **SMB1** (Super Mario Bros.)
  - **SMB3** (Super Mario Bros. 3)
  - **SMW** (Super Mario World)
  - **NSMBU** (New Super Mario Bros. U)
  - **3W** (Super Mario 3D World)
- Adjust Level Timer, Autoscroll Speed, and Clear Conditions (type, category, target count).
- Regenerate random Creation IDs or edit creation timestamps.

### 🪟 Windows Integration
- Optional one-click registration of `.bcd` file associations in Windows Explorer, complete with custom application icons.
- Drag-and-drop `.bcd` course files directly onto the editor window to open immediately.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>F1</kbd> | Switch to **Coursebot Save Slots Manager** |
| <kbd>F2</kbd> | Switch to **Level Canvas Editor** |
| <kbd>F4</kbd> | Open **Course Properties** |
| <kbd>F5</kbd> | Run **Course Diagnostics** |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open Course file (`.bcd`) |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save Course file |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Export Course to a new `.bcd` file |
| <kbd>+</kbd> / <kbd>-</kbd> | Zoom In / Zoom Out |
| <kbd>Space</kbd> | Fit Canvas to Window |
| <kbd>R</kbd> | Reset Canvas Zoom (100%) |
| <kbd>Delete</kbd> | Delete selected entity |

---

## 🚀 Getting Started

### Download Standalone Binaries
Pre-compiled standalone packages for **Windows**, **Linux**, and **macOS** are automatically generated on releases.

👉 [Download the latest release here](https://github.com/basilicon/SMM2SaveEditor/releases)

No separate .NET runtime installation is required for standalone releases.

---

### Building from Source

#### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or .NET 6.0 SDK)
- Git

#### Clone & Run
```bash
# Clone the repository
git clone https://github.com/basilicon/SMM2SaveEditor.git
cd SMM2SaveEditor

# Restore dependencies and run
dotnet restore
dotnet run
```

#### Build Self-Contained Release
```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

---

## 📁 Save File Locations

When using emulators, SMM2 save folders are typically located at:

| Emulator | Default Path |
| :--- | :--- |
| **Ryujinx** | `%AppData%\Ryujinx\bis\user\save\<title_id>\<save_id>` |
| **Yuzu / Suyu** | `%AppData%\yuzu\nand\user\save\0000000000000000\<profile_id>\<title_id>` |
| **Hardware (JKSV/Checkpoint)** | Extracted SD card save backup folder containing `save.dat` and `course_data_*.bcd` |

> [!NOTE]  
> SMM2SaveEditor will automatically detect Ryujinx and Yuzu save directories when you open the **Coursebot Save Slots** view.

---

## 🛠️ Technology Stack

- **Framework**: [.NET 8 / C#](https://dotnet.microsoft.com/)
- **UI Toolkit**: [Avalonia UI 11](https://avaloniaui.net/) (Cross-platform XAML)
- **Viewport Navigation**: [Avalonia.Controls.PanAndZoom](https://github.com/wieslawsoltes/PanAndZoom)
- **Image Processing**: [SkiaSharp](https://github.com/mono/SkiaSharp)
- **Binary Parsing**: [Kaitai Struct C# Runtime](https://kaitai.io/)
- **Cryptography**: [BouncyCastle Cryptography](https://www.bouncycastle.org/csharp/) (AES-CBC-128 & CMAC)

---

## ⚠️ Disclaimer

This tool is an unofficial, community-made save editor and is not affiliated with, endorsed by, or associated with Nintendo. 

- Always use the **Backup Save** feature before modifying your save files.
- Modifying course files with impossible parameters, corrupted flags, or unplayable states can cause crashes or bans if uploaded to Nintendo's online servers. Use responsibly for local play and testing.
