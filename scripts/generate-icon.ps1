param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeIconMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

function New-RoundedRectanglePath([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-StarPoints([float]$CenterX, [float]$CenterY, [float]$OuterRadius, [float]$InnerRadius) {
    $points = New-Object 'System.Drawing.PointF[]' 10
    for ($i = 0; $i -lt 10; $i++) {
        $angle = (-90 + ($i * 36)) * [Math]::PI / 180
        $radius = if (($i % 2) -eq 0) { $OuterRadius } else { $InnerRadius }
        $points[$i] = [System.Drawing.PointF]::new(
            $CenterX + [Math]::Cos($angle) * $radius,
            $CenterY + [Math]::Sin($angle) * $radius
        )
    }
    return $points
}

function New-IconBitmap([int]$Size) {
    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $bounds = New-RoundedRectanglePath (10 * $scale) (10 * $scale) (236 * $scale) (236 * $scale) (52 * $scale)
    $orange = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 138, 61))
    $graphics.FillPath($orange, $bounds)

    $book = New-RoundedRectanglePath (50 * $scale) (66 * $scale) (156 * $scale) (122 * $scale) (18 * $scale)
    $green = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 42, 163, 154))
    $graphics.FillPath($green, $book)

    $spinePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 255, 255)), ([Math]::Max(2, 9 * $scale))
    $graphics.DrawLine($spinePen, (128 * $scale), (72 * $scale), (128 * $scale), (184 * $scale))

    $starBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 210, 74))
    $graphics.FillPolygon($starBrush, (New-StarPoints (180 * $scale) (72 * $scale) (35 * $scale) (15 * $scale)))

    $fontSize = [Math]::Max(8, 88 * $scale)
    $font = New-Object System.Drawing.Font "Segoe UI", $fontSize, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $graphics.DrawString("K", $font, $white, ([System.Drawing.RectangleF]::new(47 * $scale, 91 * $scale, 162 * $scale, 88 * $scale)), $format)

    $graphics.Dispose()
    $orange.Dispose()
    $green.Dispose()
    $spinePen.Dispose()
    $starBrush.Dispose()
    $font.Dispose()
    $format.Dispose()
    $white.Dispose()

    return $bitmap
}

$outputDir = Split-Path -Parent $OutputPath
if ($outputDir) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

$bitmap = New-IconBitmap 256
$handle = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($handle)
$stream = [System.IO.File]::Create($OutputPath)
try {
    $icon.Save($stream)
}
finally {
    $stream.Dispose()
    $icon.Dispose()
    [NativeIconMethods]::DestroyIcon($handle) | Out-Null
    $bitmap.Dispose()
}
