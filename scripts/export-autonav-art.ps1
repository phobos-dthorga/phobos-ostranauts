#requires -Version 7.0
# Mechanical exports of approved Imagegen artwork; no repainting or recolouring.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repoRoot 'assets/phobos-autonav/source'
$imageRoot = Join-Path $repoRoot 'mods/PhobosAutoNav/images/phobos/autonav'
New-Item -ItemType Directory -Path $imageRoot -Force | Out-Null

# Shared alpha bounds of the approved 1254 x 1254 originals, including bent pins.
# One common crop keeps intact/damaged components registered. Do not crop each
# state independently or damaged hardware will appear to move when repaired.
$crop = [Drawing.RectangleF]::new(367, 404, 521, 480)
$sources = @(
    @{ Name = 'PhobosAutoNavModule'; Hash = '3608E4815DDB7FD6637BEAF7357812802FBF6A415BA13110227DF4481ADE2478' },
    @{ Name = 'PhobosAutoNavModuleDmg'; Hash = 'A1800D144178D76F6998903991B0B42ED399F99CE9CE6FE131FEDF554074B6E9' }
)
foreach ($entry in $sources) {
    $sourcePath = Join-Path $sourceRoot ($entry.Name + '-approved.png')
    if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $entry.Hash) {
        throw "Approved source changed: $sourcePath. Review the shared crop before exporting."
    }
    $source = [Drawing.Bitmap]::new($sourcePath)
    try {
        foreach ($size in @(16, 256)) {
            $output = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [Drawing.Graphics]::FromImage($output)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                # A one-world-pixel margin at both sizes. Preserve aspect ratio.
                $scale = ($size * 14.0 / 16.0) / [Math]::Max($crop.Width, $crop.Height)
                $width = $crop.Width * $scale
                $height = $crop.Height * $scale
                $target = [Drawing.RectangleF]::new(($size - $width) / 2, ($size - $height) / 2, $width, $height)
                $graphics.DrawImage($source, $target, $crop, [Drawing.GraphicsUnit]::Pixel)
                $suffix = if ($size -eq 16) { '' } else { 'Portrait' }
                $output.Save((Join-Path $imageRoot ($entry.Name + $suffix + '.png')), [Drawing.Imaging.ImageFormat]::Png)
            } finally {
                $graphics.Dispose()
                $output.Dispose()
            }
        }
    } finally { $source.Dispose() }
}

# Neutral tangent-space normal data. This is shader data, not borrowed game art.
$normal = [Drawing.Bitmap]::new(16, 16, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($normal)
try {
    $graphics.Clear([Drawing.Color]::FromArgb(255, 128, 128, 255))
    $normal.Save((Join-Path $imageRoot 'PhobosAutoNavModuleNormal.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose()
    $normal.Dispose()
}
Write-Output 'Exported approved Auto Nav artwork: two 16px items, two 256px portraits and a neutral normal map.'
