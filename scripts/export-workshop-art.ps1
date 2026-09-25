#requires -Version 7.0
<#
.SYNOPSIS
Exports Workshop previews from masters retained on the dedicated art branch.
.DESCRIPTION
Reads the pinned Git objects without checking out masters into the current tree.
Only mechanical nearest-neighbour resizing; no generated artwork is repainted.
#>
[CmdletBinding()]
param([switch]$VerifyOnly)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root 'assets/workshop'
$manifest = Get-Content -LiteralPath (Join-Path $art 'exports.json') -Raw | ConvertFrom-Json
$preview = Join-Path $art 'previews'

function Read-GitBytes([string]$Object) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.ArgumentList.Add('cat-file')
    $start.ArgumentList.Add('blob')
    $start.ArgumentList.Add($Object)
    $process = [Diagnostics.Process]::Start($start)
    $buffer = [IO.MemoryStream]::new()
    try {
        $errorRead = $process.StandardError.ReadToEndAsync()
        $process.StandardOutput.BaseStream.CopyTo($buffer)
        $process.WaitForExit()
        $gitError = $errorRead.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            throw "Cannot read master ${Object}. Fetch origin codex/workshop-art-masters first. $gitError"
        }
        return ,$buffer.ToArray()
    } finally { $buffer.Dispose(); $process.Dispose() }
}

function Get-Sha256([byte[]]$Bytes) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes))
}

function Resize-Pixels([Drawing.Image]$Master, [int]$Size) {
    if ($Master.Width -ne $Master.Height -or $Master.Width -lt 2 * $Size) {
        throw 'Workshop masters must be square and at least twice each export dimension.'
    }
    $result = [Drawing.Bitmap]::new($Size, $Size, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($result)
    try {
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.DrawImage($Master, [Drawing.Rectangle]::new(0, 0, $Size, $Size),
            [Drawing.Rectangle]::new(0, 0, $Master.Width, $Master.Height), [Drawing.GraphicsUnit]::Pixel)
    } catch { $result.Dispose(); throw }
    finally { $graphics.Dispose() }
    return $result
}

function Save-OrVerify([Drawing.Image]$Image, [string]$Name, [long]$MaxBytes, [string]$Directory = $preview) {
    $buffer = [IO.MemoryStream]::new()
    try {
        $Image.Save($buffer, [Drawing.Imaging.ImageFormat]::Png)
        $bytes = $buffer.ToArray()
        if ($bytes.Length -ge $MaxBytes) { throw "Export exceeds byte budget: $Name" }
        $path = Join-Path $Directory $Name
        if ($VerifyOnly) {
            if (-not (Test-Path -LiteralPath $path)) { throw "Missing preview: $Name" }
            if ((Get-Sha256 ([IO.File]::ReadAllBytes($path))) -ne (Get-Sha256 $bytes)) {
                throw "Preview differs from its pinned master/export rules: $Name"
            }
        } else {
            New-Item -ItemType Directory -Force -Path $Directory | Out-Null
            [IO.File]::WriteAllBytes($path, $bytes)
        }
        Write-Output ('{0}: {1} x {2}, {3:N0} bytes' -f $Name, $Image.Width, $Image.Height, $bytes.Length)
    } finally { $buffer.Dispose() }
}

# Read and validate all sources before writing any previews.
$sources = @{}
foreach ($entry in $manifest.assets) {
    if ($entry.id -notmatch '^Phobos[A-Za-z]+$') { throw 'Invalid Workshop asset ID.' }
    $bytes = Read-GitBytes "$($manifest.masterCommit):$($entry.masterPath)"
    if ((Get-Sha256 $bytes) -ne $entry.sha256) { throw "Master hash changed: $($entry.id)" }
    $sources[$entry.id] = $bytes
}
if (-not $VerifyOnly) { New-Item -ItemType Directory -Force -Path $preview | Out-Null }

# A compact 2 x 2 review sheet uses the exported 512px artwork, never full masters.
$tile = 512
$gap = 16
$sheetSize = 2 * $tile + 3 * $gap
$sheet = [Drawing.Bitmap]::new($sheetSize, $sheetSize, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
$sheetGraphics = [Drawing.Graphics]::FromImage($sheet)
try {
    $sheetGraphics.Clear([Drawing.Color]::FromArgb(20, 24, 26))
    $index = 0
    foreach ($entry in $manifest.assets) {
        $stream = [IO.MemoryStream]::new([byte[]]$sources[$entry.id], $false)
        $master = $null
        try {
            $master = [Drawing.Image]::FromStream($stream)
            if ($master.Width -ne $entry.width -or $master.Height -ne $entry.height) {
                throw "Master dimensions disagree with manifest: $($entry.id)"
            }
            foreach ($size in $manifest.exportSizes) {
                $resized = Resize-Pixels $master $size
                try {
                    Save-OrVerify $resized "$($entry.id)-$size.png" $manifest.maxPreviewBytes
                    if ($size -eq $tile) {
                        # Native mod-menu and Workshop uploader both look here.
                        Save-OrVerify $resized 'preview.png' $manifest.maxPreviewBytes (Join-Path $root "mods/$($entry.id)")
                        $x = $gap + ($index % 2) * ($tile + $gap)
                        $y = $gap + [Math]::Floor($index / 2) * ($tile + $gap)
                        $sheetGraphics.DrawImageUnscaled($resized, [int]$x, [int]$y)
                    }
                } finally { $resized.Dispose() }
            }
        } finally { if ($null -ne $master) { $master.Dispose() }; $stream.Dispose() }
        $index++
    }
    Save-OrVerify $sheet 'collection.png' 5000000
} finally { $sheetGraphics.Dispose(); $sheet.Dispose() }
Write-Output $(if ($VerifyOnly) { 'Workshop previews verified.' } else { 'Workshop previews exported.' })
