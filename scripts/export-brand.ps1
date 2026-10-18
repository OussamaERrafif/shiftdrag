# Render the SVG's filled path geometry with WPF; no external graphics tools.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$projectRoot = Split-Path $PSScriptRoot -Parent
$brandRoot = Join-Path $projectRoot 'assets\brand'
New-Item -ItemType Directory -Force $brandRoot | Out-Null
[xml]$sourceSvg = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'docs\assets\mark.svg')
$namespaces = New-Object System.Xml.XmlNamespaceManager($sourceSvg.NameTable)
$namespaces.AddNamespace('svg','http://www.w3.org/2000/svg')
$brushes = @{}
foreach ($gradient in $sourceSvg.SelectNodes('//svg:linearGradient',$namespaces)) {
    $brush = New-Object System.Windows.Media.LinearGradientBrush
    $brush.StartPoint = New-Object System.Windows.Point(0,0)
    $brush.EndPoint = New-Object System.Windows.Point(1,1)
    foreach ($stop in $gradient.SelectNodes('svg:stop',$namespaces)) {
        $color = [System.Windows.Media.ColorConverter]::ConvertFromString($stop.GetAttribute('stop-color'))
        $offset = [double]::Parse($stop.GetAttribute('offset'),[Globalization.CultureInfo]::InvariantCulture)
        $brush.GradientStops.Add((New-Object System.Windows.Media.GradientStop($color,$offset)))
    }
    $brush.Freeze()
    $brushes[$gradient.id] = $brush
}
$pngs = @{}
foreach ($size in @(16,20,24,32,40,48,64,128,256,512)) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    $drawing = $visual.RenderOpen()
    try {
        $drawing.PushTransform((New-Object System.Windows.Media.ScaleTransform(($size/256.0),($size/256.0))))
        foreach ($path in $sourceSvg.SelectNodes('/svg:svg/svg:path',$namespaces)) {
            $fill = $path.GetAttribute('fill')
            if ($fill -match '^url\(#(.+)\)$') { $fillBrush = $brushes[$Matches[1]] }
            else { $fillBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString($fill) }
            $drawing.DrawGeometry($fillBrush,$null,[System.Windows.Media.Geometry]::Parse($path.d))
        }
        $drawing.Pop()
    } finally { $drawing.Close() }
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($size,$size,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $memory = New-Object System.IO.MemoryStream
    try {
        $encoder.Save($memory)
        $pngs[$size] = $memory.ToArray()
        [IO.File]::WriteAllBytes((Join-Path $brandRoot "icon-$size.png"),$pngs[$size])
    } finally { $memory.Dispose() }
}
# PNG-backed multi-resolution ICO works on all supported Windows versions.
$iconSizes = @(16,20,24,32,40,48,64,128,256)
$stream = [IO.File]::Create((Join-Path $brandRoot 'ShiftDrag.ico'))
$writer = New-Object IO.BinaryWriter($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$iconSizes.Count)
    $offset = 6 + 16 * $iconSizes.Count
    foreach ($size in $iconSizes) {
        $dimension = if ($size -eq 256) { 0 } else { $size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$pngs[$size].Length); $writer.Write([uint32]$offset)
        $offset += $pngs[$size].Length
    }
    foreach ($size in $iconSizes) { $writer.Write([byte[]]$pngs[$size]) }
} finally { $writer.Dispose() }
Write-Output 'Exported ShiftDrag PNGs (16-512 px) and multi-resolution Windows ICO.'
