# Actualiza catalogo.json con los .ecbiblias de esta carpeta.
#
# - Lo que escribiste a mano en catalogo.json (nombres, licencias, orden) se
#   respeta: sólo se completan el tamaño (size) y la huella (sha256) de cada
#   archivo, y name/abbreviation/language si están vacíos.
# - Un .ecbiblias que no está en la lista se agrega al final, con lo que trae
#   adentro (nombre, sigla, idioma) y la licencia vacía para completar.
# - Al final dice qué Biblias va a mostrar el programa y cuáles no, y por qué:
#   revisa lo mismo que revisa el programa al leer la lista.
#
# Se usa con doble clic en «Actualizar catalogo.bat», puesto en la misma
# carpeta que las Biblias. También: .\Actualizar catalogo.ps1 -Carpeta <ruta>

param([string]$Carpeta = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$Lista = Join-Path $Carpeta 'catalogo.json'
$Idiomas = @{ es = 'Español'; en = 'Inglés'; pt = 'Portugués'; fr = 'Francés'; de = 'Alemán';
              it = 'Italiano'; gn = 'Guaraní'; qu = 'Quechua'; ht = 'Criollo haitiano' }
$Orden = @('name', 'abbreviation', 'language', 'file', 'size', 'sha256', 'license')

function Escribir([string]$marca, [string]$color, [string]$texto) {
    Write-Host ('  {0,-7}' -f $marca) -ForegroundColor $color -NoNewline
    Write-Host $texto
}

function Texto($valor) {
    if ($null -eq $valor) { return '' }
    return ([string]$valor).Trim()
}

# Lo que dice el manifest.json de adentro del .ecbiblias.
function Leer-Paquete([string]$ruta) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ruta)
    try {
        $entrada = $zip.GetEntry('manifest.json')
        if ($null -eq $entrada) { throw 'no es un paquete de Biblias (le falta manifest.json)' }
        $lector = New-Object System.IO.StreamReader($entrada.Open(), [System.Text.Encoding]::UTF8)
        try { $manifest = $lector.ReadToEnd() | ConvertFrom-Json } finally { $lector.Dispose() }
        $versiones = @($manifest.versions)
        if ($versiones.Count -ne 1) { throw "trae $($versiones.Count) versiones: en la lista va un archivo por versión" }
        return $versiones[0]
    }
    finally { $zip.Dispose() }
}

function Huella([string]$ruta) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $ruta).Hash.ToLowerInvariant()
}

# JSON escrito a mano: Windows PowerShell 5.1 lo escribe con sangrías raras y
# cambia las comillas simples por '. Acá los acentos quedan legibles.
function Json-Texto([string]$s) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('"')
    foreach ($c in $s.ToCharArray()) {
        switch ($c) {
            '"' { [void]$sb.Append('\"') }
            '\' { [void]$sb.Append('\\') }
            "`n" { [void]$sb.Append('\n') }
            "`r" { [void]$sb.Append('\r') }
            "`t" { [void]$sb.Append('\t') }
            default {
                if ([int]$c -lt 32) { [void]$sb.Append(('\u{0:x4}' -f [int]$c)) }
                else { [void]$sb.Append($c) }
            }
        }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

function Json-Valor($v) {
    if ($null -eq $v) { return 'null' }
    if ($v -is [bool]) { if ($v) { return 'true' } else { return 'false' } }
    if ($v -is [int] -or $v -is [long] -or $v -is [double] -or $v -is [decimal]) {
        return ([System.Convert]::ToString($v, [System.Globalization.CultureInfo]::InvariantCulture))
    }
    if ($v -is [string]) { return (Json-Texto $v) }
    # Otra cosa que alguien haya puesto a mano (una lista, un objeto): tal cual.
    return ($v | ConvertTo-Json -Compress -Depth 10)
}

function Escribir-Lista($lineas) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("{`n  `"format`": 1,`n  `"versions`": [")
    for ($i = 0; $i -lt $lineas.Count; $i++) {
        $linea = $lineas[$i]
        [void]$sb.Append("`n    {")
        $claves = @($Orden | Where-Object { $linea.Contains($_) }) + @($linea.Keys | Where-Object { $Orden -notcontains $_ })
        for ($j = 0; $j -lt $claves.Count; $j++) {
            $coma = if ($j -lt $claves.Count - 1) { ',' } else { '' }
            [void]$sb.Append("`n      $(Json-Texto $claves[$j]): $(Json-Valor $linea[$claves[$j]])$coma")
        }
        [void]$sb.Append("`n    }")
        if ($i -lt $lineas.Count - 1) { [void]$sb.Append(',') }
    }
    if ($lineas.Count -gt 0) { [void]$sb.Append("`n  ") }
    [void]$sb.Append("]`n}`n")
    return $sb.ToString()
}

function Tamano([long]$bytes) {
    if ($bytes -ge 1MB) { return ('{0:0.0} MB' -f ($bytes / 1MB)) }
    return ('{0} KB' -f [math]::Max(1, [math]::Floor($bytes / 1KB)))
}

Write-Host ''
Write-Host "Carpeta: $Carpeta"
Write-Host ''

# --- La lista como está hoy -------------------------------------------------
$lineas = New-Object System.Collections.ArrayList
$anterior = $null
if (Test-Path -LiteralPath $Lista) {
    $anterior = [System.IO.File]::ReadAllText($Lista, [System.Text.Encoding]::UTF8)
    try { $json = $anterior | ConvertFrom-Json }
    catch {
        Write-Host 'catalogo.json no se puede leer: tiene un error de escritura (una coma, una comilla o una llave de más o de menos).' -ForegroundColor Red
        Write-Host 'No se cambió nada. Corregilo y volvé a correr esto.' -ForegroundColor Red
        Write-Host "Detalle: $($_.Exception.Message)"
        exit 1
    }
    if ($json.format -ne 1) {
        Write-Host "catalogo.json dice ""format"": $($json.format), y tiene que ser 1. No se cambió nada." -ForegroundColor Red
        exit 1
    }
    foreach ($v in @($json.versions)) {
        if ($null -eq $v) { continue }
        $linea = [ordered]@{}
        foreach ($p in $v.PSObject.Properties) { $linea[$p.Name] = $p.Value }
        [void]$lineas.Add($linea)
    }
}
else {
    Write-Host 'No había catalogo.json: se arma uno nuevo.' -ForegroundColor Yellow
    Write-Host ''
}

$archivos = @(Get-ChildItem -LiteralPath $Carpeta -File -Filter '*.ecbiblias' | Sort-Object Name)
$enLaLista = @{}
$paraSubir = New-Object System.Collections.ArrayList
$bien = 0
$mal = 0

# --- Cada línea de la lista --------------------------------------------------
foreach ($linea in $lineas) {
    $archivo = Texto $linea['file']
    $problemas = New-Object System.Collections.ArrayList
    $avisos = New-Object System.Collections.ArrayList
    $falta = $false

    if (-not $archivo) {
        [void]$problemas.Add('le falta "file" (el nombre del archivo)')
    }
    elseif ($archivo -match '^[a-z]+://') {
        if ($archivo -notmatch '^https://') { [void]$problemas.Add('"file" tiene que empezar con https://') }
        [void]$avisos.Add('está en otra dirección: no se revisa el archivo')
    }
    else {
        $ruta = Join-Path $Carpeta $archivo
        $enLaLista[$archivo.ToLowerInvariant()] = $true
        if (-not (Test-Path -LiteralPath $ruta -PathType Leaf)) {
            # El programa la muestra igual (con el tamaño y la huella que ya
            # tiene escritos): sólo falla si tampoco está en Cloudflare.
            $falta = $true
            [void]$avisos.Add("$archivo no está en esta carpeta: no se pudo revisar. Si tampoco está en Cloudflare, la descarga va a fallar")
        }
        else {
            try {
                $paquete = Leer-Paquete $ruta
                foreach ($par in @(@('name', $paquete.name), @('abbreviation', $paquete.abbreviation), @('language', $paquete.language))) {
                    if (-not (Texto $linea[$par[0]]) -and (Texto $par[1])) { $linea[$par[0]] = Texto $par[1] }
                }
            }
            catch { [void]$problemas.Add("$archivo no se puede abrir: $($_.Exception.Message)") }

            $tam = (Get-Item -LiteralPath $ruta).Length
            $sha = Huella $ruta
            $antes = Texto $linea['sha256']
            if ($antes.ToLowerInvariant() -ne $sha -or [string]$linea['size'] -ne [string]$tam) {
                if ($antes) { [void]$avisos.Add('el archivo cambió: tamaño y huella actualizados') }
                [void]$paraSubir.Add($archivo)
            }
            $linea['size'] = $tam
            $linea['sha256'] = $sha
        }
    }

    # Lo mismo que mira el programa: sin esto, la Biblia no aparece.
    if (-not (Texto $linea['name'])) { [void]$problemas.Add('le falta "name" (el nombre)') }
    if (-not (Texto $linea['abbreviation'])) { [void]$problemas.Add('le falta "abbreviation" (la sigla)') }
    if (-not ($linea['size'] -as [long]) -or ($linea['size'] -as [long]) -le 0) { [void]$problemas.Add('le falta "size" (el tamaño)') }
    if ((Texto $linea['sha256']) -notmatch '^[0-9a-fA-F]{64}$') { [void]$problemas.Add('"sha256" no es una huella válida (64 letras y números)') }
    if (-not (Texto $linea['license'])) { [void]$avisos.Add('sin licencia') }

    $idioma = (Texto $linea['language']).ToLowerInvariant()
    if (-not $idioma) { $idioma = 'es' }
    $nombreIdioma = if ($Idiomas.ContainsKey($idioma)) { $Idiomas[$idioma] } else { $idioma.ToUpperInvariant() }
    $titulo = if (Texto $linea['abbreviation']) { Texto $linea['abbreviation'] } elseif ($archivo) { $archivo } else { '(sin sigla)' }
    $detalle = '{0,-8} {1,-10} {2,-8} {3}' -f $titulo, $nombreIdioma, (Tamano ([long]($linea['size'] -as [long]))), (Texto $linea['name'])

    if ($problemas.Count -gt 0) {
        $mal++
        Escribir 'ERROR' Red "$detalle"
        foreach ($p in $problemas) { Write-Host "           - $p" -ForegroundColor Red }
    }
    elseif ($falta) {
        $bien++
        Escribir 'FALTA' Yellow "$detalle"
    }
    else {
        $bien++
        Escribir 'OK' Green "$detalle"
    }
    foreach ($a in $avisos) { Write-Host "           · $a" -ForegroundColor Yellow }
}

# --- Archivos que no estaban en la lista ---------------------------------------
foreach ($f in $archivos) {
    if ($enLaLista.ContainsKey($f.Name.ToLowerInvariant())) { continue }
    try { $paquete = Leer-Paquete $f.FullName }
    catch {
        $mal++
        Escribir 'ERROR' Red "$($f.Name): no se agregó"
        Write-Host "           - $($_.Exception.Message)" -ForegroundColor Red
        continue
    }

    $linea = [ordered]@{
        name         = Texto $paquete.name
        abbreviation = Texto $paquete.abbreviation
        language     = if (Texto $paquete.language) { (Texto $paquete.language).ToLowerInvariant() } else { 'es' }
        file         = $f.Name
        size         = $f.Length
        sha256       = Huella $f.FullName
        license      = ''
    }
    [void]$lineas.Add($linea)
    [void]$paraSubir.Add($f.Name)
    $bien++
    $idioma = $linea['language']
    $nombreIdioma = if ($Idiomas.ContainsKey($idioma)) { $Idiomas[$idioma] } else { $idioma.ToUpperInvariant() }
    Escribir 'NUEVA' Cyan ('{0,-8} {1,-10} {2,-8} {3}' -f $linea['abbreviation'], $nombreIdioma, (Tamano $f.Length), $linea['name'])
    Write-Host '           · agregada al final; falta la licencia ("license")' -ForegroundColor Yellow
}

# Dos líneas con la misma sigla: el programa las toma como la misma Biblia.
$repetidas = $lineas | Where-Object { Texto $_['abbreviation'] } | Group-Object { (Texto $_['abbreviation']).ToUpperInvariant() } | Where-Object Count -gt 1
foreach ($g in $repetidas) {
    Write-Host "  AVISO  la sigla $($g.Name) está $($g.Count) veces en la lista" -ForegroundColor Yellow
}

# --- Guardar y resumir --------------------------------------------------------
$nuevo = Escribir-Lista $lineas
Write-Host ''
if ($nuevo -ne $anterior) {
    [System.IO.File]::WriteAllText($Lista, $nuevo, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host 'catalogo.json actualizado.' -ForegroundColor Green
    [void]$paraSubir.Insert(0, 'catalogo.json')
}
else {
    Write-Host 'catalogo.json ya estaba al día: no se cambió.'
}

Write-Host ''
if ($mal -gt 0) {
    Write-Host "$bien Biblias listas para el programa, $mal con problemas: el programa no muestra las marcadas con ERROR." -ForegroundColor Yellow
}
else {
    Write-Host "$bien Biblias listas para el programa, ninguna con problemas." -ForegroundColor Green
}

if ($paraSubir.Count -gt 0) {
    Write-Host ''
    Write-Host 'Para que se vea en el programa, subí a Cloudflare R2:' -ForegroundColor Cyan
    foreach ($a in ($paraSubir | Select-Object -Unique)) { Write-Host "  - $a" }
}
Write-Host ''
