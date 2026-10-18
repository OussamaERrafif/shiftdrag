# UI and drag verification

The previous drag path redrew the layered window for every DragOver, recreated child controls for repeated modifier-key callbacks, and exposed multiple child hit targets. The update has one drop surface for an external drag, ignores key repeats, makes same-mode updates idempotent, caches file data and segment geometry, and paints only on a selection change. Drag feedback remains Copy until a real drop; a center drop cancels.

Automated regressions include 100 held-key callbacks, 100 same-mode updates with no rebuild, page sizing, and a non-committing center. The added image tools are verified against actual output pixels/dimensions, and optimized encoding is checked for metadata removal.

A native UI file-drag test converted the generated sample to JPG. Its diagnostic summary was:

```
external=True glass=True builds=2 dragEvents=2 selectionChanges=1 paints=1 effectChanges=1 invalidData=0 applied=True
```

The two builds are initial construction and the first binding of the incoming file type. This is an actual OLE drag into the production drop target; the lab opens the wheel automatically. It does not verify physical Shift timing in Explorer. The shortcut gate is tested independently; Explorer and mixed-DPI behavior remain manual checks.

Native backdrop blur was attempted using the documented [DWM system backdrop attribute](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type) and frame extension. The visible result on this PC was an opaque rectangular backdrop, including after layout and with WindowChrome. That path was removed. The release uses verified per-pixel transparency and glass-style gradients, with no desktop capture or simulated screenshot blur.

Conversion timings, inputs, run counts and tradeoffs are recorded in [the benchmark report](../benchmarks/PERFORMANCE.md) and its CSV files.
