<#
.SYNOPSIS
Writes the console's icon as raw pixels: assets\icon\vantage-48.rgba, from assets\icon\vantage-48.png.
.DESCRIPTION
The console draws the icon through Terminal.Gui's ImageView, which takes pixels and has no image decoder, so the
pixels are committed beside the PNGs and embedded in the Tui assembly. The file is the width and the height as one
byte each, then four bytes (red, green, blue, alpha) per pixel, row by row. Run it after New-Icon.ps1 changes the drawing.
#>
[CmdletBinding()]
param(
    [string]$IconFolder = (Join-Path $PSScriptRoot '..\assets\icon'),
    [int]$Size = 48
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = Join-Path $IconFolder "vantage-$Size.png"
$target = Join-Path $IconFolder "vantage-$Size.rgba"
$bitmap = [System.Drawing.Bitmap]::new($source)
try {
    $bytes = [System.Collections.Generic.List[byte]]::new()
    $bytes.Add([byte]$bitmap.Width)
    $bytes.Add([byte]$bitmap.Height)
    foreach ($y in 0..($bitmap.Height - 1)) {
        foreach ($x in 0..($bitmap.Width - 1)) {
            $pixel = $bitmap.GetPixel($x, $y)
            $bytes.AddRange([byte[]]@($pixel.R, $pixel.G, $pixel.B, $pixel.A))
        }
    }

    [System.IO.File]::WriteAllBytes($target, $bytes.ToArray())
    Write-Output "Wrote $target ($($bitmap.Width) x $($bitmap.Height), $($bytes.Count) bytes)."
}
finally {
    $bitmap.Dispose()
}
