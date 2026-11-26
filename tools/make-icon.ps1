<#
.SYNOPSIS
    Builds the application icon (falah.ico) from the restaurant's logo: trims the transparent margin, keeps the
    logo square and centred, and writes PNG-compressed frames from 16 to 256 px (Windows Vista+ icon format).

.EXAMPLE
    .\tools\make-icon.ps1
#>
param(
    [string] $Logo = (Join-Path $PSScriptRoot 'brand\falah-logo.png'),
    [string] $Out  = (Join-Path $PSScriptRoot '..\src\Falah.Client.Shared\Brand\falah.ico')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$src = [Drawing.Bitmap]::FromFile((Resolve-Path $Logo).Path)
try {
    # Bounds of the visible logo (alpha above a small threshold), sampled every 4 px - plenty for a 2500 px logo.
    $minX = $src.Width; $minY = $src.Height; $maxX = 0; $maxY = 0
    for ($y = 0; $y -lt $src.Height; $y += 4) {
        for ($x = 0; $x -lt $src.Width; $x += 4) {
            if ($src.GetPixel($x, $y).A -gt 16) {
                if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    $side = [Math]::Max($maxX - $minX, $maxY - $minY) + 8
    $cx = ($minX + $maxX) / 2; $cy = ($minY + $maxY) / 2
    $crop = New-Object Drawing.RectangleF(($cx - $side / 2), ($cy - $side / 2), $side, $side)

    $frames = @()
    foreach ($size in 16, 20, 24, 32, 40, 48, 64, 96, 128, 256) {
        $bmp = New-Object Drawing.Bitmap($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.Clear([Drawing.Color]::Transparent)
        $g.DrawImage($src, (New-Object Drawing.RectangleF(0, 0, $size, $size)), $crop, [Drawing.GraphicsUnit]::Pixel)
        $g.Dispose()
        $ms = New-Object IO.MemoryStream
        $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
        $frames += , @{ Size = $size; Bytes = $ms.ToArray() }
    }
} finally { $src.Dispose() }

# ICONDIR + ICONDIRENTRY[] + PNG data.
$out = [IO.Path]::GetFullPath($Out)
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
$fs = [IO.File]::Create($out)
$w = New-Object IO.BinaryWriter($fs)
try {
    $w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($f in $frames) {
        $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }
        $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
        $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]$f.Bytes.Length); $w.Write([UInt32]$offset)
        $offset += $f.Bytes.Length
    }
    foreach ($f in $frames) { $w.Write($f.Bytes) }
} finally { $w.Dispose() }
Write-Host "Icon: $out ($($frames.Count) sizes, $((Get-Item $out).Length) bytes)"
