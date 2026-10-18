<div align="center">

<img src="docs/assets/mark.svg" width="72" height="72" alt="ShiftDrag logo">

# ShiftDrag for Windows

**Drag. Drop. Done.**

Convert, edit, and organize files with a transparent wheel, right where you work.

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?logo=windows)](#getting-started)
[![C# / WPF](https://img.shields.io/badge/C%23-WPF-7952F5)](docs/DEVELOPMENT.md)
[![Local processing](https://img.shields.io/badge/Processing-local-58794B)](docs/PRIVACY.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-F97336)](LICENSE)

[Getting started](#getting-started) · [User guide](docs/USER_GUIDE.md) · [Supported formats](docs/FORMATS.md) · [Report a bug](https://github.com/OussamaERrafif/shiftdrag/issues/new/choose)

<img src="docs/assets/workspace.jpg" width="1100" alt="ShiftDrag workspace with file drop area, quick tools, and tray status">

</div>

ShiftDrag is a native C# / WPF utility for Windows 10 and 11. Drag files, tap a modifier key, and drop onto an action. Processing happens on your PC, with no account, telemetry, or uploads. Minimize or close the window and it stays available in the system tray.

**Status: v0.2 preview.** This repository contains the source, documentation, screenshots, and research. Optional local engines unlock media, PDF, and Office features. See the [capability matrix](docs/FORMATS.md) for requirements and limitations.

## One gesture, three wheels

1. Start dragging files from Explorer or your desktop.
2. Tap **Shift**, **Alt**, or **Ctrl** while holding the mouse.
3. Release the key, hover over a segment, and drop to apply it.

| Key | Wheel | Examples |
|---|---|---|
| **Shift** | Convert | PNG → JPG, video → MP4, PDF → images |
| **Alt** | Tools | Resize, crop, watermark, trim, mute, organize PDF pages |
| **Ctrl** | Actions | Copy files or paths, float on the shelf, reveal, copy or move |
| **Page Up / Page Down** | More options | Browse pages of up to eight segments during a drag |
| **Escape** or center drop | Cancel | Dismiss without applying an operation |

The wheel stays open after releasing the modifier. You can also choose files in the workspace and click an action, or use arrow keys and Enter.

<p align="center">
  <img src="docs/assets/glass-wheel.jpg" width="1000" alt="Transparent glass-style tools wheel showing image actions and a purple selection highlight">
  <br>
  <sub>Actual Windows app screenshot: translucent wheel, paged tools, and a highlighted selection.</sub>
</p>

## What you can do

- **Images:** convert, compress, resize, visually crop, rotate, flip, watermark, build collages, flatten transparency, and create PDFs.
- **Audio and video:** convert, trim, compress, normalize, mute, extract audio or frames, change speed, join, and split with local FFmpeg.
- **PDFs:** render every page, extract selectable text, merge, split, reorder, duplicate, rotate, and remove document metadata with an optional bundled PDF engine.
- **Everyday files:** convert text and subtitles, create or safely extract ZIP archives, and keep file references on a floating shelf.
- **Desktop workflow:** context-sensitive actions, tray-only operation when hidden, configurable output folder, image quality, glass tint, and video encoding speed.

Conversions keep your originals and avoid overwriting existing outputs. **Move to…** explicitly moves the original files. The [user guide](docs/USER_GUIDE.md) explains output handling, cancellation, and multi-file behavior.

## Getting started

### Build and run from source

Requirements: **Windows 10/11**, **.NET Framework 4.8**, and Windows PowerShell. The core app uses the Windows Framework compiler; no .NET SDK or NuGet restore is needed.

```powershell
git clone https://github.com/OussamaERrafif/shiftdrag.git
cd shiftdrag
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
.\build\ShiftDrag.exe
```

Alternatively, double-click **Start ShiftDrag.cmd** to build if needed and open the app. A source build supports basic image, text, subtitle, ZIP, and image-to-PDF operations immediately.

### Enable additional formats

| Engine | Enables | Setup |
|---|---|---|
| FFmpeg + ffprobe | Audio/video; WebP and AVIF | Install separately; use PATH, `C:\ffmpeg\bin`, or executable overrides in Settings |
| Python 3.12 + PDF packages | PDF rendering, extraction, page tools | Follow the [PDF bundling instructions](docs/DEVELOPMENT.md#optional-pdf-engine) |
| LibreOffice | Office document conversions | Install separately and configure `soffice.exe` in Settings if needed |

Keep `Main.xaml`, `pdf_engine.py`, and any `engines` directory beside `ShiftDrag.exe`. The source repository does not include these third-party engine binaries. Codec support depends on the installed FFmpeg build; Office conversions have not been validated on the development machine.

## Performance and verification

Compatible video mute/metadata operations copy streams without re-encoding. Video conversion defaults to configurable x264 `veryfast`; image export avoids redundant managed pixel buffers.

| Local benchmark, three-run median | Previous | v0.2 |
|---|---:|---:|
| Video conversion | 1,780 ms | 876 ms |
| Video mute | 1,396 ms | 145 ms |
| Image rotation | 1,361 ms | 953 ms |

These measurements describe specific test inputs, with encoding tradeoffs. JPEG did not consistently improve. Read the [benchmark report and raw results](benchmarks/PERFORMANCE.md).

The development build passed **46 functional checks** with PDF and FFmpeg available. Automated checks cover output content, cancellation, collision safety, ZIP traversal rejection, shortcut repeats, paging, and stream preservation. Native UI checks covered conversion, crop, and a real OLE file drag. Physical Explorer shortcut timing and mixed-DPI behavior still need broader manual testing. See [verification details](docs/TESTING.md).

## Documentation

| Guide | Contents |
|---|---|
| [User guide](docs/USER_GUIDE.md) | Gestures, tray, shelf, settings, outputs, and limitations |
| [Formats and tools](docs/FORMATS.md) | Supported families, dependencies, and gaps |
| [Development](docs/DEVELOPMENT.md) | Build, PDF bundling, packaging, and repository layout |
| [Architecture](docs/ARCHITECTURE.md) | Native UI, drag lifecycle, conversion pipeline, local engines |
| [Testing](docs/TESTING.md) | Automated checks, manual QA, and known verification limits |
| [Troubleshooting](docs/TROUBLESHOOTING.md) | Missing engines, tray behavior, drag issues, and diagnostics |
| [Privacy](docs/PRIVACY.md) | Local processing, settings, logs, and temporary files |
| [Screenshots](docs/SCREENSHOTS.md) | Actual app captures and reference provenance |
| [Logo and brand assets](docs/BRAND.md) | Vector source, Windows icons, colors, and regeneration |
| [Changelog](CHANGELOG.md) | Changes in v0.2 |
| [Research](research/RESEARCH.md) | Website crawl, reference screenshots, demo observations |

## Contributing

Bug reports, reproducible examples, and focused improvements are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) for setup and review expectations. For security-sensitive reports, see [SECURITY.md](SECURITY.md).

## Credits and license

Inspired by [Tangerine for Mac](https://tangerineformac.com/) and its [drag-and-convert demo](https://x.com/th2049/status/2111477585461039241). ShiftDrag is an independent Windows implementation with its own code and branding, with no affiliation or endorsement implied. It does not claim full feature parity.

Original project code and documentation are available under the [MIT license](LICENSE). Dependencies and reference captures retain their respective licenses and rights; see [third-party notices](THIRD_PARTY_NOTICES.md).
