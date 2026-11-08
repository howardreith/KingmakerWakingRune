<#
.SYNOPSIS
  Paints the original Refuge Token icon from code (authoring only; nothing here ships or runs in the game).
  Writes art\RefugeToken_master.png (512x512, not packaged) and assets\icons\RefugeToken.png (128x128, the native icon size).
#>
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

$size = 1024
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppPArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

function P([double] $x, [double] $y) { New-Object System.Drawing.PointF ([single]($x * $size)), ([single]($y * $size)) }
function C([int] $a, [int] $r, [int] $gr, [int] $b) { [System.Drawing.Color]::FromArgb($a, $r, $gr, $b) }

# Angular talisman, slight three-quarter view, with an asymmetrical notch on the upper right.
$outline = @((P .30 .13), (P .60 .09), (P .68 .19), (P .62 .24), (P .70 .29), (P .87 .47), (P .78 .78), (P .49 .92), (P .21 .79), (P .12 .44))
$inner = @((P .325 .185), (P .575 .155), (P .625 .21), (P .55 .255), (P .655 .335), (P .795 .485), (P .725 .735), (P .49 .85), (P .265 .745), (P .19 .46))
$depth = @($outline | ForEach-Object { New-Object System.Drawing.PointF ($_.X + 26), ($_.Y + 34) })

$shadow = New-Object System.Drawing.SolidBrush (C 120 0 0 0)
$g.FillPolygon($shadow, @($depth | ForEach-Object { New-Object System.Drawing.PointF ($_.X + 14), ($_.Y + 18) }))

# Thick edge seen in three-quarter view.
$edge = New-Object System.Drawing.Drawing2D.LinearGradientBrush((P 0 .1), (P 1 .9), (C 255 58 44 28), (C 255 18 15 13))
$g.FillPolygon($edge, $depth)

# Aged metal rim: warm highlight against the cool rune.
$rim = New-Object System.Drawing.Drawing2D.LinearGradientBrush((P .1 .1), (P .9 .9), (C 255 214 166 92), (C 255 74 52 30))
$g.FillPolygon($rim, $outline)

# Dark stone face.
$stone = New-Object System.Drawing.Drawing2D.LinearGradientBrush((P .2 .15), (P .8 .85), (C 255 72 74 80), (C 255 26 27 32))
$g.FillPolygon($stone, $inner)

# Stone grain, deterministic so the build of the art is repeatable.
$random = New-Object System.Random 4726
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddPolygon($inner)
$g.SetClip($path)
for ($i = 0; $i -lt 2600; $i++) {
    $x = $random.Next($size); $y = $random.Next($size); $s = 2 + $random.Next(7)
    $light = $random.Next(2) -eq 0
    $alpha = 14 + $random.Next(34)
    $b = New-Object System.Drawing.SolidBrush ($(if ($light) { C $alpha 170 172 180 } else { C $alpha 0 0 0 }))
    $g.FillEllipse($b, $x, $y, $s, $s)
    $b.Dispose()
}

# Incised blue-green glyph: an original seven-spoke mark, not a canonical rune.
$cx = .47; $cy = .50
$glowPen = New-Object System.Drawing.Pen (C 90 40 240 215), 54
$glyphPen = New-Object System.Drawing.Pen (C 255 70 235 205), 22
$cutPen = New-Object System.Drawing.Pen (C 255 6 30 34), 34
foreach ($pen in @($glowPen, $cutPen, $glyphPen)) {
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $points = New-Object System.Collections.ArrayList
    for ($k = 0; $k -lt 8; $k++) {
        $a = (-90 + $k * 3 * 360 / 7) * [Math]::PI / 180
        [void]$points.Add((P ($cx + .22 * [Math]::Cos($a)) ($cy + .24 * [Math]::Sin($a))))
    }
    $g.DrawLines($pen, [System.Drawing.PointF[]]$points.ToArray())
    $g.DrawLine($pen, (P $cx ($cy - .10)), (P $cx ($cy + .12)))
}

# One hairline break: it is snapped to activate.
$g.ResetClip()
$crack = @((P .635 .23), (P .57 .335), (P .615 .405), (P .535 .525), (P .59 .615), (P .515 .725), (P .545 .845))
$crackGlow = New-Object System.Drawing.Pen (C 70 120 255 235), 20
$crackPen = New-Object System.Drawing.Pen (C 255 8 8 10), 9
$g.DrawLines($crackGlow, $crack)
$g.DrawLines($crackPen, $crack)

# Warm highlight on the upper rim.
$hl = New-Object System.Drawing.Pen (C 210 255 226 160), 12
$hl.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$hl.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLines($hl, @((P .135 .435), (P .31 .145), (P .595 .105)))
$g.Dispose()

function Save-Scaled([int] $w, [string] $file) {
    New-Item -ItemType Directory -Force -Path (Split-Path $file) | Out-Null
    $out = New-Object System.Drawing.Bitmap $w, $w, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $s = [System.Drawing.Graphics]::FromImage($out)
    $s.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $s.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $s.DrawImage($bmp, 0, 0, $w, $w)
    $s.Dispose()
    $out.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    Write-Host "$file ($w x $w)"
}
Save-Scaled 512 (Join-Path $Root 'art\RefugeToken_master.png')
Save-Scaled 128 (Join-Path $Root 'assets\icons\RefugeToken.png')
$bmp.Dispose()
