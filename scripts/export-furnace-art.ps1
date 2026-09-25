#requires -Version 7.0
# Mechanical native-size derivatives; all creative changes belong in retained masters.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root 'assets/phobos-furnace'
$runtime = Join-Path $root 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
$preview = Join-Path $art 'previews'
New-Item -ItemType Directory -Force -Path $preview | Out-Null
$manifest = Get-Content -LiteralPath (Join-Path $art 'exports.json') -Raw | ConvertFrom-Json
function Resize-Pixels([Drawing.Bitmap]$Bitmap, [Drawing.Rectangle]$Crop, [int]$Width, [int]$Height) {
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
foreach ($entry in $manifest.assets) {
    $source = Join-Path $art ('source/' + $entry.source)
    if ((Get-FileHash -LiteralPath $source).Hash -ne $entry.sha256) { throw "Furnace master hash changed: $($entry.source)" }
    $master = [Drawing.Bitmap]::new($source)
    $small = $null; $normal = $null; $portrait = $null; $zoom = $null
    try {
        $crop = [Drawing.Rectangle]::new($entry.crop[0], $entry.crop[1], $entry.crop[2], $entry.crop[3])
        if ($crop.X -lt 0 -or $crop.Y -lt 0 -or $crop.Right -gt $master.Width -or $crop.Bottom -gt $master.Height) { throw 'Invalid furnace crop.' }
        $w = [int]$entry.size[0]; $h = [int]$entry.size[1]
        $masterScale = if ([Math]::Min($w, $h) -le 32) { 4 } else { 2 }
        if ($master.Width -lt $w * $masterScale -or $master.Height -lt $h * $masterScale) { throw 'Master is below resolution policy.' }
        $small = Resize-Pixels $master $crop $w $h
        for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
            $p = $small.GetPixel($x, $y)
            $small.SetPixel($x, $y, $(if ($p.A -ge 128) { [Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B) } else { [Drawing.Color]::Transparent }))
        } }
        $small.Save((Join-Path $runtime ($entry.id + '.png')), [Drawing.Imaging.ImageFormat]::Png)
        $small.Save((Join-Path $preview ($entry.id + '-world.png')), [Drawing.Imaging.ImageFormat]::Png)
        $zoom = Resize-Pixels $small ([Drawing.Rectangle]::new(0, 0, $w, $h)) ($w * 4) ($h * 4)
        $zoom.Save((Join-Path $preview ($entry.id + '-zoom.png')), [Drawing.Imaging.ImageFormat]::Png)
        $normal = [Drawing.Bitmap]::new($w, $h)
        $ng = [Drawing.Graphics]::FromImage($normal)
        try { $ng.Clear([Drawing.Color]::FromArgb(128, 128, 255)) } finally { $ng.Dispose() }
        $normal.Save((Join-Path $runtime ($entry.id + 'Normal.png')), [Drawing.Imaging.ImageFormat]::Png)
        $portrait = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $pg = [Drawing.Graphics]::FromImage($portrait)
        try {
            $pg.Clear([Drawing.Color]::Transparent); $pg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $pg.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
            $scale = [Math]::Floor(256 / [Math]::Max($w, $h)); $pw = $w * $scale; $ph = $h * $scale
            $pg.DrawImage($small, [Drawing.Rectangle]::new((256-$pw)/2, (256-$ph)/2, $pw, $ph))
        } finally { $pg.Dispose() }
        $portrait.Save((Join-Path $runtime ($entry.id + 'Portrait.png')), [Drawing.Imaging.ImageFormat]::Png)
    } finally { foreach ($image in @($master, $small, $normal, $portrait, $zoom)) { if ($null -ne $image) { $image.Dispose() } } }
}
Write-Output "Exported $($manifest.assets.Count) original furnace assets, flat normals, portraits and crisp previews."
