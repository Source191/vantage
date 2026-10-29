<#
.SYNOPSIS
Draws the Vantage header (the mark, the name, the tagline and the install line over a faint git graph) in three layouts.
.DESCRIPTION
Writes to assets\header:
- buy-me-a-coffee-cover.png, 3200 x 800. Buy Me a Coffee shows a cover full width at about 390 px tall and crops it, and its About and
  Support cards overlap the bottom of the middle half, so the words stay in the middle half and above that strip.
- readme-banner.png, 1600 x 400, the top of the README (shown at about 800 px wide, sharp on high-DPI screens).
- social-preview.png, 1280 x 640, the repository's social preview (uploaded by hand under Settings > General); GitHub crops 40 px at each edge.
With -QrCode, also writes assets\support\buy-me-a-coffee-qr.png, that QR code at 160 px (Markdown shows an image at its own size, and the README avoids HTML).
The output is committed, so a build never needs this script; run it to change the drawing. The mark comes from VantageMark.ps1, as the icon's does.
#>
[CmdletBinding()]
param(
    [string]$OutputFolder = (Join-Path $PSScriptRoot '..\assets\header'),
    [string]$QrCode,
    [string]$QrOutputFolder = (Join-Path $PSScriptRoot '..\assets\support')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VantageMark.ps1')

$title = 'Vantage'
$tagline = 'A console for Claude Code sessions', 'and git worktrees on Windows.'
$install = 'dotnet tool install -g Source191.Vantage'

function Get-FontFamily([string]$name) {
    # FontFamily throws for a family that is not installed, where Font would quietly substitute another.
    [System.Drawing.FontFamily]::new($name)
}

$titleFamily = Get-FontFamily 'Segoe UI Semibold'
$monoFamily = Get-FontFamily 'Cascadia Mono'
$typographic = [System.Drawing.StringFormat]::GenericTypographic

function New-Font([System.Drawing.FontFamily]$family, [float]$pixels) {
    [System.Drawing.Font]::new($family, $pixels, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
}

function ColourWithAlpha([string]$hex, [int]$alpha) { [System.Drawing.Color]::FromArgb($alpha, (ColourOf $hex)) }

# The text block's lines, each sized as a fraction of the mark so the whole group scales as one.
function Get-Lines([float]$mark) {
    @(
        [pscustomobject]@{ Text = $title; Font = (New-Font $titleFamily ($mark * 0.40)); Colour = '#FFFFFF'; Before = 0 }
        [pscustomobject]@{ Text = $tagline[0]; Font = (New-Font $monoFamily ($mark * 0.098)); Colour = '#CBD5E1'; Before = $mark * 0.03 }
        [pscustomobject]@{ Text = $tagline[1]; Font = (New-Font $monoFamily ($mark * 0.098)); Colour = '#CBD5E1'; Before = $mark * 0.02 }
        [pscustomobject]@{ Text = "> $install"; Font = (New-Font $monoFamily ($mark * 0.082)); Colour = '#94A3B8'; Before = $mark * 0.09 }
    )
}

function Measure-Group([System.Drawing.Graphics]$graphics, [float]$mark) {
    $lines = Get-Lines $mark
    $width = 0
    $height = 0
    foreach ($line in $lines) {
        $size = $graphics.MeasureString($line.Text, $line.Font, [System.Drawing.PointF]::Empty, $typographic)
        $width = [Math]::Max($width, $size.Width)
        $height += $line.Before + $line.Font.GetHeight($graphics)
    }
    [pscustomobject]@{ Lines = $lines; TextWidth = $width; TextHeight = $height; Gap = $mark * 0.24; Width = $mark + $mark * 0.24 + $width; Height = [Math]::Max($mark, $height) }
}

function Add-Background([System.Drawing.Graphics]$graphics, [int]$width, [int]$height) {
    $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new((PointOf 0 0), (PointOf $width $height), (ColourOf '#1E1B4B'), (ColourOf '#0F172A'))
    $graphics.FillRectangle($background, 0, 0, $width, $height)
}

# A faint git graph: lanes of commits across the whole width, with a branch leaving or joining a neighbouring lane now and then.
function Add-GitGraph([System.Drawing.Graphics]$graphics, [int]$width, [int]$height) {
    $unit = $height / 100
    $random = [System.Random]::new(191)
    $colours = '#2DD4BF', '#A78BFA', '#64748B', '#2DD4BF', '#A78BFA'
    $laneCount = $colours.Count
    $lanes = 0..($laneCount - 1) | ForEach-Object { $height * (0.12 + 0.76 * $_ / ($laneCount - 1)) }
    $spacing = 16 * $unit
    for ($lane = 0; $lane -lt $laneCount; $lane++) {
        $y = $lanes[$lane]
        $colour = ColourWithAlpha $colours[$lane] 58
        $pen = [System.Drawing.Pen]::new($colour, 0.9 * $unit)
        $graphics.DrawLine($pen, 0, $y, $width, $y)
        $x = $random.NextDouble() * $spacing
        while ($x -lt $width) {
            $neighbour = $lane + ($random.Next(2) * 2 - 1)
            if ($random.NextDouble() -lt 0.18 -and $neighbour -ge 0 -and $neighbour -lt $laneCount) {
                $to = $lanes[$neighbour]
                $graphics.DrawBezier($pen, (PointOf $x $y), (PointOf ($x + $spacing * 0.6) $y), (PointOf ($x + $spacing * 0.4) $to), (PointOf ($x + $spacing) $to))
            }
            $radius = 1.7 * $unit
            $graphics.FillEllipse([System.Drawing.SolidBrush]::new((ColourOf '#14123A')), $x - $radius, $y - $radius, 2 * $radius, 2 * $radius)
            $graphics.DrawEllipse([System.Drawing.Pen]::new($colour, 0.8 * $unit), $x - $radius, $y - $radius, 2 * $radius, 2 * $radius)
            $x += $spacing * (0.7 + $random.NextDouble() * 0.9)
        }
    }
}

# Darkens the graph behind the group so the words read cleanly, fading out to the edges of an ellipse around it.
function Add-Shade([System.Drawing.Graphics]$graphics, [System.Drawing.RectangleF]$group) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddEllipse($group.X - $group.Width * 0.25, $group.Y - $group.Height * 0.55, $group.Width * 1.5, $group.Height * 2.1)
    $shade = [System.Drawing.Drawing2D.PathGradientBrush]::new($path)
    $shade.CenterColor = ColourWithAlpha '#111033' 235
    $shade.SurroundColors = [System.Drawing.Color[]]@((ColourWithAlpha '#111033' 0))
    $shade.FocusScales = [System.Drawing.PointF]::new(0.55, 0.45)
    $graphics.FillPath($shade, $path)
}

function Add-Group([System.Drawing.Graphics]$graphics, [System.Drawing.RectangleF]$box) {
    $nominal = 100
    $measured = Measure-Group $graphics $nominal
    $mark = $nominal * [Math]::Min($box.Width / $measured.Width, $box.Height / $measured.Height)
    $group = Measure-Group $graphics $mark
    $left = $box.X + ($box.Width - $group.Width) / 2
    $top = $box.Y + ($box.Height - $group.Height) / 2
    Add-Shade $graphics ([System.Drawing.RectangleF]::new($left, $top, $group.Width, $group.Height))
    Add-VantageMark $graphics $left ($top + ($group.Height - $mark) / 2) $mark
    $x = $left + $mark + $group.Gap
    $y = $top + ($group.Height - $group.TextHeight) / 2
    foreach ($line in $group.Lines) {
        $y += $line.Before
        $graphics.DrawString($line.Text, $line.Font, [System.Drawing.SolidBrush]::new((ColourOf $line.Colour)), $x, $y, $typographic)
        if ($line.Text.StartsWith('> ')) {
            $graphics.DrawString('>', $line.Font, [System.Drawing.SolidBrush]::new((ColourOf '#2DD4BF')), $x, $y, $typographic)
        }
        $y += $line.Font.GetHeight($graphics)
    }
}

function New-Header([int]$width, [int]$height, [System.Drawing.RectangleF]$box) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.TextRenderingHint = 'AntiAliasGridFit'
    Add-Background $graphics $width $height
    Add-GitGraph $graphics $width $height
    Add-Group $graphics $box
    $graphics.Dispose()
    $bitmap
}

function Save-Png([System.Drawing.Bitmap]$bitmap, [string]$path) {
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

function Assert-Size([string]$path, [int]$width, [int]$height) {
    $image = [System.Drawing.Image]::FromFile($path)
    $actual = "$($image.Width)x$($image.Height)"
    $image.Dispose()
    if ($actual -ne "${width}x${height}") {
        throw "$(Split-Path $path -Leaf) is $actual, expected ${width}x${height}."
    }
}

$layouts = @(
    [pscustomobject]@{ Name = 'buy-me-a-coffee-cover.png'; Width = 3200; Height = 800; Box = [System.Drawing.RectangleF]::new(880, 150, 1440, 400) }
    [pscustomobject]@{ Name = 'readme-banner.png'; Width = 1600; Height = 400; Box = [System.Drawing.RectangleF]::new(120, 70, 1360, 260) }
    [pscustomobject]@{ Name = 'social-preview.png'; Width = 1280; Height = 640; Box = [System.Drawing.RectangleF]::new(110, 150, 1060, 340) }
)

New-Item -ItemType Directory -Force $OutputFolder | Out-Null
$OutputFolder = (Resolve-Path $OutputFolder).Path
foreach ($layout in $layouts) {
    $path = Join-Path $OutputFolder $layout.Name
    Save-Png (New-Header $layout.Width $layout.Height $layout.Box) $path
    Assert-Size $path $layout.Width $layout.Height
}
Write-Host "Wrote $($layouts.Count) header images to $OutputFolder"

if ($QrCode) {
    New-Item -ItemType Directory -Force $QrOutputFolder | Out-Null
    $qrPath = Join-Path (Resolve-Path $QrOutputFolder).Path 'buy-me-a-coffee-qr.png'
    $source = [System.Drawing.Bitmap]::new((Resolve-Path $QrCode).Path)
    $qr = [System.Drawing.Bitmap]::new(160, 160)
    $graphics = [System.Drawing.Graphics]::FromImage($qr)
    $graphics.InterpolationMode = 'HighQualityBicubic'
    $graphics.PixelOffsetMode = 'HighQuality'
    $graphics.DrawImage($source, 0, 0, 160, 160)
    $graphics.Dispose()
    $source.Dispose()
    Save-Png $qr $qrPath
    Assert-Size $qrPath 160 160
    Write-Host "Wrote $qrPath"
}
