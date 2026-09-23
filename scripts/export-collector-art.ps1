#requires -Version 7.0
# Mechanical crop/scale/export of the retained Imagegen master; no redrawing.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root 'assets/phobos-residue-collector'
$runtime = Join-Path $root 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
$entry = Get-Content -LiteralPath (Join-Path $art 'sources.json') -Raw | ConvertFrom-Json
$source = Join-Path $art ('source/' + $entry.Source)
if ((Get-FileHash -LiteralPath $source).Hash -ne $entry.Sha256) { throw 'Collector master hash changed.' }
function Sample([Drawing.Bitmap]$Bitmap, [Drawing.Rectangle]$Crop, [int]$Width, [int]$Height) {
    $result = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($result)
    try {
        $g.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.DrawImage($Bitmap, [Drawing.Rectangle]::new(0, 0, $Width, $Height), $Crop, [Drawing.GraphicsUnit]::Pixel)
    } finally { $g.Dispose() }
    return $result
}
$master = [Drawing.Bitmap]::new($source)
$small = $null; $zoom = $null; $normal = $null; $portrait = $null
try {
    $crop = [Drawing.Rectangle]::new($entry.Crop[0], $entry.Crop[1], $entry.Crop[2], $entry.Crop[3])
    if ($crop.X -lt 0 -or $crop.Y -lt 0 -or $crop.Right -gt $master.Width -or $crop.Bottom -gt $master.Height) { throw 'Invalid crop.' }
    $small = Sample $master $crop 32 16
    for ($y = 0; $y -lt 16; $y++) {
        for ($x = 0; $x -lt 32; $x++) {
            $p = $small.GetPixel($x, $y)
            $small.SetPixel($x, $y, $(if ($p.A -ge 128) { [Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B) } else { [Drawing.Color]::Transparent }))
        }
    }
    $small.Save((Join-Path $runtime 'PhobosResidueCollector.png'), [Drawing.Imaging.ImageFormat]::Png)
    $small.Save((Join-Path $art 'previews/collector-v1-world.png'), [Drawing.Imaging.ImageFormat]::Png)
    $zoom = Sample $small ([Drawing.Rectangle]::new(0, 0, 32, 16)) 256 128
    $zoom.Save((Join-Path $art 'previews/collector-v1-zoom.png'), [Drawing.Imaging.ImageFormat]::Png)
    $normal = [Drawing.Bitmap]::new(32, 16)
    $ng = [Drawing.Graphics]::FromImage($normal)
    try { $ng.Clear([Drawing.Color]::FromArgb(128, 128, 255)) } finally { $ng.Dispose() }
    $normal.Save((Join-Path $runtime 'PhobosResidueCollectorNormal.png'), [Drawing.Imaging.ImageFormat]::Png)
    $portrait = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pg = [Drawing.Graphics]::FromImage($portrait)
    try { $pg.Clear([Drawing.Color]::Transparent); $pg.DrawImageUnscaled($zoom, 0, 64) } finally { $pg.Dispose() }
    $portrait.Save((Join-Path $runtime 'PhobosResidueCollectorPortrait.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    foreach ($image in @($master, $small, $zoom, $normal, $portrait)) { if ($null -ne $image) { $image.Dispose() } }
}
Write-Output 'Exported collector colour, flat normal, portrait and pixel-scale previews.'
