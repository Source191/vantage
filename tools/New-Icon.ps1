<#
.SYNOPSIS
Draws the Vantage icon (two worktree branches merging into one node, shaped as a V) at every size.
.DESCRIPTION
Writes assets\icon\vantage-<size>.png for each size and assets\icon\vantage.ico (PNG-compressed entries, up to 256 px: the format cannot hold 512), then checks
every file. The output is committed, so a build never needs this script; run it to change the drawing.
src\Source191.Vantage\icon.png is the 128 px file and is copied by hand (a test keeps the two in step).
#>
[CmdletBinding()]
param(
    [string]$OutputFolder = (Join-Path $PSScriptRoot '..\assets\icon')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sizes = 16, 32, 48, 64, 128, 256, 512
$master = 512

function ColourOf([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }
function PointOf([double]$x, [double]$y) { [System.Drawing.PointF]::new($x, $y) }

function New-RoundedRectangle([float]$size, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc(0, 0, $diameter, $diameter, 180, 90)
    $path.AddArc($size - $diameter, 0, $diameter, $diameter, 270, 90)
    $path.AddArc($size - $diameter, $size - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc(0, $size - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $path
}

function New-Pen([string]$hex, [float]$width) {
    $pen = [System.Drawing.Pen]::new((ColourOf $hex), $width)
    $pen.StartCap = 'Round'
    $pen.EndCap = 'Round'
    $pen
}

function Add-Node([System.Drawing.Graphics]$graphics, [float]$x, [float]$y, [float]$radius, [string]$ring) {
    $graphics.FillEllipse([System.Drawing.SolidBrush]::new((ColourOf '#0F172A')), $x - $radius, $y - $radius, 2 * $radius, 2 * $radius)
    $inner = $radius * 0.78
    $graphics.DrawEllipse((New-Pen $ring ($radius * 0.55)), $x - $inner, $y - $inner, 2 * $inner, 2 * $inner)
}

function New-Master {
    $bitmap = [System.Drawing.Bitmap]::new($master, $master)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.SetClip((New-RoundedRectangle $master 96))
    $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new((PointOf 0 0), (PointOf 0 $master), (ColourOf '#1E1B4B'), (ColourOf '#0F172A'))
    $graphics.FillRectangle($background, 0, 0, $master, $master)
    # The stem is drawn first so the branches' round ends cover its top and the join is clean.
    $graphics.DrawLine((New-Pen '#FFFFFF' 40), 256, 330, 256, 408)
    $graphics.DrawLine((New-Pen '#2DD4BF' 40), 124, 112, 256, 330)
    $graphics.DrawLine((New-Pen '#A78BFA' 40), 388, 112, 256, 330)
    Add-Node $graphics 124 104 46 '#2DD4BF'
    Add-Node $graphics 388 104 46 '#A78BFA'
    Add-Node $graphics 256 408 46 '#FFFFFF'
    $graphics.Dispose()
    $bitmap
}

function Get-Resized([System.Drawing.Bitmap]$source, [int]$size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = 'HighQualityBicubic'
    $graphics.SmoothingMode = 'HighQuality'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.CompositingMode = 'SourceCopy'
    $graphics.DrawImage($source, 0, 0, $size, $size)
    $graphics.Dispose()
    $bitmap
}

function Get-PngBytes([System.Drawing.Bitmap]$bitmap) {
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $stream.ToArray()
}

function New-IconFile([hashtable]$pngBySize) {
    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$pngBySize.Count)
    $offset = 6 + 16 * $pngBySize.Count
    foreach ($size in ($pngBySize.Keys | Sort-Object)) {
        $bytes = $pngBySize[$size]
        $writer.Write([byte]($size -eq 256 ? 0 : $size)); $writer.Write([byte]($size -eq 256 ? 0 : $size))
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$bytes.Length); $writer.Write([uint32]$offset)
        $offset += $bytes.Length
    }
    foreach ($size in ($pngBySize.Keys | Sort-Object)) {
        $writer.Write([byte[]]$pngBySize[$size])
    }
    $writer.Flush()
    $stream.ToArray()
}

New-Item -ItemType Directory -Force $OutputFolder | Out-Null
$OutputFolder = (Resolve-Path $OutputFolder).Path
$drawing = New-Master
$pngBySize = @{}
foreach ($size in $sizes) {
    $resized = $size -eq $master ? $drawing : (Get-Resized $drawing $size)
    $pngBySize[$size] = Get-PngBytes $resized
    [System.IO.File]::WriteAllBytes((Join-Path $OutputFolder "vantage-$size.png"), $pngBySize[$size])
}
[System.IO.File]::WriteAllBytes((Join-Path $OutputFolder 'vantage.ico'), (New-IconFile ($pngBySize.GetEnumerator() | Where-Object { $_.Key -le 256 } | ForEach-Object -Begin { $small = @{} } -Process { $small[$_.Key] = $_.Value } -End { $small })))

foreach ($size in $sizes) {
    $image = [System.Drawing.Image]::FromFile((Join-Path $OutputFolder "vantage-$size.png"))
    $actual = "$($image.Width)x$($image.Height)"
    $image.Dispose()
    if ($actual -ne "${size}x${size}") {
        throw "vantage-$size.png is $actual."
    }
}

$icon = [System.IO.File]::ReadAllBytes((Join-Path $OutputFolder 'vantage.ico'))
$entries = [System.BitConverter]::ToUInt16($icon, 4)
$iconSizes = @($sizes | Where-Object { $_ -le 256 })
if ($entries -ne $iconSizes.Count) {
    throw "vantage.ico lists $entries images, expected $($iconSizes.Count)."
}

Write-Host "Wrote $($sizes.Count) PNGs and vantage.ico to $OutputFolder"
