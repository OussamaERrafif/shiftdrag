# Privacy and local data

ShiftDrag processes files on the local computer. The app has no account system, telemetry, analytics, upload endpoint, or cloud conversion service. Optional FFmpeg, PDF, and LibreOffice processing runs through local executables.

## Storage

| Data | Location / lifetime |
|---|---|
| Conversion outputs | Beside the source or in the configured output folder; remain until you remove them |
| Settings | `%LOCALAPPDATA%\ShiftDrag\settings.json`; persists between sessions |
| Error diagnostics | Local ShiftDrag settings directory; may contain exception text and file paths |
| Wheel diagnostics | Local `wheel-diagnostics.log`; interaction counters, no file names or contents |
| Activity and floating shelf | In memory for the current session; shelf entries reference original files |
| PDF request | Temporary local JSON file, removed after the helper finishes |
| Conversion stage | Temporary output before publication; removed on handled failure/cancellation |
| Self-test fixtures | Repository-adjacent `test-results/`; synthetic files, excluded from Git |

An unexpected process termination can leave temporary files. Removing a shelf row does not delete the original file. Explicit **Move to…** moves source files; other conversion/editing operations produce new outputs.

Clipboard actions put selected file references or paths onto the Windows clipboard, where other applications can access them according to Windows behavior. Dragging shelf entries into another app passes the file references to that app.

Dependencies are installed separately or bundled locally. Downloading dependencies and using GitHub requires network access; local conversion does not. This statement describes ShiftDrag's application behavior, not the privacy practices of unrelated source/target applications or externally installed engines.

Review logs and screenshots for personal paths or document contents before including them in a public bug report. For sensitive reports, see [SECURITY.md](../SECURITY.md).
