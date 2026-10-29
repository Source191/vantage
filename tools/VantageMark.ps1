<#
.SYNOPSIS
The Vantage mark (two worktree branches merging into one node, shaped as a V on a rounded tile), drawn once for every script that needs it.
.DESCRIPTION
Dot-source it: New-Icon.ps1 draws the icon with it and New-Header.ps1 draws the mark on the header images. The mark is laid out on a
512 px square and scaled to the size asked for, so every image carries the same drawing.
#>

Add-Type -AssemblyName System.Drawing

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

function Add-VantageMark([System.Drawing.Graphics]$graphics, [float]$x, [float]$y, [float]$size) {
    $layout = 512
    $state = $graphics.Save()
    $graphics.TranslateTransform($x, $y)
    $graphics.ScaleTransform($size / $layout, $size / $layout)
    $graphics.SetClip((New-RoundedRectangle $layout 96))
    $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new((PointOf 0 0), (PointOf 0 $layout), (ColourOf '#1E1B4B'), (ColourOf '#0F172A'))
    $graphics.FillRectangle($background, 0, 0, $layout, $layout)
    # The stem is drawn first so the branches' round ends cover its top and the join is clean.
    $graphics.DrawLine((New-Pen '#FFFFFF' 40), 256, 330, 256, 408)
    $graphics.DrawLine((New-Pen '#2DD4BF' 40), 124, 112, 256, 330)
    $graphics.DrawLine((New-Pen '#A78BFA' 40), 388, 112, 256, 330)
    Add-Node $graphics 124 104 46 '#2DD4BF'
    Add-Node $graphics 388 104 46 '#A78BFA'
    Add-Node $graphics 256 408 46 '#FFFFFF'
    $graphics.Restore($state)
}
