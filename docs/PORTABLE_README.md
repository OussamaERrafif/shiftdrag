# ShiftDrag for Windows — portable preview

Drag. Drop. Done. A native, local file converter for Windows 10/11 with .NET Framework 4.8.

## Run

Extract the entire ZIP, then double-click **Start ShiftDrag.cmd** or **ShiftDrag.exe**. Keep `Main.xaml`, `pdf_engine.py`, and the `engines` folder beside the executable. Minimizing or closing hides the window; click the tray icon to reopen, or right-click it to quit.

1. Drag one or more real files from Explorer.
2. Tap **Shift** for conversion, **Alt** for tools, or **Ctrl** for file actions.
3. Release the key, select a wheel segment, and release the mouse.

**Page Up / Page Down** browse more options during a drag. Center drop or Escape cancels. You can also choose files from the workspace and click an option. **Test the drag** uses a generated sample through the actual OLE drop target.

Conversions preserve originals and avoid replacing existing outputs. **Move to…** explicitly moves originals. The shelf and activity last for the session. Preferences and diagnostics live under `%LOCALAPPDATA%\ShiftDrag`.

## Engines

Image, text, subtitle, ZIP, and image-to-PDF operations use Windows/.NET. PDF rendering/text/page tools require the bundled Python engine to be present. FFmpeg/ffprobe and LibreOffice are separately installed optional engines; configure them in Settings. This package does not include FFmpeg or LibreOffice.

PDF text exports have limited layout preservation and do not perform OCR. Animated image input uses the first frame. Codec support depends on FFmpeg. This is preview software; Explorer shortcut timing, mixed-DPI workflows, and Office conversions need broader validation. The wheel uses real transparency and glass-style gradients; native Acrylic blur is disabled.

## Documentation and source

- [Project and screenshots](https://github.com/OussamaERrafif/shiftdrag)
- [Complete user guide](https://github.com/OussamaERrafif/shiftdrag/blob/main/docs/USER_GUIDE.md)
- [Supported formats](https://github.com/OussamaERrafif/shiftdrag/blob/main/docs/FORMATS.md)
- [Troubleshooting](https://github.com/OussamaERrafif/shiftdrag/blob/main/docs/TROUBLESHOOTING.md)
- [Privacy](https://github.com/OussamaERrafif/shiftdrag/blob/main/docs/PRIVACY.md)
- [Report a bug](https://github.com/OussamaERrafif/shiftdrag/issues/new/choose)

See `PERFORMANCE.md` for local benchmark results, `LICENSE` for original code, and `THIRD_PARTY_NOTICES.md` plus runtime notices for dependencies. Inspired by [Tangerine for Mac](https://tangerineformac.com/); independent implementation with no affiliation or full-parity claim.
