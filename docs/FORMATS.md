# Supported formats and tools

The wheel is computed from the selected file types and detected engines. A format appearing in a family does not mean every possible input/output pair is supported. Mixed families and folders fall back to ZIP; file actions remain available.

| Family | Export formats | Tools | Engine |
|---|---|---|---|
| Images | PNG, JPEG, TIFF, BMP, GIF, PDF; WebP and AVIF with FFmpeg | Compress to JPEG; resize by maximum width; visual crop; rotate 90°/180°/270°; flip; half-size; grayscale; background; text watermark; collage; strip metadata | Windows/.NET; FFmpeg for additional codecs |
| Audio | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA | Compress, trim, remove metadata, normalize, mono/stereo, timed bleep, waveform MP4 | FFmpeg |
| Video | MP4, MOV, MKV, WebM, AVI, WMV, GIF, MP3 | Compress, trim, remove metadata, mute, extract audio/frame, crop, speed, split, rectangle redaction, join | FFmpeg; ffprobe for speed/join |
| PDF | All-page PNG/JPG at 300 DPI; TXT, Markdown, DOCX, EPUB | Compress content streams, strip tags, merge, split, reorder, omit, duplicate, rotate pages | Bundled Python + pypdf + PDFium + Pillow |
| Text | TXT, Markdown, HTML | HTML escaping and Unicode text handling | .NET |
| Subtitles | SRT, VTT, TXT | SRT/VTT conversion preserves cue timing | .NET |
| ZIP | ZIP archive or extracted folder | Files, folders, mixed selections; path and expanded-size checks | .NET |
| Office documents | DOCX, ODT, RTF, TXT, HTML, PDF, EPUB | Format conversion | LibreOffice |
| Spreadsheets | XLSX, ODS, CSV, TSV, PDF | Format conversion | LibreOffice |
| Presentations | PPTX, ODP, PDF | Format conversion | LibreOffice |

## Selection behavior

Select multiple images for a multi-page PDF or a collage of 2–64 images. Multiple PDFs enable merge; multiple videos enable join. Join order follows selection order. A single file's current extension is usually omitted from the conversion wheel. Additional operations appear on subsequent wheel pages.

File actions support files and folders: copy files to the clipboard, copy paths, float references on a shelf, reveal in Explorer, copy to a chosen folder, or move originals to a chosen folder.

## Limits to know

- Animated image imports use the first frame. HEIC/HEIF input depends on installed Windows decoders or FFmpeg; HEIC export is unavailable.
- Text/Markdown/DOCX/EPUB PDF exports extract selectable text and have limited layout preservation. Scanned PDFs require OCR elsewhere. PDF-to-PPTX/XLSX is unavailable.
- PDF compression compresses content streams losslessly and may not shrink image-heavy files. Rendering rejects pages above 100 megapixels at 300 DPI.
- Video crop and redaction take explicit pixel rectangles. Redaction covers that rectangle for the whole clip; there is no automatic face or text tracking.
- Codec support varies by FFmpeg build. LibreOffice conversions have not been validated on the development machine.
- ZIP extraction rejects path traversal and enforces entry-count and 20 GB expanded-size limits. TAR, GZIP, and RAR extraction are unavailable.
- There is no OCR, annotation editor, advanced photo adjustment suite, macOS Pages/RTFD support, or AirDrop integration. Keyboard modifiers are currently fixed.

See the [user guide](USER_GUIDE.md) for saving, cancellation, and workflow details.
