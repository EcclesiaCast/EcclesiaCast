# Vuelve a dibujar los fondos de ejemplo (installer\fondos\*.jpg).
# Necesita el SDK de .NET 8 y ffmpeg en el PATH.
#   powershell -File installer\fondos\generar.ps1

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$raw = Join-Path ([IO.Path]::GetTempPath()) 'ecclesiacast-fondos'
if (Test-Path $raw) { Remove-Item $raw -Recurse -Force }

dotnet run --configuration Release --project (Join-Path $here 'generador') -- $raw
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron dibujar los fondos.' }

Get-ChildItem $raw -Filter *.rgb | ForEach-Object {
    $jpg = Join-Path $here ($_.BaseName + '.jpg')
    ffmpeg -loglevel error -y -f rawvideo -pix_fmt rgb24 -s 1920x1080 -i $_.FullName -q:v 3 $jpg
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg no pudo guardar $jpg" }
}

Remove-Item $raw -Recurse -Force
Get-ChildItem $here -Filter *.jpg | Select-Object Name, @{ n = 'KB'; e = { [int]($_.Length / 1KB) } }
