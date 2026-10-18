"""Offline PDF operations. Invoked only by ShiftDrag with a local JSON request."""
import io
import json
import re
import sys
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

from pypdf import PdfReader, PdfWriter
import pypdfium2 as pdfium


def save(writer, path):
    with open(path, "wb") as stream:
        writer.write(stream)


def render(path, folder, extension):
    folder.mkdir(parents=True, exist_ok=True)
    document = pdfium.PdfDocument(path)
    try:
        for i in range(len(document)):
            page = document[i]
            width, height = page.get_size()
            if width * height * (300 / 72) ** 2 > 100_000_000:
                raise ValueError("Page exceeds the 100 megapixel render limit at 300 DPI")
            bitmap = page.render(scale=300 / 72)
            try:
                image = bitmap.to_pil()
                if extension == "jpg":
                    image = image.convert("RGB")
                image.save(folder / f"page-{i + 1:03}.{extension}")
            finally:
                bitmap.close()
                page.close()
    finally:
        document.close()


def docx(pages, output):
    paragraphs = []
    for i, text in enumerate(pages):
        if i:
            paragraphs.append('<w:p><w:r><w:br w:type="page"/></w:r></w:p>')
        for line in text.splitlines():
            paragraphs.append(f'<w:p><w:r><w:t xml:space="preserve">{escape(line)}</w:t></w:r></w:p>')
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("[Content_Types].xml", '<?xml version="1.0"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>')
        archive.writestr("_rels/.rels", '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>')
        archive.writestr("word/document.xml", '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>' + "".join(paragraphs) + '<w:sectPr/></w:body></w:document>')


def epub(pages, output):
    with zipfile.ZipFile(output, "w") as archive:
        archive.writestr("mimetype", "application/epub+zip", compress_type=zipfile.ZIP_STORED)
        archive.writestr("META-INF/container.xml", '<?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="content.opf" media-type="application/oebps-package+xml"/></rootfiles></container>')
        archive.writestr("content.opf", '<?xml version="1.0"?><package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="book"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:identifier id="book">shiftdrag-local-export</dc:identifier><dc:title>Converted document</dc:title><dc:language>en</dc:language><meta property="dcterms:modified">2026-01-01T00:00:00Z</meta></metadata><manifest><item id="content" href="content.xhtml" media-type="application/xhtml+xml"/><item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/></manifest><spine><itemref idref="content"/></spine></package>')
        archive.writestr("content.xhtml", '<?xml version="1.0" encoding="utf-8"?><html xmlns="http://www.w3.org/1999/xhtml"><head><title>Converted document</title></head><body>' + "".join(f'<section id="p{i}">' + "".join(f"<p>{escape(line)}</p>" for line in text.splitlines()) + "</section>" for i, text in enumerate(pages)) + "</body></html>")
        archive.writestr("nav.xhtml", '<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops"><head><title>Contents</title></head><body><nav epub:type="toc"><ol>' + "".join(f'<li><a href="content.xhtml#p{i}">Page {i+1}</a></li>' for i in range(len(pages))) + "</ol></nav></body></html>")


def main(request):
    files = request["files"]
    output = Path(request["output"])
    operation = request["operation"]
    options = request.get("options", {})
    if operation in ("to:png", "to:jpg"):
        render(files[0], output, operation[3:])
        return
    reader = PdfReader(files[0])
    if reader.is_encrypted:
        raise ValueError("Encrypted PDFs need an unlocked copy before conversion")
    if operation == "splitpdf":
        output.mkdir(parents=True)
        for i, page in enumerate(reader.pages):
            writer = PdfWriter()
            writer.add_page(page)
            save(writer, output / f"page-{i+1:03}.pdf")
        return
    if operation.startswith("to:"):
        pages = [page.extract_text() or "" for page in reader.pages]
        if not any(text.strip() for text in pages):
            raise ValueError("This PDF has no selectable text. OCR is not included; use PNG or JPG export")
        ext = operation[3:]
        if ext == "docx":
            docx(pages, output)
        elif ext == "epub":
            epub(pages, output)
        else:
            output.write_text("\n\n".join(pages), encoding="utf-8")
        return
    writer = PdfWriter()
    if operation == "mergepdf":
        for file in files:
            writer.append(file)
    elif operation == "organizepdf":
        selection = options.get("pages", "").strip()
        if not selection:
            raise ValueError("Enter page numbers separated by commas (for example 3,1,2)")
        numbers = [int(n.strip()) for n in selection.split(",")]
        rotation = int(options.get("rotation", "0"))
        if rotation not in (0, 90, 180, 270):
            raise ValueError("Rotation must be 0, 90, 180, or 270")
        for number in numbers:
            if number < 1 or number > len(reader.pages):
                raise ValueError(f"Page {number} does not exist; document has {len(reader.pages)} pages")
            page = writer.add_page(reader.pages[number - 1])
            if rotation:
                page.rotate(rotation)
    else:
        writer.append(reader)
        if operation == "compress":
            for page in writer.pages:
                page.compress_content_streams()
        if operation == "strip":
            writer.metadata = None
            writer._root_object.pop("/Metadata", None)
    save(writer, output)


if __name__ == "__main__":
    try:
        main(json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig")))
    except Exception as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
