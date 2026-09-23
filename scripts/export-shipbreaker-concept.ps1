#requires -Version 7.0
param([ValidateSet('v1', 'v2')][string]$Version = 'v2')
# Mechanical scale previews only; the generated master is never modified.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path -Parent $PSScriptRoot
$artRoot = Join-Path $repoRoot 'assets/phobos-shipbreaker'
$stem = "PhobosShipbreakerInstalled-concept-$Version"
$sourcePath = Join-Path $artRoot "source/$stem.png"
$sourceHashes = @{
    v1 = 'BC0E97FFC967DC81F506A0CA066B7361788723C823FA202517F2B114439335D1'
    v2 = 'AEF03A9CBB28501B24BF40DD424DFA769398388308E859C8ECE5AF0AF0ED79EE'
}
$expectedHash = $sourceHashes[$Version]
if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Concept master changed. Review and version the scale previews before exporting.'
}
$previewRoot = Join-Path $artRoot 'previews'
New-Item -ItemType Directory -Force -Path $previewRoot | Out-Null
$master = [Drawing.Bitmap]::new($sourcePath)
try {
    if ($master.Width -ne $master.Height) { throw 'This concept preview expects a square master.' }
    $small = [Drawing.Bitmap]::new(64, 64, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [Drawing.Graphics]::FromImage($small)
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            if ($Version -eq 'v1') {
                # Preserve the original smooth concept's historical scale preview.
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            } else {
                # The owner requested harder pixel edges. Do not blend new colours.
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
            }
            $graphics.DrawImage($master, [Drawing.Rectangle]::new(0, 0, 64, 64),
                0, 0, $master.Width, $master.Height, [Drawing.GraphicsUnit]::Pixel)
        } finally { $graphics.Dispose() }
        $small.Save((Join-Path $previewRoot "$stem-64.png"), [Drawing.Imaging.ImageFormat]::Png)
        $zoom = [Drawing.Bitmap]::new(512, 512, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [Drawing.Graphics]::FromImage($zoom)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
                $graphics.DrawImage($small, [Drawing.Rectangle]::new(0, 0, 512, 512),
                    0, 0, 64, 64, [Drawing.GraphicsUnit]::Pixel)
            } finally { $graphics.Dispose() }
            $zoom.Save((Join-Path $previewRoot "$stem-64-zoom.png"), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $zoom.Dispose() }
    } finally { $small.Dispose() }
} finally { $master.Dispose() }
Write-Output "Exported $Version as a 64px scale preview and its 8x enlargement; neither is a finished game asset."
