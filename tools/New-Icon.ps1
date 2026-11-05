<#
.SYNOPSIS
Draws the icon (two worktree branches meeting in the bar of a T over a stem to one node) at every size.
.DESCRIPTION
Writes assets\icon\logo-<size>.png for each size and assets\icon\logo.ico (PNG-compressed entries, up to 256 px: the format cannot hold 512), then checks
every file. The output is committed, so a build never needs this script; run it to change the drawing.
#>
[CmdletBinding()]
param(
    [string]$OutputFolder = (Join-Path $PSScriptRoot '..\assets\icon')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LogoMark.ps1')

$sizes = 16, 32, 48, 64, 128, 256, 512
$master = 512

function New-Master {
    $bitmap = [System.Drawing.Bitmap]::new($master, $master)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.PixelOffsetMode = 'HighQuality'
    Add-LogoMark $graphics 0 0 $master
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
    [System.IO.File]::WriteAllBytes((Join-Path $OutputFolder "logo-$size.png"), $pngBySize[$size])
}
[System.IO.File]::WriteAllBytes((Join-Path $OutputFolder 'logo.ico'), (New-IconFile ($pngBySize.GetEnumerator() | Where-Object { $_.Key -le 256 } | ForEach-Object -Begin { $small = @{} } -Process { $small[$_.Key] = $_.Value } -End { $small })))

foreach ($size in $sizes) {
    $image = [System.Drawing.Image]::FromFile((Join-Path $OutputFolder "logo-$size.png"))
    $actual = "$($image.Width)x$($image.Height)"
    $image.Dispose()
    if ($actual -ne "${size}x${size}") {
        throw "logo-$size.png is $actual."
    }
}

$icon = [System.IO.File]::ReadAllBytes((Join-Path $OutputFolder 'logo.ico'))
$entries = [System.BitConverter]::ToUInt16($icon, 4)
$iconSizes = @($sizes | Where-Object { $_ -le 256 })
if ($entries -ne $iconSizes.Count) {
    throw "logo.ico lists $entries images, expected $($iconSizes.Count)."
}

Write-Host "Wrote $($sizes.Count) PNGs and logo.ico to $OutputFolder"
