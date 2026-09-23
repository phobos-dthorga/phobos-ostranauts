#requires -Version 7.0
# Mechanical scale previews only; no runtime assets or game files are changed.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path -Parent $PSScriptRoot
$artRoot = Join-Path $repoRoot 'assets/phobos-material-transport'
$previewRoot = Join-Path $artRoot 'previews'
$entries = @(Get-Content -LiteralPath (Join-Path $artRoot 'sources.json') -Raw | ConvertFrom-Json)
if (($entries.State -join ',') -ne 'Sending,Receiving') { throw 'Expected sending and receiving concepts.' }
foreach ($entry in $entries) {
    if ((Get-FileHash -LiteralPath (Join-Path $artRoot ('source/' + $entry.Source))).Hash -ne $entry.Sha256) {
        throw "Concept source changed: $($entry.Source)"
    }
}
New-Item -ItemType Directory -Force -Path $previewRoot | Out-Null
function Scale-Pixels([Drawing.Bitmap]$Source, [int]$Size) {
    $result = [Drawing.Bitmap]::new($Size, $Size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($result)
    try {
        $g.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.DrawImage($Source, [Drawing.Rectangle]::new(0, 0, $Size, $Size),
            0, 0, $Source.Width, $Source.Height, [Drawing.GraphicsUnit]::Pixel)
    } finally { $g.Dispose() }
    return $result
}
$sheet = [Drawing.Bitmap]::new(1056, 560, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$canvas = [Drawing.Graphics]::FromImage($sheet)
$font = [Drawing.Font]::new('Consolas', 11)
try {
    $canvas.Clear([Drawing.Color]::FromArgb(255, 52, 55, 59))
    $canvas.DrawString('Shipbreaker | 64px | 4 x 4 tiles', $font, [Drawing.Brushes]::White, 8, 8)
    $machine = [Drawing.Bitmap]::new((Join-Path $repoRoot 'mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosShipbreakerInstalled.png'))
    $zoom = $null
    try {
        $zoom = Scale-Pixels $machine 512
        $canvas.DrawImageUnscaled($zoom, 8, 32)
    } finally { $machine.Dispose(); if ($null -ne $zoom) { $zoom.Dispose() } }
    $index = 0
    foreach ($entry in $entries) {
        $master = [Drawing.Bitmap]::new((Join-Path $artRoot ('source/' + $entry.Source)))
        $small = $null; $zoom = $null
        try {
            if ($master.Width -ne $master.Height) { throw 'Expected square concept canvas.' }
            $small = Scale-Pixels $master 32
            # Retain generated alpha for concept review. Production opacity is a later step.
            $small.Save((Join-Path $previewRoot ($entry.State + '-v1-32.png')), [Drawing.Imaging.ImageFormat]::Png)
            $zoom = Scale-Pixels $small 256
            $zoom.Save((Join-Path $previewRoot ($entry.State + '-v1-32-zoom.png')), [Drawing.Imaging.ImageFormat]::Png)
            $left = 528 + $index * 264
            $canvas.DrawString("$($entry.State) | 32px | 2 x 2", $font, [Drawing.Brushes]::White, $left, 8)
            $canvas.DrawImageUnscaled($zoom, $left, 32)
            $index++
        } finally {
            foreach ($bitmap in @($master, $small, $zoom)) { if ($null -ne $bitmap) { $bitmap.Dispose() } }
        }
    }
    $sheet.Save((Join-Path $previewRoot 'ports-v1-scale-comparison.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $font.Dispose(); $canvas.Dispose(); $sheet.Dispose() }
Write-Output 'Exported two 32px concept previews, 8x enlargements and a same-scale Shipbreaker comparison. No runtime assets changed.'
