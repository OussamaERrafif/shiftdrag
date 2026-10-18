$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
New-Item -ItemType Directory -Force (Join-Path $root 'build') | Out-Null
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Runtime.Serialization.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Xml.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
$brandResources = @(('/resource:' + (Join-Path $root 'assets\brand\ShiftDrag.ico') + ',ShiftDrag.Brand.ico'), ('/resource:' + (Join-Path $root 'assets\brand\icon-256.png') + ',ShiftDrag.Brand.png'))
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /out:"$root\build\ShiftDrag.exe" /win32manifest:"$root\src\app.manifest" /win32icon:"$root\assets\brand\ShiftDrag.ico" @brandResources @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $root 'src\Main.xaml') -Destination (Join-Path $root 'build\Main.xaml')
Copy-Item -LiteralPath (Join-Path $root 'src\pdf_engine.py') -Destination (Join-Path $root 'build\pdf_engine.py')
Write-Output "Built $root\build\ShiftDrag.exe"
