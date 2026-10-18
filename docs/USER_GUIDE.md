# ShiftDrag user guide

[Back to the project](../README.md) · [Formats](FORMATS.md) · [Setup and build](DEVELOPMENT.md) · [Troubleshooting](TROUBLESHOOTING.md)

A native Windows file converter inspired by [Tangerine](https://tangerineformac.com/) and its [drag + Shift demo](https://x.com/th2049/status/2111477585461039241). This is an independent implementation with original branding, written in C# and WPF. No website wrapper, account, telemetry, or file uploads.

## Run

From the repository root, double-click **Start ShiftDrag.cmd**, or build first and run **build/ShiftDrag.exe**. In an assembled portable package, use its launcher or `ShiftDrag.exe` instead. Keep `Main.xaml`, `pdf_engine.py`, and the `engines` folder beside the executable. Minimizing or closing the window hides it from the taskbar and leaves the app running in the system tray. Click its tray icon to reopen it. Right-click the icon to quit or pause shortcuts.

Windows 10/11 and .NET Framework 4.8 are required. Build with `./build.ps1`; no .NET SDK or NuGet download is necessary. The build uses the Windows .NET Framework compiler.

## The gesture

1. Start dragging one or more real files from Explorer or the desktop.
2. While holding the mouse, tap **Shift** for conversions, **Alt** for file tools, or **Ctrl** for file actions.
3. Release the key, move onto a wheel segment, and release the mouse.

The menu stays open after releasing the key. Dropping in the center cancels; Escape dismisses the menu. Up to eight options appear per page. Use **Page Up / Page Down during a drag** to reach more tools; when browsing, you can also scroll or click the page arrows. The menu moves inward near screen edges. Dragging between elevated and non-elevated applications is subject to Windows restrictions.

You can also choose files or drop them onto the workspace. In that mode, click a segment or use arrow keys and Enter; Shift, Alt, and Ctrl switch wheels. Passing file paths as command-line arguments opens a menu for those files. `--tray` starts with the main window hidden.

## Included

| Family | Functionality |
|---|---|
| Images | PNG, JPEG, WebP, TIFF, BMP, GIF, AVIF; image-to-PDF; JPEG compression; resize; visual crop; rotate right/left/180°; horizontal/vertical flip; half-size; grayscale; colored background; text watermark; multi-image collage; remove metadata |
| Audio | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA; compress; trim; normalize; mono/stereo; timed bleep; waveform MP4 |
| Video | MP4, MOV, MKV, WebM, AVI, WMV, GIF, MP3; compress; trim; crop; mute; frame export; speed with pitch-preserving audio; join; split; rectangle redaction; remove metadata |
| PDF | All-page PNG/JPG at 300 DPI; text and basic Markdown; editable text DOCX and EPUB; compress content streams; remove document tags; merge; split; reorder, duplicate, omit, and rotate pages |
| Text/subtitles | TXT/Markdown/HTML; SRT/VTT with cue timing; subtitle text export |
| Archives | ZIP files/folders/selection; extraction with path traversal checks and entry/expanded-size limits |
| Office | LibreOffice-backed document, spreadsheet, and presentation conversions when installed |
| File actions | Clipboard files, copy paths, floating shelf, reveal in Explorer, copy to folder, move to folder |

Results save beside each source, or in the folder selected in Settings. Existing outputs are never overwritten. Conversion stages are published only on success. Cancellation removes the current partial output and keeps already completed items. Activity lists completed outputs if a later batch item fails.

The floating shelf stores references for this session. Drag a shelf row into another app. Removing a row leaves the original file untouched. Activity also lasts for the current session; settings and error diagnostics live in `%LOCALAPPDATA%/ShiftDrag`.

## Wheel appearance and drag stability

The wheel uses real alpha transparency, translucent gradients, a purple selection highlight, and a glass-style center and hint card. Turn the glass tint on/off in Settings. Native Windows Acrylic was visually tested in this WPF overlay and produced an opaque rectangle, so native backdrop blur is disabled. The shipped wheel does not take screenshots of your desktop to imitate blur.

During a file drag there is one hit-test/drop surface. Shortcut key repeats are ignored, the active mode is not rebuilt again, source data is read once, and the wheel redraws only when its selected segment changes. Drag feedback stays Copy across segment edges and the center; the final drop still cancels when released in the center. A small `wheel-diagnostics.log` records event counts, with no file names or contents, in the local settings folder.

Use **Test the drag** in the workspace to convert a generated sample image through the same OLE drop target. This test opens the wheel automatically, so it tests file dragging and selection without needing the global shortcut. The Explorer drag-and-key path still needs manual checking in your own workflow.

## Conversion performance

Video encoding defaults to x264 `veryfast` at the same CRF 22; choose `fast` or `medium` in Settings for different speed/compression tradeoffs. Compatible mute/metadata operations copy video streams without re-encoding, with a re-encoding fallback for incompatible streams. Image encoding avoids redundant managed pixel buffers and still removes metadata.

Three-run local medians: video conversion 1780 → 876 ms; mute 1396 → 145 ms; image rotation 1361 → 953 ms. JPEG timing did not improve consistently. These are measurements for the included test inputs, not universal speed guarantees. The portable package includes `PERFORMANCE.md`; the source CSVs and runner are in `benchmarks/`.

## Engines

- Basic image, text, subtitle, ZIP and image-to-PDF operations use Windows/.NET libraries.
- Audio/video and WebP/AVIF use **FFmpeg**, automatically detected from `engines/ffmpeg.exe`, `C:/ffmpeg/bin`, or PATH. Speed/join tools also use `ffprobe.exe` beside FFmpeg or on PATH. FFmpeg is installed on this development PC; it is **not redistributed in the app archive**. Set an executable override in Settings when needed. Codec availability depends on that FFmpeg build.
- The development portable build bundles a Python runtime, pypdf, PDFium and Pillow for offline PDF operations. Their license notices are retained in `engines/python`. No separate Python installation is required to run that assembled package. Engine binaries are excluded from the source repository; source builds require the bundling step below.
- **LibreOffice** is optional and is not installed by this project. Set `soffice.exe` in Settings or install it in its usual location to enable Office formats. Office conversions have not been validated on this PC because LibreOffice is absent.
- For a source-only rebuild, run `bundle-pdf-engine.ps1 -PythonSource <Python-3.12-directory>` against a runtime containing pypdf, pypdfium2 and Pillow. Then run `build.ps1`.

## Differences from Tangerine

This build does not claim the original's 275-path/26-tool parity. Missing features include photo annotation, advanced photo adjustments, automatic face/text detection, OCR, PDF-to-PowerPoint/Excel, HEIC export, TAR/GZIP/RAR handling, macOS Pages/RTFD, and AirDrop. Windows copy-to and Explorer actions replace Mac-specific actions. Shortcuts are currently fixed.

PDF text, DOCX, EPUB and Markdown exports extract selectable text, with limited layout preservation. Scanned PDFs need OCR elsewhere. PDF compression is lossless content-stream compression and may not reduce image-heavy files. Animated image imports use the first frame. Video crop/redaction use explicit pixel rectangles; redaction covers the entire clip. PDF page organization uses an explicit page list. File moves are explicit actions that move originals; conversion and editing operations keep originals.

## Verification

`build/ShiftDrag.exe --self-test` creates fixtures under `test-results` and writes `test-results/latest.txt`. Checks cover real image/media/PDF conversion, the added image tools, metadata stripping, PDF rendering followed by crop, merge/split/page organization, subtitle timing, Unicode HTML, collision safety, ZIP traversal rejection, cancellation, shortcut key repeats, menu rebuild prevention, paging, and preservation of compressed video packets in the fast mute path. Native UI verification covers the file picker, radial conversion, keyboard mode switching, a real 50 × 30 pixel crop, and converting a sample with an OLE drag. A physical drag-and-key gesture across Explorer and mixed-DPI displays still needs manual verification.

## Research

`research/` contains the full website screenshot, X demo screenshots, homepage and tool-page snapshots, every format tab, press information, privacy information, and an implementation feature map. Research captures are reference material and are not included in the application UI.
