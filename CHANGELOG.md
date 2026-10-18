# Changelog

## Unreleased

- New radial-ring and drag-arrow identity, with an editable vector and transparent PNG exports.
- Matching sidebar, tray, window, and executable icons; native ICO includes nine resolutions.
- Reproducible WPF asset export script and brand guide.

## 0.2 — 2026-10-18

### Added

- Redesigned workspace with quick tools, tray status, and navigation.
- Transparent glass-style wheel, purple selection, and paged actions.
- Image flips, left/180° rotation, half-size, background flattening, text watermark, and multi-image collage.
- Glass tint and video encoding preset settings.
- Generated sample for testing a real OLE drag through the production drop target.
- Public source documentation, app screenshots, research captures, contribution templates, and Windows build workflow.

### Fixed

- Repeated modifier callbacks rebuilding the active menu.
- Multiple child drop targets and unnecessary layered-window repainting during selection.
- Unstable drag feedback when crossing wheel regions.

### Performance

- Stream-copy path for compatible video mute/metadata operations, with fallback.
- Configurable x264 preset with `veryfast` as the default.
- Fewer managed image pixel buffers during export.
- Recorded benchmark CSVs and a report covering gains and unchanged JPEG performance.

### Verification and limits

The development build passed 46 functional checks with PDF/media engines present. Native UI verification included conversion, crop, and a real OLE drag. Explorer shortcut timing and mixed-DPI use still need broader manual validation. Native Acrylic is disabled after visual testing produced an opaque rectangle; the shipped effect uses per-pixel transparency and gradients.

## 0.1 — initial Windows implementation

Native C# / WPF tray app; drag-modifier wheels; image, text, subtitle and ZIP operations; optional FFmpeg and LibreOffice; local PDF helper; crop editor; shelf; activity; output staging and cancellation. Reference website and demo research were captured during development.
