# Saca las piezas de la campaña a docs\campana\salida, con Edge sin ventana y
# ffmpeg. No abre nada en pantalla. Uso:  powershell -File docs\campana\generar.ps1
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$salida = Join-Path $aqui 'salida'
New-Item -ItemType Directory -Force $salida | Out-Null

$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$pagina = 'file:///' + ((Join-Path $aqui 'pieza.html') -replace '\\', '/')
$perfil = Join-Path $env:TEMP 'ecclesiacast-campana-edge'

function Sacar($pieza, $ancho, $alto, $nombre) {
    $png = Join-Path $salida $nombre
    $argumentos = @(
        '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
        '--allow-file-access-from-files', "--user-data-dir=`"$perfil`"",
        "--window-size=$ancho,$alto", '--virtual-time-budget=4000',
        "--screenshot=`"$png`"", "`"$pagina`?p=$pieza`"")
    $p = Start-Process -FilePath $edge -ArgumentList $argumentos -PassThru -WindowStyle Hidden
    if (-not $p.WaitForExit(40000)) { $p.Kill(); throw "Edge no terminó con $pieza" }
    if (-not (Test-Path $png)) { throw "No salió $nombre" }
    "ok $nombre"
}

1..7 | ForEach-Object { Sacar "c$_" 1080 1350 ("carrusel-{0:d2}.png" -f $_) }
1..3 | ForEach-Object { Sacar "h$_" 1080 1920 ("historia-{0:d2}.png" -f $_) }
Sacar 'v1' 1080 1920 'video-marco.png'
Sacar 'v2' 1080 1920 'video-cierre.png'

# El video vertical: la demostración de la página (50 s) un poco más rápida,
# dentro del marco, y tres segundos de cierre. Sin sonido: la música se pone
# desde la biblioteca de Instagram o TikTok.
$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ffmpeg) { 'ffmpeg no está: el video no se armó'; return }
$demo = Join-Path $aqui '..\..\sitio\video\ecclesiacast.mp4'
$reel = Join-Path $salida 'reel-lanzamiento.mp4'
$filtro = '[1:v]setpts=PTS/1.35,scale=1080:608,fps=30[v];' +
          '[0:v][v]overlay=0:700:shortest=1,format=yuv420p[a];' +
          '[2:v]fps=30,format=yuv420p[c];[a][c]concat=n=2:v=1[out]'
& $ffmpeg -y -loglevel error -loop 1 -framerate 30 -i (Join-Path $salida 'video-marco.png') -i $demo `
    -loop 1 -framerate 30 -t 3.5 -i (Join-Path $salida 'video-cierre.png') `
    -filter_complex $filtro -map '[out]' -c:v libx264 -preset medium -crf 20 -movflags +faststart $reel
"ok reel-lanzamiento.mp4"
