<#
.SYNOPSIS
The mark (two worktree branches meeting in the bar of a T, a tower, over a stem to one node, on a rounded tile), drawn once for every script that needs it.
.DESCRIPTION
Dot-source it: New-Icon.ps1 draws the icon with it, New-Header.ps1 draws the mark on the header images, and New-IconPixels.ps1
draws it without its tile (-NoTile) for the console's black background, where the dark tile disappeared. The mark is laid out on a
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

function Add-Node([System.Drawing.Graphics]$graphics, [float]$x, [float]$y, [float]$radius, [string]$ring, [string]$centre = '#0F172A') {
    $graphics.FillEllipse([System.Drawing.SolidBrush]::new((ColourOf $centre)), $x - $radius, $y - $radius, 2 * $radius, 2 * $radius)
    $inner = $radius * 0.78
    $graphics.DrawEllipse((New-Pen $ring ($radius * 0.55)), $x - $inner, $y - $inner, 2 * $inner, 2 * $inner)
}

function Add-LogoMark([System.Drawing.Graphics]$graphics, [float]$x, [float]$y, [float]$size, [switch]$NoTile, [string]$NodeCentre = '#0F172A', [string]$Outline) {
    $layout = 512
    $state = $graphics.Save()
    $graphics.TranslateTransform($x, $y)
    $graphics.ScaleTransform($size / $layout, $size / $layout)
    if (-not $NoTile) {
        $tile = New-RoundedRectangle $layout 96
        $graphics.SetClip($tile)
        $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new((PointOf 0 0), (PointOf 0 $layout), (ColourOf '#1E1B4B'), (ColourOf '#0F172A'))
        $graphics.FillRectangle($background, 0, 0, $layout, $layout)
        if ($Outline) {
            # An edge that sets the dark tile off a black background, drawn on a path inset by half its width so the whole
            # stroke lies inside the tile and the four sides come out alike (a stroke on the tile's own edge was clipped unevenly).
            $width = 28
            $inset = New-RoundedRectangle ($layout - $width) (96 - $width / 2)
            $shift = [System.Drawing.Drawing2D.Matrix]::new()
            $shift.Translate($width / 2, $width / 2)
            $inset.Transform($shift)
            $graphics.DrawPath([System.Drawing.Pen]::new((ColourOf $Outline), $width), $inset)
        }
    }

    # A T: the branches run level from the top corners and meet at the centre, over a stem down to one node. The stem is
    # drawn first so the branches' round ends cover its top and the join is clean.
    $graphics.DrawLine((New-Pen '#FFFFFF' 40), 256, 120, 256, 392)
    $graphics.DrawLine((New-Pen '#2DD4BF' 40), 112, 120, 256, 120)
    $graphics.DrawLine((New-Pen '#A78BFA' 40), 400, 120, 256, 120)
    Add-Node $graphics 112 120 46 '#2DD4BF' $NodeCentre
    Add-Node $graphics 400 120 46 '#A78BFA' $NodeCentre
    Add-Node $graphics 256 392 46 '#FFFFFF' $NodeCentre
    $graphics.Restore($state)
}
