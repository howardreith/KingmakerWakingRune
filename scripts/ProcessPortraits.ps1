<#
.SYNOPSIS
  Produces the shipped portrait sizes from the approved source artwork, cropping each size independently
  and never changing an aspect ratio.

.DESCRIPTION
  Version 1 of the WR-02 package has the SheilaHeidmarch and AmbrusValsin folders exchanged (inspected:
  the folder named AmbrusValsin holds the dark-haired woman with the silver headpiece, and the folder named
  SheilaHeidmarch holds the moustached man with crossed arms). The map below binds each identity to the
  folder that actually contains that person. If a corrected package is used, pass -SwappedSources:$false.

  Output: assets\portraits\<Id>\{Fulllength,Medium,Small}.png (692x1024, 330x432, 185x242).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PackagePortraits,
    [bool] $SwappedSources = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$Sizes = @(
    @{ Name = 'Fulllength'; W = 692; H = 1024; SourceHeight = 1.00; HeadY = 0.21 },
    @{ Name = 'Medium'; W = 330; H = 432; SourceHeight = 0.74; HeadY = 0.27 },
    @{ Name = 'Small'; W = 185; H = 242; SourceHeight = 0.46; HeadY = 0.40 }
)
# Head centre of each source image, as fractions of its width and height (read from the images).
$Portraits = @(
    @{ Id = 'SheilaHeidmarch'; Folder = $(if ($SwappedSources) { 'AmbrusValsin' } else { 'SheilaHeidmarch' }); X = 0.49; Y = 0.20 },
    @{ Id = 'AmbrusValsin'; Folder = $(if ($SwappedSources) { 'SheilaHeidmarch' } else { 'AmbrusValsin' }); X = 0.50; Y = 0.21 },
    @{ Id = 'DecemvirateMember'; Folder = 'DecemvirateMember'; X = 0.52; Y = 0.23 },
    @{ Id = 'Quartermaster'; Folder = 'Quartermaster'; X = 0.47; Y = 0.17 },
    @{ Id = 'SocietyCoordinator'; Folder = 'SocietyCoordinator'; X = 0.50; Y = 0.18 }
)

foreach ($p in $Portraits) {
    $src = [System.Drawing.Image]::FromFile((Join-Path (Join-Path $PackagePortraits $p.Folder) 'Original.png'))
    $outDir = Join-Path $Root "assets\portraits\$($p.Id)"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    foreach ($s in $Sizes) {
        $cropH = [Math]::Min([double]$src.Height * $s.SourceHeight, [double]$src.Width * $s.H / $s.W)
        $cropW = $cropH * $s.W / $s.H
        $cropX = [Math]::Max(0.0, [Math]::Min($src.Width - $cropW, $src.Width * $p.X - $cropW / 2))
        $cropY = [Math]::Max(0.0, [Math]::Min($src.Height - $cropH, $src.Height * $p.Y - $cropH * $s.HeadY))
        $bmp = New-Object System.Drawing.Bitmap $s.W, $s.H
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.DrawImage($src, (New-Object System.Drawing.RectangleF 0, 0, $s.W, $s.H), (New-Object System.Drawing.RectangleF $cropX, $cropY, $cropW, $cropH), [System.Drawing.GraphicsUnit]::Pixel)
        $g.Dispose()
        $bmp.Save((Join-Path $outDir "$($s.Name).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host ("{0}/{1}: crop {2:0}x{3:0} at ({4:0},{5:0}) of {6}" -f $p.Id, $s.Name, $cropW, $cropH, $cropX, $cropY, $p.Folder)
    }
    $src.Dispose()
}
