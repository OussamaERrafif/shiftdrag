# Development

## Prerequisites

- Windows 10 or 11 with .NET Framework 4.8.
- Windows PowerShell 5.1 or a PowerShell session able to run Windows executables.
- Git for cloning. GitHub CLI is only needed for repository publishing.

The project targets C# syntax supported by the Windows .NET Framework compiler. It has no NuGet packages or .NET SDK requirement. The build expects `csc.exe` in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319` and references the Framework/WPF assemblies there.

## Compile and launch

From the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
.\build\ShiftDrag.exe
```

Quit an already running app from its tray menu before rebuilding the same executable. Closing the window leaves the process running. Build output is ignored by Git.

The launcher `Start ShiftDrag.cmd` compiles if the executable is missing. It does not rebuild automatically after source changes; run `build.ps1` for those.

## Optional PDF engine

The PDF helper needs a **full 64-bit CPython 3.12 installation**, with its `Lib`, `DLLs`, Python DLLs, Visual C++ runtime DLLs, and license. The bundler uses Python 3.12 filenames; a virtual environment or the small embeddable ZIP alone does not match that layout.

Install the pinned packages into the Python installation you intend to bundle:

```powershell
# Change this example to your own full Python 3.12 installation directory.
$pythonRoot = 'C:\Python312'
& (Join-Path $pythonRoot 'python.exe') -m pip install -r .\requirements-pdf.txt
.\bundle-pdf-engine.ps1 -PythonSource $pythonRoot
.\build.ps1
& .\build\engines\python\python.exe -c "import pypdf, pypdfium2, PIL; print('PDF engine ready')"
```

The script copies the runtime, creates a standard-library ZIP, copies packages and their license notices, and writes an isolated `python312._pth`. Runtime package installation needs network access; the app's PDF processing runs offline afterward. The script reuses an existing standard-library ZIP; use a fresh build directory when switching Python runtimes.

## Optional media and Office engines

FFmpeg discovery checks the Settings override, `build/engines/ffmpeg.exe`, `C:\ffmpeg\bin\ffmpeg.exe`, and PATH. Keep `ffprobe.exe` alongside FFmpeg or available on PATH for speed/join operations. Tests that compare compressed packets expect ffprobe beside FFmpeg.

LibreOffice discovery checks the Settings override, standard Program Files locations, and PATH. This engine is optional; Office conversion still requires validation on a machine with LibreOffice installed.

FFmpeg and LibreOffice are not downloaded or bundled by these scripts. Keep their licensing and build configuration in mind if you distribute an independently assembled package.

## Verify

```powershell
$testProcess = Start-Process -FilePath .\build\ShiftDrag.exe -ArgumentList '--self-test' -Wait -PassThru
Get-Content .\test-results\latest.txt
if ($testProcess.ExitCode -ne 0) { throw 'Self-tests failed.' }
```

The test executable is a Windows GUI binary, so `Start-Process -Wait` ensures PowerShell waits for completion. Optional engine checks run only when those engines are detected. See [testing](TESTING.md). To validate local documentation links and screenshot references, run `python scripts/check-docs.py` with Python 3.9 or newer.

## Package

```powershell
.\package.ps1
```

This creates `dist/ShiftDrag-Windows.zip`, with a `ShiftDrag/` folder containing the executable, XAML, helper, documentation, license, and any engines already present in `build/`. It does not install or discover engines for redistribution. Extract the whole archive before launching; preserve the folder structure. Do not include local logs, user settings, or test fixtures when assembling a release.

## Repository map

| Path | Purpose |
|---|---|
| `src/App.cs`, `src/Main.xaml` | Dashboard, tray lifecycle, shelf, preferences, UI orchestration |
| `src/Radial.cs` | Keyboard/drag watching, shortcut gate, wheel layout/drawing, OLE drop target |
| `src/Models.cs` | Settings, file families, context-sensitive operation catalog |
| `src/Engine.cs` | Conversion execution, safe outputs, image/media/ZIP processing, engine discovery |
| `src/CropEditor.cs` | Visual image crop editor |
| `src/DragLab.cs` | Generated sample and real OLE drag testing surface |
| `src/pdf_engine.py` | Local PDF rendering, extraction, and page operations |
| `src/SelfTests.cs` | Functional and regression checks |
| `src/app.manifest` | Windows compatibility, DPI, normal-user execution |
| `assets/brand/`, `docs/assets/mark.svg` | Exported icons and editable logo source |
| `scripts/export-brand.ps1` | Reproducible Windows icon/PNG generation |
| `benchmarks/` | Measurement runner, CSVs, analysis, and report |
| `docs/`, `research/` | Product/developer guides, screenshots, reference investigation |
| `.github/workflows/build.yml` | Windows compilation and available self-tests |

For data flow and design decisions, see [architecture](ARCHITECTURE.md).
