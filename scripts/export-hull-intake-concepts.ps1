#requires -Version 7.0
# Mechanical concept scale checks; original Imagegen masters stay unchanged.
param([switch]$Runtime)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path -Parent $PSScriptRoot
$artRoot = Join-Path $repoRoot 'assets/phobos-hull-intake'
$previewRoot = Join-Path $artRoot 'previews'
$runtimeRoot = Join-Path $repoRoot 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
$entries = @(Get-Content -LiteralPath (Join-Path $artRoot 'sources.json') -Raw | ConvertFrom-Json)
foreach ($entry in $entries) {
    if ((Get-FileHash -LiteralPath (Join-Path $artRoot ('source/' + $entry.Source))).Hash -ne $entry.Sha256) {
        throw "Concept source changed: $($entry.Source)"
    }
}
New-Item -ItemType Directory -Force -Path $previewRoot | Out-Null
function Sample-Pixels([Drawing.Bitmap]$Source, [Drawing.Rectangle]$Crop, [int]$Width, [int]$Height) {
    $result = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($result)
    try {
        $g.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.DrawImage($Source, [Drawing.Rectangle]::new(0, 0, $Width, $Height), $Crop, [Drawing.GraphicsUnit]::Pixel)
    } finally { $g.Dispose() }
    return $result
}
$sheet = [Drawing.Bitmap]::new(760, 1192, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$canvas = [Drawing.Graphics]::FromImage($sheet)
$font = [Drawing.Font]::new('Consolas', 11)
try {
    $canvas.Clear([Drawing.Color]::FromArgb(255, 52, 55, 59))
    $canvas.DrawString('HULL INTAKE | approved art | gameplay test pending', $font, [Drawing.Brushes]::White, 16, 10)
    foreach ($entry in $entries) {
        $master = [Drawing.Bitmap]::new((Join-Path $artRoot ('source/' + $entry.Source)))
        $small = $null; $zoom = $null
        try {
            $crop = [Drawing.Rectangle]::new($entry.Crop[0], $entry.Crop[1], $entry.Crop[2], $entry.Crop[3])
            # Trim the chute's square-canvas margins, including faint alpha specks.
            # Fail rather than clipping material; retain interior generated alpha.
            if ($crop.X -lt 0 -or $crop.Y -lt 0 -or $crop.Right -gt $master.Width -or $crop.Bottom -gt $master.Height) { throw 'Crop outside source.' }
            for ($y = 0; $y -lt $master.Height; $y++) {
                for ($x = 0; $x -lt $master.Width; $x++) {
                    if (-not $crop.Contains($x, $y) -and $master.GetPixel($x, $y).A -ge 128) { throw 'Crop would clip source art.' }
                }
            }
            $small = Sample-Pixels $master $crop $entry.Size[0] $entry.Size[1]
            if ($Runtime) {
                # Same binary-alpha mechanical export as the accepted processor.
                for ($y = 0; $y -lt $small.Height; $y++) {
                    for ($x = 0; $x -lt $small.Width; $x++) {
                        $pixel = $small.GetPixel($x, $y)
                        $small.SetPixel($x, $y, $(if ($pixel.A -ge 128) { [Drawing.Color]::FromArgb(255, $pixel.R, $pixel.G, $pixel.B) } else { [Drawing.Color]::Transparent }))
                    }
                }
                $name = 'Phobos' + $entry.State
                $small.Save((Join-Path $runtimeRoot "$name.png"), [Drawing.Imaging.ImageFormat]::Png)
                # Flat technical normals retain the approved paint without inventing relief.
                $normal = [Drawing.Bitmap]::new($small.Width, $small.Height)
                $ng = [Drawing.Graphics]::FromImage($normal)
                $portrait = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $pg = [Drawing.Graphics]::FromImage($portrait)
                $large = Sample-Pixels $small ([Drawing.Rectangle]::new(0, 0, $small.Width, $small.Height)) 256 ($small.Height * 4)
                try {
                    $ng.Clear([Drawing.Color]::FromArgb(128, 128, 255))
                    $normal.Save((Join-Path $runtimeRoot ($name + 'Normal.png')), [Drawing.Imaging.ImageFormat]::Png)
                    $pg.Clear([Drawing.Color]::Transparent)
                    $pg.DrawImageUnscaled($large, 0, [int]((256 - $large.Height) / 2))
                    $portrait.Save((Join-Path $runtimeRoot ($name + 'Portrait.png')), [Drawing.Imaging.ImageFormat]::Png)
                } finally { $ng.Dispose(); $normal.Dispose(); $pg.Dispose(); $portrait.Dispose(); $large.Dispose() }
            }
            $small.Save((Join-Path $previewRoot ($entry.State + '-v1-world.png')), [Drawing.Imaging.ImageFormat]::Png)
            $zoom = Sample-Pixels $small ([Drawing.Rectangle]::new(0, 0, $small.Width, $small.Height)) ($small.Width * 8) ($small.Height * 8)
            $zoom.Save((Join-Path $previewRoot ($entry.State + '-v1-zoom.png')), [Drawing.Imaging.ImageFormat]::Png)
            $top = if ($entry.State -eq 'ExteriorGrabber') { 70 } else { 492 }
            $canvas.DrawImageUnscaled($zoom, 16, $top)
            $caption = if ($entry.State -eq 'ExteriorGrabber') { "SPACE`nGrabber`n4 wide x 3 deep`n64 x 48 px" } else { "HULL`nChute`n4 wide x 1 deep`n64 x 16 px" }
            $canvas.DrawString($caption, $font, [Drawing.Brushes]::White, 540, $top)
        } finally {
            foreach ($bitmap in @($master, $small, $zoom)) { if ($null -ne $bitmap) { $bitmap.Dispose() } }
        }
    }
    $machine = [Drawing.Bitmap]::new((Join-Path $repoRoot 'mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosShipbreakerInstalled.png'))
    $machineZoom = $null
    try {
        # Orient the existing loading mouth toward the proposed hull chute.
        $machine.RotateFlip([Drawing.RotateFlipType]::Rotate180FlipNone)
        $machineZoom = Sample-Pixels $machine ([Drawing.Rectangle]::new(0, 0, $machine.Width, $machine.Height)) 512 512
        $canvas.DrawImageUnscaled($machineZoom, 16, 658)
        $canvas.DrawString("INTERIOR`nExisting fixture`n4 x 4 tiles`nMouth toward hull`n64 x 64 px", $font, [Drawing.Brushes]::White, 540, 658)
    } finally { $machine.Dispose(); if ($null -ne $machineZoom) { $machineZoom.Dispose() } }
    $canvas.DrawString('Same scale, spaced apart for inspection; install components without gaps.', $font, [Drawing.Brushes]::White, 16, 42)
    $sheet.Save((Join-Path $previewRoot 'hull-intake-scale-comparison.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $font.Dispose(); $canvas.Dispose(); $sheet.Dispose() }
Write-Output "Exported chute/grabber previews. Runtime export: $Runtime (colour, flat normals and square padded portraits)."
