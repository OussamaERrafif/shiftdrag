# Third-party notices

The root MIT license covers ShiftDrag's original code and documentation. It does not relicense third-party software, trademarks, website text, or reference screenshots.

## Optional bundled components

ShiftDrag's local PDF engine uses these existing runtime components. Their license texts and build notices are included with the portable app.

| Component | Version in this build | License notices |
|---|---|---|
| CPython | 3.12 runtime | `engines/python/LICENSE.txt` |
| pypdf | 6.10.0 | `engines/python/Lib/site-packages/pypdf-6.10.0.dist-info/licenses/LICENSE` |
| pypdfium2 / PDFium | pypdfium2 5.14.0 | `engines/python/Lib/site-packages/pypdfium2-5.14.0.dist-info/licenses/` includes wrapper licenses and PDFium dependency/build notices |
| Pillow | 12.3.0 | `engines/python/Lib/site-packages/pillow-12.3.0.dist-info/licenses/LICENSE` |

FFmpeg and LibreOffice are optional separately installed engines and are not distributed in the app archive. The source repository excludes all engine binaries. Package versions above describe the assembled development build; source dependencies are pinned in `requirements-pdf.txt`.

## Reference material

The `research/` directory contains public text snapshots and screenshots of [Tangerine's website](https://tangerineformac.com/) and [the linked X demo](https://x.com/th2049/status/2111477585461039241), captured for feature and interaction research. This material and the trademarks shown remain the property of their respective owners and are excluded from the root MIT grant. Capture provenance is recorded in `research/RESEARCH.md`.

This project does not redistribute Tangerine's application or source code. Reference branding is not used in ShiftDrag's shipped interface. ShiftDrag is an independent implementation with its own name and assets; no affiliation or endorsement is claimed.
