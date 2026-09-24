#requires -Version 7.0
# Mechanical crop/scale/export of the retained Imagegen master; no redrawing.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root 'assets/phobos-reclaimer'
$runtime = Join-Path $root 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
$entry = [pscustomobject]@{ Source = 'PhobosScrapReclaimer-v1.png'; Sha256 = '4E861578E9FD24DC11599CF2E1B09FE699D860E3BDDBB599ED42096C93402328'; Crop = @(120, 120, 1040, 1040) }
$source = Join-Path $art ('source/' + $entry.Source)
if ((Get-FileHash -LiteralPath $source).Hash -ne $entry.Sha256) { throw 'Reclaimer master hash changed.' }
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
    $small = Sample $master $crop 64 64
    for ($y = 0; $y -lt 64; $y++) {
        for ($x = 0; $x -lt 64; $x++) {
            $p = $small.GetPixel($x, $y)
            $small.SetPixel($x, $y, $(if ($p.A -ge 128) { [Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B) } else { [Drawing.Color]::Transparent }))
        }
    }
    $small.Save((Join-Path $runtime 'PhobosScrapReclaimer.png'), [Drawing.Imaging.ImageFormat]::Png)
    $small.Save((Join-Path $art 'previews/reclaimer-v1-world.png'), [Drawing.Imaging.ImageFormat]::Png)
    $zoom = Sample $small ([Drawing.Rectangle]::new(0, 0, 64, 64)) 512 512
    $zoom.Save((Join-Path $art 'previews/reclaimer-v1-zoom.png'), [Drawing.Imaging.ImageFormat]::Png)
    $normal = [Drawing.Bitmap]::new(64, 64)
    $ng = [Drawing.Graphics]::FromImage($normal)
    try { $ng.Clear([Drawing.Color]::FromArgb(128, 128, 255)) } finally { $ng.Dispose() }
    $normal.Save((Join-Path $runtime 'PhobosScrapReclaimerNormal.png'), [Drawing.Imaging.ImageFormat]::Png)
    $portrait = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pg = [Drawing.Graphics]::FromImage($portrait)
    try { $pg.Clear([Drawing.Color]::Transparent); $pg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor; $pg.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half; $pg.DrawImage($small, [Drawing.Rectangle]::new(0, 0, 256, 256)) } finally { $pg.Dispose() }
    $portrait.Save((Join-Path $runtime 'PhobosScrapReclaimerPortrait.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    foreach ($image in @($master, $small, $zoom, $normal, $portrait)) { if ($null -ne $image) { $image.Dispose() } }
}
Write-Output 'Exported reclaimer colour, flat normal, portrait and pixel-scale previews.'
