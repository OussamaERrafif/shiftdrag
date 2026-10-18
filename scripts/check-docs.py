"""Validate repository-local Markdown links, anchors, and embedded images."""
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
FILES = sorted(ROOT.glob("*.md"))
for directory in ("docs", "research", "benchmarks", ".github"):
    FILES.extend(sorted((ROOT / directory).rglob("*.md")))


def anchors(path):
    counts = {}
    found = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        heading = re.match(r"^#{1,6}\s+(.+?)\s*#*\s*$", line)
        if not heading:
            continue
        slug = re.sub(r"[^\w\- ]", "", heading[1].lower()).replace(" ", "-")
        duplicate = counts.get(slug, 0)
        counts[slug] = duplicate + 1
        found.add(slug if duplicate == 0 else f"{slug}-{duplicate}")
    return found


errors = []
checked = 0
for source in FILES:
    content = source.read_text(encoding="utf-8")
    links = re.findall(r"!?\[[^\]\n]*\]\(([^)\n]+)\)", content)
    links += re.findall(r'<(?:img|a)\b[^>]*\b(?:src|href)="([^"]+)"', content)
    for link in links:
        parsed = urlsplit(link.strip("<>"))
        if parsed.scheme or parsed.netloc:
            continue
        target = (source.parent / unquote(parsed.path)).resolve() if parsed.path else source
        checked += 1
        if not target.is_relative_to(ROOT) or not target.exists():
            errors.append(f"{source.relative_to(ROOT)}: missing local target {link}")
        elif parsed.fragment and target.suffix.lower() == ".md":
            if unquote(parsed.fragment) not in anchors(target):
                errors.append(f"{source.relative_to(ROOT)}: missing heading {link}")

if errors:
    print("\n".join(errors))
    sys.exit(1)
print(f"Documentation OK: {len(FILES)} Markdown files, {checked} local links/images.")
