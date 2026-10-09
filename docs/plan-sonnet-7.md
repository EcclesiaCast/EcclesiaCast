# Plan de trabajo para Sonnet — actualizaciones automáticas

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2) y lo que agrega
[plan-sonnet-2.md](plan-sonnet-2.md) al principio:
- No abrir la aplicación ni ninguna ventana visible. Para lo visual, dibujá a
  PNG desde un programa de consola STA en tu carpeta temporal.
- No escribir nunca en la biblioteca real
  (`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`).
- No ejecutar python: no está instalado y llamarlo abre la Microsoft Store.
- Rama `beta6`, sin push. Un commit por punto, en inglés, con
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Castellano rioplatense para todo lo que se ve.
- CHANGELOG [Sin publicar] y `docs/pendientes.md` al día.
- Los archivos tienen fin de línea CRLF: sed y perl los rompen, así que usá la
  herramienta Edit.
- **No ejecutes nunca un instalador** ni nada que descargues.

## Qué quiere Alexis

Que el programa avise solo cuando hay una versión nueva y se actualice con un
botón, sin que nadie tenga que entrar a GitHub. Lo decidido:
- Avisa en castellano qué trae la versión nueva.
- Tres botones: **«Actualizar ahora»**, **«Más tarde»** y **«Saltar esta
  versión»**.
- **Nunca interrumpe mientras se está proyectando.**

## 1. Reglas en Core (con pruebas)

Todo lo que se pueda decidir sin internet ni ventanas va en
`EcclesiaCast.Core/Updates/`.

- **Comparar versiones** del estilo `1.0.0-beta.7`, `1.0.0-beta.10`, `1.0.0`
  y `1.0.1`, según SemVer 2.0: `beta.10` > `beta.9` (por número, no por
  texto); `1.0.0` > `1.0.0-rc.1` > `1.0.0-beta.10`. Se ignora lo que va después
  de `+` y una `v` al principio (las etiquetas son `v1.0.0`).
- **Elegir la versión a ofrecer** de una lista de *releases* de GitHub (nombre
  de etiqueta, si es *prerelease*, si es *draft*, texto, archivos):
  - Nunca un *draft*.
  - Si la versión instalada es **estable**, sólo se ofrecen estables. Si es
    beta o rc, se ofrecen también las *prerelease*: así quien prueba betas
    llega a la 1.0 y a las betas siguientes.
  - Sólo si es **mayor** que la instalada y **no fue saltada**. Saltar la
    1.0.1 no esconde la 1.0.2.
  - Sólo si tiene un archivo `EcclesiaCast-*-setup.exe`.
- **Cuándo buscar:**
  - Una vez al abrir, a los ~15 segundos, para no demorar el arranque.
  - A lo sumo **una vez por día**: guardar la fecha de la última búsqueda en
    los ajustes (`ISettingsStore`).
  - Nunca si está apagado en los ajustes, ni en un build de desarrollo
    (Debug, o versión que no se pueda leer).
- **El texto de «qué trae»:** el cuerpo del *release* es Markdown. Pasalo a
  texto legible: títulos, viñetas, negritas y enlaces (`[texto](url)` →
  `texto`). Sin HTML ni WebView.
- **Leer la respuesta de la API de GitHub** (`GET
  https://api.github.com/repos/EcclesiaCast/EcclesiaCast/releases`, JSON)
  con `System.Text.Json`, probado con un JSON de ejemplo guardado en las
  pruebas. Tomá de cada archivo el nombre, `browser_download_url`, `size` y
  `digest` (`"sha256:…"`; GitHub lo da en los archivos nuevos y puede faltar
  en los viejos).

## 2. Buscar y avisar (App)

- Un servicio `UpdateChecker`. `HttpClient` con `User-Agent:
  EcclesiaCast/<versión>` (GitHub lo exige) y un tiempo límite de 15
  segundos.
- Sin internet, con un error o con el límite de pedidos de GitHub: **no
  molesta**; sólo lo anota en el registro.
- **Si hay una versión nueva:**
  - **Si no se está proyectando** (la ventana de salida no está prendida;
    buscá cómo se sabe hoy en `IProjectionWindowService` y el MainViewModel):
    abre la ventana de aviso.
  - **Si se está proyectando:** no abre nada. Aparece un botón discreto en
    la barra de arriba, **«⬆ Versión nueva»**, y al tocarlo se abre la
    ventana. El botón queda hasta que se actualice o se salte esa versión.
- **La ventana de aviso** (`UpdateWindow`, oscura como las demás, centrada en
  la ventana principal):
  - Título «Hay una versión nueva de EcclesiaCast».
  - «Tenés la 1.0.0-beta.7 · La nueva es la 1.0.0».
  - El texto de qué trae, con desplazamiento si es largo.
  - Los tres botones:
    - **«Actualizar ahora»** (el principal): punto 3.
    - **«Más tarde»:** cierra; vuelve a avisar la próxima vez que se abra el
      programa (respetando la vez por día).
    - **«Saltar esta versión»:** guarda esa versión en los ajustes y no avisa
      más por ella.
- **Menú Ayuda:**
  - «Buscar actualizaciones…» busca en el momento, aunque ya se haya buscado
    hoy y aunque esté saltada. Si no hay nada: «Tenés la última versión
    (1.0.0)». Sin internet: «No se pudo buscar: revisá la conexión a
    internet».
  - Casilla «Buscar actualizaciones al abrir», prendida de entrada.

## 3. Actualizar ahora

1. **Descargar** el instalador a una carpeta temporal propia
   (`%TEMP%\EcclesiaCast-actualizacion\`), con una barra de progreso en la
   misma ventana y un botón «Cancelar».
   - Si la descarga falla: «No se pudo descargar la actualización», y el
     botón para reintentar.
2. **Verificar** antes de ejecutar:
   - Que el tamaño coincida con `size`.
   - Si hay `digest`, que el SHA-256 coincida.
   - **Firma digital** (la vamos a tener con SignPath): si el programa
     instalado está firmado (`EcclesiaCast.App.exe` o el desinstalador; buscá
     cómo leerlo con `X509Certificate.CreateFromSignedFile` o WinVerifyTrust),
     el instalador nuevo **tiene que** estar firmado y con el mismo firmante.
     Si no, no se ejecuta y se avisa. Si el instalado no está firmado (como
     hoy), se acepta sin firma.
   - Si algo no coincide: se borra el archivo y se avisa «La actualización
     descargada no es válida; no se instaló nada».
3. **Copia de seguridad automática** de la biblioteca antes de instalar,
   porque la versión nueva puede migrar la base. Copiá `ecclesiacast.db` (con
   el mismo cuidado que el respaldo actual: mirá `IBackupDialog` y su
   servicio) a `%APPDATA%\EcclesiaCast\respaldos\antes-de-<versión>.db`.
   Guardá sólo las 3 últimas.
4. **Ejecutar el instalador** con `/SILENT /SP- /NOCANCEL /RELANZAR` y
   **cerrar EcclesiaCast enseguida**. El instalador muestra sólo su barra de
   progreso.
   - Windows va a pedir permiso de administrador: lo acepta el operador.
   - En `installer/EcclesiaCast.iss`, la entrada de `[Run]` que abre el
     programa tiene `skipifsilent`. Sumá otra que lo abra **cuando se pasó
     `/RELANZAR`** (una función `Check` que mire `ExpandConstant('{param:…}')`
     o `ParamStr`). Así, después de actualizar, el programa se vuelve a
     abrir solo.
   - Revisá que el instalador cierre o espere al programa si sigue abierto
     (`CloseApplications`/`AppMutex`): mirá qué hay hoy y que no quede a
     medias.
5. Si el operador apretó «Actualizar ahora» **mientras proyectaba** (desde el
   botón «⬆ Versión nueva»), preguntá antes: «Se va a cerrar la proyección
   para actualizar. ¿Seguimos?».

## 4. Las notas de cada versión en castellano

Hoy `.github/workflows/release.yml` publica el *release* con
`generate_release_notes: true` (títulos de commits en inglés) más el renglón
de la política de firma.
- Cambialo para que el cuerpo sea **la sección de esa versión del
  `CHANGELOG.md`**: la que empieza con `## [1.0.0]`, hasta la próxima `## [`.
  Arriba va el renglón de la política de firma.
- Si la sección no existe, que use la de `[Sin publicar]`, y si tampoco, las
  notas generadas como ahora.
- La extracción, en PowerShell dentro del workflow. Probala localmente
  corriendo ese mismo PowerShell sobre el `CHANGELOG.md` real.
- En `docs/pendientes.md` §2 (los pasos para publicar la 1.0) anotá que, al
  publicar, `[Sin publicar]` se renombra a `[1.0.0] - fecha`.

## 5. Documentación

- **`docs/privacidad.md`:** una fila nueva, «Al abrir el programa (una vez
  por día; se apaga en Ayuda) | GitHub | Nada tuyo: pregunta cuál es la
  última versión. Al actualizar, baja el instalador nuevo». Sacá el último
  párrafo, el que dice que algún día se iba a conectar solo, porque ahora
  pasa y está en la lista.
- **Guía de uso:** sección «Actualizaciones».
- **CHANGELOG** [Sin publicar].
- **`docs/pendientes.md`:** en §1 «Para probar», los pasos de abajo; en §2,
  marcar hechas las actualizaciones automáticas.

## Pruebas

- **En Core:**
  - comparar versiones, con todos los casos de arriba más los raros (`1.0`,
    `v1.0.0`, `1.0.0+abc`, texto inválido);
  - elegir la versión (estable contra beta, *draft*, saltada, sin
    instalador);
  - cuándo buscar (una vez por día, apagado);
  - Markdown a texto;
  - leer el JSON de ejemplo.
- **Descarga y verificación,** con un servidor HTTP local de prueba (que la
  URL de la API y la del archivo se puedan cambiar): descarga correcta,
  tamaño equivocado, SHA-256 equivocado, descarga cortada. **No ejecutes el
  instalador**: dejá esa parte detrás de una interfaz que en la prueba no
  hace nada.
- **Firma:** probá la lectura del firmante con algún `.exe` firmado del
  sistema (por ejemplo `C:\Windows\explorer.exe`) y con el
  `EcclesiaCast.App.exe` sin firmar.
- **Copia de seguridad:** sobre una copia de la base en tu carpeta temporal,
  nunca sobre la real; que queden sólo 3.
- **Dibujos a PNG** de la ventana de aviso (con un texto de cambios largo y
  uno corto, en la descarga y en el error) y del botón «⬆ Versión nueva» en
  la barra.
- La extracción del CHANGELOG del workflow, sobre el `CHANGELOG.md` real.
- Compilación con 0 advertencias y todas las pruebas en verde (hoy 574).

## Al terminar

- Rearmá el instalador de la beta.7 como en `.github/workflows/release.yml`:
  borrá `publish`; corré `dotnet publish
  src/EcclesiaCast.App/EcclesiaCast.App.csproj --configuration Release
  --runtime win-x64 --self-contained true -p:PublishSingleFile=false
  -p:Version=1.0.0-beta.7 --output publish`; y después
  `"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.7
  installer\EcclesiaCast.iss`.
- Informe final en castellano, para Alexis:
  - qué quedó hecho (con commits);
  - cómo lo probaste;
  - qué no pudiste probar;
  - qué decidiste vos.

### Para «Para probar» (§1 de pendientes)

1. Con la beta.7 instalada, cuando esté publicada una versión mayor en
   GitHub, abrí el programa: a los pocos segundos aparece el aviso con lo que
   trae.
2. «Más tarde» → no vuelve a avisar hasta mañana o la próxima vez que lo
   abras. «Saltar esta versión» → no avisa más por esa.
3. Con la salida prendida, no aparece ninguna ventana: aparece «⬆ Versión
   nueva» arriba.
4. «Actualizar ahora» → descarga, se cierra, Windows pide permiso, el
   instalador actualiza y el programa se vuelve a abrir solo. Las canciones y
   todo lo demás siguen ahí, y hay una copia en
   `%APPDATA%\EcclesiaCast\respaldos`.
5. Ayuda → «Buscar actualizaciones…» sin nada nuevo dice «Tenés la última
   versión».
