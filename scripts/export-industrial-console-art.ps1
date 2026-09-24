#requires -Version 7.0
# Deterministic crop/nearest-neighbour export of retained Imagegen masters.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root 'assets/phobos-industrial-console'
$runtime = Join-Path $root 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
function Sample([Drawing.Bitmap]$Source, [Drawing.Rectangle]$Crop, [int]$Width, [int]$Height) {
    $result = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($result)
    try {
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.DrawImage($Source, [Drawing.Rectangle]::new(0,0,$Width,$Height), $Crop, [Drawing.GraphicsUnit]::Pixel)
    } finally { $graphics.Dispose() }
    return $result
}
foreach ($entry in (Get-Content -LiteralPath (Join-Path $art 'manifest.json') -Raw | ConvertFrom-Json)) {
    $source = Join-Path $art ('source/' + $entry.file)
    if ((Get-FileHash -LiteralPath $source).Hash -ne $entry.sha256) { throw "Console master changed: $($entry.file)" }
    if ($entry.state -eq 'Panel') { Copy-Item -LiteralPath $source -Destination (Join-Path $runtime $entry.file) -Force; continue }
    $master = [Drawing.Bitmap]::new($source)
    $small = $normal = $portrait = $zoom = $null
    try {
        $crop = [Drawing.Rectangle]::new($entry.crop[0],$entry.crop[1],$entry.crop[2],$entry.crop[3])
        if ($crop.X -lt 0 -or $crop.Y -lt 0 -or $crop.Right -gt $master.Width -or $crop.Bottom -gt $master.Height) { throw 'Invalid console crop' }
        $small = Sample $master $crop 48 48
        for ($y=0; $y -lt 48; $y++) { for ($x=0; $x -lt 48; $x++) {
            $pixel = $small.GetPixel($x,$y)
            $color = if ($pixel.A -ge 128) { [Drawing.Color]::FromArgb(255,$pixel.R,$pixel.G,$pixel.B) } else { [Drawing.Color]::Transparent }
            $small.SetPixel($x,$y,$color)
        } }
        $stem = [IO.Path]::GetFileNameWithoutExtension($entry.file)
        $small.Save((Join-Path $runtime $entry.file), [Drawing.Imaging.ImageFormat]::Png)
        $normal = [Drawing.Bitmap]::new(48,48)
        $graphics = [Drawing.Graphics]::FromImage($normal)
        try { $graphics.Clear([Drawing.Color]::FromArgb(128,128,255)) } finally { $graphics.Dispose() }
        $normal.Save((Join-Path $runtime ($stem+'Normal.png')), [Drawing.Imaging.ImageFormat]::Png)
        $portrait = Sample $small ([Drawing.Rectangle]::new(0,0,48,48)) 256 256
        $portrait.Save((Join-Path $runtime ($stem+'Portrait.png')), [Drawing.Imaging.ImageFormat]::Png)
        $zoom = Sample $small ([Drawing.Rectangle]::new(0,0,48,48)) 480 480
        $zoom.Save((Join-Path $art ('previews/'+$entry.file)), [Drawing.Imaging.ImageFormat]::Png)
    } finally { foreach ($image in @($master,$small,$normal,$portrait,$zoom)) { if ($null -ne $image) { $image.Dispose() } } }
}
Write-Output 'Exported four 48 x 48 console states, flat normals, portraits and shared faceplate.'
