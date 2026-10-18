# Conversion benchmark

Measured on this PC with the same source files, three runs per case; values below are medians. This is a small local comparison, not a universal speed claim. The CSV files retain every measurement.

| Operation | Previous build | Updated build | Relative speed |
|---|---:|---:|---:|
| image-jpeg | 250 ms | 284 ms | 0.88× |
| image-rotate | 1361 ms | 953 ms | 1.43× |
| video-convert | 1780 ms | 876 ms | 2.03× |
| video-mute | 1396 ms | 145 ms | 9.63× |

Inputs: a deterministic 2400 × 1600 RGB noise PNG and a four-second 1280 × 720, 30 fps H.264 test clip. Engine construction is outside the timer; the first timed run includes JIT/startup work. No self-test suite or other benchmark ran concurrently.

Video conversion changes x264 from `medium` to `veryfast` at the same CRF 22. The speed setting is configurable; output size/quality tradeoffs depend on content. Muting compatible media copies the video stream without re-encoding, with a re-encoding fallback for incompatible streams. Image export uses a native pixel snapshot instead of two managed pixel buffers; metadata stripping is regression-tested. JPEG timing did not improve consistently and no JPEG speedup is claimed.
