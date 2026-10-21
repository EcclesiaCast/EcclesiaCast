# Vuelve a dibujar los fondos en video (installer\fondos\*.mp4).
# Necesita el SDK de .NET 8 y ffmpeg en el PATH. Tarda: entre 1 y 6 minutos
# por video.
#   powershell -File installer\fondos\generar.ps1              (todos)
#   powershell -File installer\fondos\generar.ps1 "Aurora"     (sólo uno)

param([string]$Nombre)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

$arguments = @('run', '--configuration', 'Release', '--project', (Join-Path $here 'generador'), '--', $here, '20')
if ($Nombre) { $arguments += $Nombre }
dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron dibujar los fondos.' }

Get-ChildItem $here -Filter *.mp4 | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
