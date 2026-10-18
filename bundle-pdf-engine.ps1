param([Parameter(Mandatory=$true)][string]$PythonSource)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$destination = Join-Path $PSScriptRoot 'build\engines\python'
New-Item -ItemType Directory -Force $destination | Out-Null
foreach ($file in @('python.exe','python3.dll','python312.dll','vcruntime140.dll','vcruntime140_1.dll','LICENSE.txt')) {
    Copy-Item -LiteralPath (Join-Path $PythonSource $file) -Destination $destination
}
Copy-Item -LiteralPath (Join-Path $PythonSource 'DLLs') -Destination $destination -Recurse -Force
$libraryZip = Join-Path $destination 'python312.zip'
if (-not (Test-Path -LiteralPath $libraryZip)) {
    $archive = [IO.Compression.ZipFile]::Open($libraryZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $libraryRoot = (Join-Path $PythonSource 'Lib') + '\'
        Get-ChildItem -LiteralPath $libraryRoot -Recurse -File | Where-Object {
            $relative = $_.FullName.Substring($libraryRoot.Length)
            $relative -notmatch '^(site-packages|test|idlelib|tkinter|turtledemo|ensurepip)\\' -and $relative -notmatch '(__pycache__|\.pyc$)'
        } | ForEach-Object {
            $relative = $_.FullName.Substring($libraryRoot.Length).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$_.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
}
$packages = Join-Path $destination 'Lib\site-packages'
New-Item -ItemType Directory -Force $packages | Out-Null
foreach ($package in @('pypdf','pypdfium2','pypdfium2_raw','pypdfium2_cfg','PIL')) {
    Copy-Item -LiteralPath (Join-Path $PythonSource ('Lib\site-packages\' + $package)) -Destination $packages -Recurse -Force
}
Get-ChildItem -LiteralPath (Join-Path $PythonSource 'Lib\site-packages') -Directory | Where-Object { $_.Name -match '^(pypdf|pypdfium2|pillow)-.+\.dist-info$' } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $packages -Recurse -Force }
@('python312.zip','DLLs','Lib\site-packages','.','import site') | Set-Content -LiteralPath (Join-Path $destination 'python312._pth') -Encoding ascii
Write-Output "Bundled local PDF engine: $destination"
