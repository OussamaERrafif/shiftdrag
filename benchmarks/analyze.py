import csv
import statistics
from pathlib import Path

root = Path(__file__).parent
data = {name: list(csv.DictReader((root / f'{name}.csv').open())) for name in ('before', 'after')}
rows = []
for case in dict.fromkeys(row['case'] for row in data['before']):
    values = {name: statistics.median(int(row['milliseconds']) for row in items if row['case'] == case) for name, items in data.items()}
    rows.append(f"| {case} | {values['before']} ms | {values['after']} ms | {values['before']/values['after']:.2f}× |")
report = '''# Conversion benchmark

Measured on this PC with the same source files, three runs per case; values below are medians. This is a small local comparison, not a universal speed claim. The CSV files retain every measurement.

| Operation | Previous build | Updated build | Relative speed |
|---|---:|---:|---:|
''' + '\n'.join(rows) + '''

Inputs: a deterministic 2400 × 1600 RGB noise PNG and a four-second 1280 × 720, 30 fps H.264 test clip. Engine construction is outside the timer; the first timed run includes JIT/startup work. No self-test suite or other benchmark ran concurrently.

Video conversion changes x264 from `medium` to `veryfast` at the same CRF 22. The speed setting is configurable; output size/quality tradeoffs depend on content. Muting compatible media copies the video stream without re-encoding, with a re-encoding fallback for incompatible streams. Image export uses a native pixel snapshot instead of two managed pixel buffers; metadata stripping is regression-tested. JPEG timing did not improve consistently and no JPEG speedup is claimed.
'''
(root / 'PERFORMANCE.md').write_text(report, encoding='utf-8')
print(report)
