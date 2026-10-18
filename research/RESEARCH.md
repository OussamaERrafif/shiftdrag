# Reference research — 18 October 2026

These captures document the original feature investigation. They depict third-party material, not ShiftDrag's own UI; rights remain with the original owners. For actual Windows app screenshots, see the [screenshot gallery](../docs/SCREENSHOTS.md).

Sources inspected:

- https://tangerineformac.com/
- https://tangerineformac.com/tools
- https://tangerineformac.com/press
- https://tangerineformac.com/privacy
- https://x.com/th2049/status/2111477585461039241

The website was captured as `tangerine-website.jpg`. Text snapshots are saved separately; `categories.json` contains all ten format categories. X initially returned HTTP 403 through text retrieval, but browser playback worked. The 21-second demo shows the radial conversion workflow, PDF/image outputs, and a crop editor. Frames are saved as `x-demo.jpg` and `x-demo-2.jpg`.

The interaction uses three context-sensitive wheels during an existing file drag. A tapped modifier opens a wheel that persists after key release. Dropping onto a segment performs a local operation and writes a new sibling copy. File actions also accept folders and mixed selections. The Windows implementation substitutes Alt for Command.

| Observed reference feature | Windows implementation |
|---|---|
| Drag + Shift conversion wheel | Global keyboard hook plus an OLE file drop target |
| Command tools wheel | Alt tools wheel |
| Control actions wheel | Ctrl actions wheel |
| Offline processing | Native/.NET operations, local FFmpeg, bundled PDF engine, optional local LibreOffice |
| Context-sensitive formats | Catalog computed from input type and engine availability |
| Conversion copies | Collision-safe staged outputs beside originals or in a chosen folder |
| Floating shelf | Topmost draggable file-reference shelf |
| PDF-to-image then crop demo | 300 DPI page rendering plus visual crop editor |
| Rich photo editors, OCR and other unimplemented tools | Explicitly listed as gaps in the documentation and in-app help |

No original application source code was available. The Windows implementation uses its own code and identity; reference screenshots and branding are not reused as shipped UI assets.
