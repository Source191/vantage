<#
.SYNOPSIS
Writes the console's icon as raw pixels: assets\icon\vantage-48.rgba, the Vantage icon with an edge round its tile in the console's frame grey.
.DESCRIPTION
The console draws the icon through Terminal.Gui's ImageView, which takes pixels and has no image decoder, so the
pixels are committed beside the PNGs and embedded in the Tui assembly. The dark tile vanished against the console's
black, so this copy has an edge round it in the grey of the console's frames (white was too stark, and the mark alone read worse); the tile's rounded
corners stay transparent and the console blends them with its background. The file is the width and the height as
one byte each, then four bytes (red, green, blue, alpha) per pixel, row by row. Run it after VantageMark.ps1 changes
the drawing.
#>
[CmdletBinding()]
param(
    [string]$IconFolder = (Join-Path $PSScriptRoot '..\assets\icon'),
    [int]$Size = 48,
    [string]$Outline = '#7A7574'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VantageMark.ps1')

$master = 512
$target = Join-Path $IconFolder "vantage-$Size.rgba"
$large = [System.Drawing.Bitmap]::new($master, $master)
$small = [System.Drawing.Bitmap]::new($Size, $Size)
try {
    $graphics = [System.Drawing.Graphics]::FromImage($large)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.PixelOffsetMode = 'HighQuality'
    Add-VantageMark $graphics 0 0 $master -Outline $Outline
    $graphics.Dispose()

    $graphics = [System.Drawing.Graphics]::FromImage($small)
    $graphics.InterpolationMode = 'HighQualityBicubic'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.DrawImage($large, 0, 0, $Size, $Size)
    $graphics.Dispose()

    $bytes = [System.Collections.Generic.List[byte]]::new()
    $bytes.Add([byte]$Size)
    $bytes.Add([byte]$Size)
    foreach ($y in 0..($Size - 1)) {
        foreach ($x in 0..($Size - 1)) {
            $pixel = $small.GetPixel($x, $y)
            $bytes.AddRange([byte[]]@($pixel.R, $pixel.G, $pixel.B, $pixel.A))
        }
    }

    [System.IO.File]::WriteAllBytes($target, $bytes.ToArray())
    Write-Output "Wrote $target ($Size x $Size, $($bytes.Count) bytes)."
}
finally {
    $large.Dispose()
    $small.Dispose()
}
