# Troubleshooting

## The window disappeared

Minimizing or closing hides ShiftDrag from the taskbar and leaves its icon in the notification area, possibly inside Windows' hidden-icons popup. Click the orange arrow icon to reopen. Use the icon's right-click **Quit** command to stop it completely. To rebuild the executable, quit first.

## The shortcut does not open the wheel

Start dragging an actual file before tapping Shift, Alt, or Ctrl. Keep holding the mouse until dropping onto the wheel. Confirm shortcuts are enabled in the workspace or tray menu. Windows restricts drag/drop between elevated and normal-user applications; run the source application and ShiftDrag at the same privilege level.

Try **Choose files** to check conversion independently, then **Test the drag** to test the actual OLE drop target with a generated sample. The latter opens the wheel automatically; it does not test the global shortcut path.

## The wheel flickers or selects the wrong segment

v0.2 ignores repeated modifier callbacks, uses one external drop surface, caches geometry and file data, and repaints on selection changes. If a problem remains, report the Windows version, display scale, monitor arrangement, source application, file type, and exact gesture. Physical Explorer timing and mixed-DPI behavior still need broader testing.

The local `wheel-diagnostics.log` records event counters. Check it before sharing; other diagnostic files can include local paths. The wheel has a glass-style tint and real transparency; native Acrylic blur is disabled. Turning the tint off in Settings changes its appearance.

## Formats or tools are missing

The catalog adapts to the selection and detected engines. A single file's current extension is usually omitted. Use Page Up / Page Down for additional tools. Mixed file families and folders offer ZIP plus file actions.

- **Audio/video, WebP, AVIF:** install FFmpeg separately. Configure `ffmpeg.exe` in Settings or place it on PATH. Keep ffprobe alongside it for probe-dependent tools. Codec availability depends on the build.
- **PDF tools:** a source-only build needs the [PDF runtime bundling step](DEVELOPMENT.md#optional-pdf-engine). `python.exe`, its packages, and `pdf_engine.py` must remain in their expected locations.
- **Office:** install LibreOffice and configure `soffice.exe`. These conversions remain unvalidated on the development machine.

## The PDF export has no text or the layout changed

PDF-to-TXT/Markdown/DOCX/EPUB uses selectable-text extraction with limited layout preservation. It does not perform OCR. Use image export for a visual rendering of all pages. Content-stream compression may not reduce an image-heavy PDF.

## Conversion failed or was canceled

Check that the selected output folder is writable and the relevant engine works. A failed active stage is cleaned up; already completed batch items remain and appear in session activity. Original conversion inputs remain intact. File **Move to…** is explicitly different and moves originals.

## Build or packaging fails

Run the scripts on Windows with .NET Framework 4.8 and the Framework64 compiler installed. Quit the running tray process before replacing `ShiftDrag.exe`. Keep `Main.xaml` beside the executable. PDF bundling expects a full Python 3.12 layout, not a virtual environment. See [development](DEVELOPMENT.md) for prerequisites and commands.

## Settings and diagnostics

Local files live under `%LOCALAPPDATA%\ShiftDrag`. Exit via the tray before manually changing settings. Activity and shelf references last only for the session. [Privacy](PRIVACY.md) describes logs and temporary files; [issue templates](https://github.com/OussamaERrafif/shiftdrag/issues/new/choose) list useful report details.
