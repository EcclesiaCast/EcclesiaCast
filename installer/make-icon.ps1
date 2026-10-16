# Generates the EcclesiaCast application icon from the logo (a screen with a
# flame on a navy rounded square, sitio/img/logo.svg, rendered to
# sitio/img/icono-512.png). The .ico is committed, so this only needs to run
# when the artwork changes:
#
#   powershell -ExecutionPolicy Bypass -File installer\make-icon.ps1
#
# Sizes 16..128 are written as 32-bit BMP entries and 256 as PNG, which is what
# Windows and Inno Setup expect.

param(
    [string]$IcoPath = (Join-Path $PSScriptRoot '..\src\EcclesiaCast.App\app.ico'),
    [string]$PngPath = (Join-Path $PSScriptRoot '..\docs\img\icon-256.png'),
    [string]$SourcePng = (Join-Path $PSScriptRoot '..\sitio\img\icono-512.png')
)

Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$s) {
    # The artwork is sitio/img/logo.svg, rendered once to a 512 px PNG (see
    # the comment above); every size is that picture scaled down.
    $src = [System.Drawing.Image]::FromFile($SourcePng)
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $s, $s)
    $g.Dispose(); $src.Dispose()
    return ,$bmp
}

function Get-DibBytes([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    # BITMAPINFOHEADER
    $bw.Write([int]40); $bw.Write([int]$w); $bw.Write([int]($h * 2))
    $bw.Write([int16]1); $bw.Write([int16]32); $bw.Write([int]0)
    $bw.Write([int]($w * $h * 4)); $bw.Write([int]0); $bw.Write([int]0)
    $bw.Write([int]0); $bw.Write([int]0)
    # XOR mask: BGRA, bottom-up
    $data = $bmp.LockBits(
        (New-Object System.Drawing.Rectangle(0, 0, $w, $h)),
        [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $stride = $data.Stride
    $buf = New-Object byte[] ($stride * $h)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $buf.Length)
    $bmp.UnlockBits($data)
    for ($y = $h - 1; $y -ge 0; $y--) { $bw.Write($buf, ($y * $stride), ($w * 4)) }
    # AND mask: 1bpp, rows padded to 4 bytes, all zeros (alpha does the work)
    $maskRow = [int][Math]::Floor((($w + 31) / 32)) * 4
    $bw.Write((New-Object byte[] ($maskRow * $h)))
    $bw.Flush()
    return ,$ms.ToArray()
}

function Get-PngBytes([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return ,$ms.ToArray()
}

$outIco = $IcoPath
$outPng = $PngPath
foreach ($p in @($outIco, $outPng)) {
    $dir = Split-Path -Parent $p
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    if ($s -ge 256) { $bytes = Get-PngBytes $bmp } else { $bytes = Get-DibBytes $bmp }
    $images += [pscustomobject]@{ Size = $s; Bytes = $bytes }
    if ($s -eq 256) { $bmp.Save($outPng, [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
}

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([int16]0); $bw.Write([int16]1); $bw.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = $img.Size; if ($dim -ge 256) { $dim = 0 }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim)
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([int16]1); $bw.Write([int16]32)
    $bw.Write([int]$img.Bytes.Length); $bw.Write([int]$offset)
    $offset += $img.Bytes.Length
}
foreach ($img in $images) { $bw.Write([byte[]]$img.Bytes, 0, $img.Bytes.Length) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($outIco, $ms.ToArray())
Write-Output ("ico: {0} bytes, {1} images" -f (Get-Item $outIco).Length, $images.Count)
