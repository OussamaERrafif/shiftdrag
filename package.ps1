$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = $PSScriptRoot
$buildRoot = Join-Path $root 'build'
if (-not (Test-Path -LiteralPath (Join-Path $buildRoot 'ShiftDrag.exe'))) { throw 'Run build.ps1 first.' }
Copy-Item -LiteralPath (Join-Path $root 'docs\PORTABLE_README.md') -Destination (Join-Path $buildRoot 'README.md')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $buildRoot
Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination $buildRoot
Copy-Item -LiteralPath (Join-Path $root 'benchmarks\PERFORMANCE.md') -Destination $buildRoot
@('@echo off','start "" "%~dp0ShiftDrag.exe"') | Set-Content -LiteralPath (Join-Path $buildRoot 'Start ShiftDrag.cmd') -Encoding ascii
$distRoot = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $distRoot | Out-Null
$archivePath = Join-Path $distRoot 'ShiftDrag-Windows.zip'
if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath }
$archive = [IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try {
    $prefix = $buildRoot + '\'
    Get-ChildItem -LiteralPath $buildRoot -Recurse -File | Where-Object { $_.FullName -notmatch '\\__pycache__\\|\.pyc$' -and $_.Name -ne 'BenchmarkRunner.exe' } | ForEach-Object {
        $entry = 'ShiftDrag/' + $_.FullName.Substring($prefix.Length).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$_.FullName,$entry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
Get-Item -LiteralPath $archivePath | Select-Object FullName,Length
