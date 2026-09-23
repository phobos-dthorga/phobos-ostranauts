#requires -Version 7.0
# Mechanical pixel exports and original technical shader data. Masters stay intact.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path -Parent $PSScriptRoot
$artRoot = Join-Path $repoRoot 'assets/phobos-shipbreaker'
$imageRoot = Join-Path $repoRoot 'mods/PhobosShipbreaker/images/phobos/shipbreaker'
$entries = @(Get-Content -LiteralPath (Join-Path $artRoot 'sources.json') -Raw | ConvertFrom-Json)
$expectedNames = @('Installed', 'InstalledDmg', 'Loose', 'LooseDmg', 'Section', 'Residue')
if (($entries.State -join ',') -ne ($expectedNames -join ',')) { throw 'Unexpected Shipbreaker asset set.' }
# Preflight all masters before changing any output.
foreach ($entry in $entries) {
    $path = Join-Path $artRoot ('source/' + $entry.Source)
    if ((Get-FileHash -LiteralPath $path).Hash -ne $entry.Sha256) { throw "Art master changed: $($entry.Source). Review and version it before exporting." }
    if ($entry.Size -ne $(if ($entry.State -eq 'Residue') { 16 } else { 64 })) { throw 'Unexpected world dimensions.' }
}
New-Item -ItemType Directory -Force -Path $imageRoot, (Join-Path $artRoot 'previews') | Out-Null

function Resize-Pixels([Drawing.Bitmap]$Source, [int]$Size) {
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

function New-SurfaceNormals([Drawing.Bitmap]$Colour, [string]$State) {
    $size = $Colour.Width
    $heights = [double[,]]::new($size, $size)
    # Low-relief geometry, independent of paint brightness: dark paint is not a hole.
    # Broad regions only; damage paint does not invent deep physical cavities.
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            if ($Colour.GetPixel($x, $y).A -ne 0) { $heights[$x, $y] = 0.4 }
        }
    }
    $regions = @()
    if ($State -ne 'Residue') {
        # Inclusive x/y bounds at the exported 64-pixel registration.
        $regions = @(@(13, 29, 49, 54, 0.2), @(9, 22, 12, 54, 0.65), @(51, 22, 54, 54, 0.65))
        if ($State -ne 'Section') {
            $regions += @(@(9, 7, 29, 19, 0.9), @(40, 10, 51, 15, 0.15),
                @(8, 24, 55, 27, 0.8), @(30, 22, 34, 32, 1.1))
        } else {
            $regions += @(@(15, 26, 48, 27, 0.5), @(15, 43, 48, 44, 0.5), @(30, 23, 33, 50, 0.5))
        }
        if ($State.StartsWith('Loose')) {
            $regions += @(@(20, 22, 23, 53, 0.5), @(40, 22, 43, 53, 0.5))
        }
    }
    foreach ($r in $regions) {
        for ($y = $r[1]; $y -le $r[3]; $y++) {
            for ($x = $r[0]; $x -le $r[2]; $x++) {
                if ($Colour.GetPixel($x, $y).A -ne 0) { $heights[$x, $y] = $r[4] }
            }
        }
    }
    $normal = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            $nx = 0.0; $ny = 0.0
            if ($Colour.GetPixel($x, $y).A -ne 0) {
                $nx = ($heights[[Math]::Max(0, $x - 1), $y] - $heights[[Math]::Min($size - 1, $x + 1), $y]) / 2
                # Game NormalPNGtoDXTnm inverts PNG green. Author positive Y
                # downward here so the engine obtains upward tangent-space Y.
                $ny = ($heights[$x, [Math]::Max(0, $y - 1)] - $heights[$x, [Math]::Min($size - 1, $y + 1)]) / 2
            }
            $length = [Math]::Sqrt($nx * $nx + $ny * $ny + 1)
            $normal.SetPixel($x, $y, [Drawing.Color]::FromArgb(255,
                [int][Math]::Round(127.5 * ($nx / $length + 1)),
                [int][Math]::Round(127.5 * ($ny / $length + 1)),
                [int][Math]::Round(127.5 * (1 / $length + 1))))
        }
    }
    return $normal
}

$sheet = [Drawing.Bitmap]::new(768, 600, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$canvas = [Drawing.Graphics]::FromImage($sheet)
$font = [Drawing.Font]::new('Consolas', 11)
try {
    $canvas.Clear([Drawing.Color]::FromArgb(255, 52, 55, 59))
    $canvas.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $canvas.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
    $index = 0
    foreach ($entry in $entries) {
        $master = [Drawing.Bitmap]::new((Join-Path $artRoot ('source/' + $entry.Source)))
        $world = $null; $normal = $null; $portrait = $null
        try {
            if ($master.Width -ne $master.Height) { throw 'Master canvas must remain square.' }
            $world = Resize-Pixels $master $entry.Size
            $visible = 0
            for ($y = 0; $y -lt $entry.Size; $y++) {
                for ($x = 0; $x -lt $entry.Size; $x++) {
                    $c = $world.GetPixel($x, $y)
                    # Generated surfaces can be alpha 253. Make material fully opaque,
                    # with a binary outline and no blended fringe or RGB recolouring.
                    if ($c.A -ge 128) {
                        $world.SetPixel($x, $y, [Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)); $visible++
                    } else { $world.SetPixel($x, $y, [Drawing.Color]::FromArgb(0, 0, 0, 0)) }
                }
            }
            if ($visible -lt ($entry.Size * $entry.Size / 5) -or $visible -eq ($entry.Size * $entry.Size)) {
                throw "Unexpected alpha coverage for $($entry.State)."
            }
            $name = 'PhobosShipbreaker' + $entry.State
            $world.Save((Join-Path $imageRoot "$name.png"), [Drawing.Imaging.ImageFormat]::Png)
            $normal = New-SurfaceNormals $world $entry.State
            $normal.Save((Join-Path $imageRoot ($name + 'Normal.png')), [Drawing.Imaging.ImageFormat]::Png)
            $portrait = Resize-Pixels $world 256
            $portrait.Save((Join-Path $imageRoot ($name + 'Portrait.png')), [Drawing.Imaging.ImageFormat]::Png)
            # Every world sprite shown at the same 4x zoom, including the small residue.
            $left = ($index % 3) * 256; $top = [int][Math]::Floor($index / 3) * 300
            $width = $entry.Size * 4
            $canvas.DrawImage($world, [Drawing.Rectangle]::new($left + (256 - $width) / 2, $top + 25, $width, $width),
                0, 0, $entry.Size, $entry.Size, [Drawing.GraphicsUnit]::Pixel)
            $canvas.DrawString("$($entry.State) | $($entry.Size)px | 4x", $font, [Drawing.Brushes]::White, $left + 8, $top + 5)
            $index++
        } finally {
            foreach ($bitmap in @($master, $world, $normal, $portrait)) { if ($null -ne $bitmap) { $bitmap.Dispose() } }
        }
    }
    $sheet.Save((Join-Path $artRoot 'previews/production-contact-sheet.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $font.Dispose(); $canvas.Dispose(); $sheet.Dispose() }
Write-Output 'Exported Shipbreaker artwork: six world sprites, six matching normal maps and six pixel-preserving portraits.'
