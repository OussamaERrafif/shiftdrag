# Testing and verification

## Automated checks

Build first, then run from the repository root:

```powershell
$testProcess = Start-Process .\build\ShiftDrag.exe -ArgumentList '--self-test' -Wait -PassThru
Get-Content .\test-results\latest.txt
if ($testProcess.ExitCode -ne 0) { throw 'Self-tests failed.' }
```

Fixtures and outputs go to ignored, timestamped directories under `test-results/`. No personal files are needed. The development build passed **46 checks** with the PDF runtime and FFmpeg/ffprobe installed. The Windows CI workflow builds the source and runs the checks available on its runner; a source-only run skips optional PDF/media groups. A green core run does not establish optional-engine coverage.

| Area | Assertions |
|---|---|
| Shortcut and wheel | Held-key repeat suppression, repeated same-mode callbacks, page sizing, cancel center |
| Images | Readable conversion, resize/crop dimensions, invalid crop cleanup, rotation, half-size, flip pixels, background color, watermark, collage, metadata removal |
| Outputs | Original preservation, collision-safe repeated conversions, cancellation, no partial published files |
| Text / ZIP | Subtitle cue timing, escaped Unicode HTML, ZIP round trip, traversal rejection |
| PDF, when installed | Rendering and crop, selectable text, merge/split, organization/rotation, invalid page cleanup, DOCX/EPUB package content, compression readability |
| FFmpeg, when installed | Audio/image conversion, trim, normalize, channels, waveform, bleep, speed, crop, redaction, split/join, exact video packet hash preservation on fast mute |

## Native UI verification recorded for v0.2

- File picker, radial conversion, and keyboard mode switching.
- Visual crop producing a real 50 × 30 pixel image.
- Paging through image tools and inspecting the transparent wheel.
- A real OLE drag of a generated sample into the production drop target, producing a JPEG.
- Tray-only behavior when minimizing/closing and reopening from the icon.

The recorded OLE drag summary is in [the QA notes](../research/QA-ui-and-drag.md). It uses the app's **Test the drag** control, which opens the wheel automatically. This does not establish physical Shift timing in Explorer.

## Manual checks for changes to input or rendering

1. Drag a disposable image from Explorer, tap and release Shift, cross segment boundaries, then drop on JPEG. Confirm a new readable output and an intact original.
2. Hold a modifier long enough to generate key repeats; confirm the wheel remains stable.
3. Switch to Alt/Ctrl while dragging; page with Page Up / Page Down. Confirm the highlighted operation matches the drop.
4. Drop in the center and test Escape. Confirm neither creates an output.
5. Repeat near screen edges, on mixed-DPI displays, and after changing display scale.
6. Minimize/close the dashboard, reopen from the tray, pause shortcuts, and quit explicitly.
7. Use the crop editor; cancel an active long conversion and inspect outputs and activity.

Explorer modifier timing across real workflows and mixed-DPI behavior still need broader verification. LibreOffice conversions remain unvalidated on the development machine. Report the Windows version, display scale, exact gesture, file type, and engine versions with a regression.

## Performance

The [benchmark report](../benchmarks/PERFORMANCE.md) includes three-run medians, fixture descriptions, and encoding tradeoffs. [Before](../benchmarks/before.csv) and [after](../benchmarks/after.csv) CSVs preserve measurements; [BenchmarkRunner.cs](../benchmarks/BenchmarkRunner.cs) and [analyze.py](../benchmarks/analyze.py) record timing and generate the report. Large generated fixture files are intentionally excluded from Git.
