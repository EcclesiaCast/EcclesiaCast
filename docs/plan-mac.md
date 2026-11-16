# Plan: EcclesiaCast para Mac

15/11/2026. Escrito por Fable (arquitecto) después de revisar el código, las
ramas abiertas y los datos de afuera (Apple, Avalonia, GitHub). Las decisiones
de Alexis del 15/11 están en la sección 19. Los encargos para cada modelo están
en `docs/plan-mac-1.md` en adelante, y las reglas comunes en
[plan-mac-reglas.md](plan-mac-reglas.md).

## 0. Resumen de una página

**Qué se hace.** Una sola base de código para Windows y Mac. La interfaz se
pasa de WPF (que sólo existe en Windows) a **Avalonia** (se escribe casi igual
y funciona en los dos sistemas). Mientras tanto la versión de Windows **sigue
exactamente como está**: la interfaz nueva se arma al lado, y Windows recién
cambia cuando la nueva la iguale en todas las comparaciones y en tu prueba en
la iglesia.

**Por qué se puede.** El programa ya está bien separado por dentro: la lógica
(canciones, Biblias, playlist, base de datos, control desde el celular) no
depende de Windows, y el video ya se dibuja de una forma que se puede llevar a
Mac sin que tape la letra. Lo que hay que rehacer son las 48 pantallas y unas
veinte piezas que le hablan directo a Windows.

**Orden.**

| Fase | Qué | Quién | Resultado |
|---|---|---|---|
| 0 | Prototipo chico en una Mac real. Pruebas automáticas en Mac. Pasar a .NET 10 | Opus + Sonnet | Sabemos si el camino sirve, sin tocar el programa |
| 1 | Separar del programa todo lo que no es pantalla | Opus + Sonnet | Windows igual que hoy, pero listo para tener dos interfaces |
| 2 | La salida al proyector en Avalonia (letra, video, escenario) | Opus | La letra se ve **igual** en la interfaz vieja y la nueva |
| 3 | La ventana del operador y los 40 y pico diálogos | Sonnet, en fila | El programa entero funciona en Mac |
| 4 | Empaquetar, página web, actualización, guía | Opus + Sonnet | **Beta para Mac publicada** |
| 5 | Igualar a Windows lo que falta y retirar la interfaz vieja | Opus + Sonnet | Una sola interfaz para los dos sistemas |
| 6 | Lo que quedó afuera en Mac (web, YouTube en vivo, capturar ventanas…) | según pedidos | Mac deja de ser beta |

**Qué no va a tener la primera versión de Mac** (lo aceptaste el 15/11):
capturar otra ventana, panel web y YouTube en vivo (queda YouTube descargado),
PowerPoint directo (se carga el PDF), seguir la canción y el control de
Spotify. Tampoco el corrector de ortografía.

**Cuánto lleva.** Entre 35 y 45 sesiones de modelos (unas 12 de Opus, 20 de
Sonnet, 6 de revisión mía). En tiempo de calendario, de 3 a 5 meses hasta la
beta de Mac, porque el ritmo lo marcan tus pruebas. Detalle en la sección 17.

**Cuánto cuesta en plata.** Nada obligatorio: no se paga la cuenta de Apple
(decisión tuya), las Mac de GitHub son gratis para proyectos públicos, y la
Mac de prueba es prestada. Como mucho, un adaptador de USB-C a HDMI.

**Qué te voy a pedir.** Cuatro visitas a la Mac prestada (una por fase, de una
hora cada una, con una lista de pasos), conseguir de 3 a 5 probadores entre
los que la piden en Instagram, y probar en la iglesia dos domingos la interfaz
nueva de Windows antes de retirar la vieja. Detalle en la sección 18.

**Cuándo se frena.** Si después de 3 sesiones de Opus el prototipo no cumple
los 6 puntos obligatorios en la Mac real, el proyecto de Mac se pausa. Hasta
ahí no se tocó nada del programa, así que no se pierde nada.

**Primer paso concreto:** publicar la 1.4.0 (con «Seguir la canción»
escondido), averiguar qué Mac te prestan (menú  → «Acerca de esta Mac»:
qué chip y qué versión de macOS) y lanzar la tanda 1, el prototipo
([plan-mac-1.md](plan-mac-1.md)), que no toca el programa y puede empezar ya.

---

## 1. Lo que encontré en el código

Hice el inventario sobre la rama `claude/determined-mahavira-fd2c61`, que es
la que junta todo lo que va a ser la 1.4.0 (52 cambios que todavía no están
en `main`). Es lo que va a existir cuando empiece el trabajo de Mac.

Cosas que **contradicen el encargo** (gana el código):

- **`MainViewModel.cs` no tiene 5.000 líneas: tiene 456.** Ya está partido en
  29 archivos (`MainViewModel.Bible.cs`, `.Media.cs`, `.Playlist.cs`…), que
  suman unas 10.200 líneas. Es buena noticia: se puede mover por partes.
- **El programa no incluye ninguna fuente.** Anton está sólo en la página web
  (`sitio/fuentes/`). Los temas usan las fuentes que tenga Windows, y la de
  fábrica es «Segoe UI», que **no existe en Mac**. Hay que resolverlo (sección
  11).
- **La rama de integración suma cosas que el encargo no nombra:** `NAudio`
  (entrada de sonido), `Whisper.net` (reconocer el canto), una ventana de
  Ajustes, una papelera, avisos propios (`NoticeWindow`) y el buscador de
  Pixabay. Con eso son **48 archivos XAML** (8.600 líneas) y 45.000 líneas en
  la App, no 43 y 42.000.
- **No encontré la rama donde «se está partiendo la ventana principal».**
  Ninguna rama ni copia de trabajo tiene cambios así (`MainWindow.xaml` sigue
  entero, 2.600 líneas). Si ese trabajo está en una sesión abierta y sin
  guardar, tiene que entrar a `main` **antes de la fase 1**, porque la fase 1
  mueve los mismos archivos.
- **El programa no tiene «una sola copia abierta» ni evita que la pantalla se
  duerma** con código propio. En Mac conviene agregar las dos cosas (sección
  13).
- **.NET 8 se quedó sin soporte de Microsoft el 10/11/2026** (hace cinco días).
  No es de Mac, pero hay que pasar a .NET 10 igual, y conviene hacerlo antes de
  empezar (tanda 3).

Buenas noticias que simplifican el plan:

- **El video ya se dibuja «a mano»** (`Controls/VlcVideoSurface`): LibVLC
  entrega cada cuadro y el programa lo pinta en una imagen común, con la letra
  encima. Ese método existe igual en Avalonia. El problema clásico de «el
  video tapa la letra» no aparece.
- **Ya hay 26 interfaces de servicios** (`IProjectionWindowService`,
  `IWindowCapture`, `IWebPageHost`, `ISongEditor`…) y todo se arma en un solo
  lugar (`App.xaml.cs`). La separación que hace falta ya está empezada.
- **El servidor para el celular y OBS** está escrito sobre conexiones comunes
  (`TcpListener`), sin nada de Windows. Funciona en Mac tal cual.
- **La carpeta de datos** sale de una sola línea (`App.DataDir`), y en Mac esa
  misma línea da `~/Library/Application Support/EcclesiaCast`.
- **No se usan los controles de WPF más difíciles de pasar** (DataGrid,
  ListView con columnas, animaciones declaradas en XAML): cero.

## 2. Inventario de lo que depende de Windows

Esfuerzo: **C** chico (una parte de una sesión), **M** mediano (una sesión),
**G** grande (varias sesiones o riesgo alto).

### 2.1 Se reutiliza (casi) como está

| Pieza | Qué hay que tocar | Esf. |
|---|---|---|
| `EcclesiaCast.Core` (10 mil líneas) y `EcclesiaCast.Data` (14 mil) | Nada de fondo. Revisar: `Updates/CodeSignature` (ya se salta sola si no es Windows), `Performance/LowPerformance` (lee el registro que deja el instalador), rutas de los importadores | C |
| `tests/EcclesiaCast.Core.Tests` (11 mil líneas) | Correrlas también en Mac; arreglar las que supongan rutas de Windows | C |
| ViewModels (34 archivos; `MainViewModel` son 29 partes, 10.200 líneas) | Sacarles lo poco de WPF que tienen: 6 relojes (`DispatcherTimer`), `Application.Current` (unas 12 veces), 13 archivos con diálogos de abrir/guardar de Windows, 6 ventanas que se abren directo, 2 imágenes (`ImageSource`) | M |
| Las 26 interfaces de servicios (`Services/I*.cs`) | Nada | — |
| Servidor del celular y salidas de OBS (`Remote/`) | Una línea que usa el hilo de WPF | C |
| `YtDlp`, `ToolInstaller`, `LightVideoCache`, `GifImporter`, `UsageReporter`, `UpdateChecker` | Nombres y direcciones por sistema (`yt-dlp.exe` → `yt-dlp_macos`; `deno` para Mac); buscar `ffmpeg` también en las carpetas de Homebrew | C–M |
| Importadores de ProPresenter y EasyWorship (leen archivos, están en Core) | Sumar las carpetas de Mac de ProPresenter (`~/Documents/ProPresenter`). EasyWorship es sólo de Windows, pero una carpeta copiada se puede elegir a mano | C |
| Paquetes de Biblias `.ecbiblias` y catálogo para bajar | Nada (probar en Mac) | C |

### 2.2 Se reemplaza (la misma función, hecha de nuevo para los dos sistemas)

| Pieza | Por qué | Con qué | Esf. |
|---|---|---|---|
| 48 pantallas XAML (8.600 líneas) y `App.xaml` (736 líneas de estilos) | WPF | AXAML de Avalonia. Lo trabajoso: 439 «Triggers» y 160 plantillas de controles, que en Avalonia se escriben distinto | **G** |
| Código de las pantallas (`MainWindow` 1.600 líneas + 4 partes, diseñadores de tema, canción y escenario) | WPF | Avalonia. 45 propiedades de control, 65 animaciones hechas en código, arrastrar tarjetas | **G** |
| `SlideView`, `ProjectedView`, `SlideTextFit`, `TextFit`, `StageBoxView` (cómo se dibuja y se ajusta la letra) | Usan el motor de texto de WPF | El de Avalonia. **Riesgo: que la letra corte distinto.** Sección 7.2 | **G** |
| `VlcVideoSurface` (1.033 líneas), `VideoThumbnail` | Imagen de WPF | Imagen de Avalonia; mismo método | M |
| `OutputWindow`, `StageWindow` (pantalla completa sin robar el foco: `SetWindowPos`, `GetWindowLong`) | Windows | Avalonia + un toque nativo por sistema | M |
| `ScreenDisplayProvider`, `OperatorScreen` (lista de pantallas con WinForms) | WinForms | Lista de pantallas de Avalonia | C |
| Diálogos de abrir y guardar archivo (13 archivos) | Windows | Los de Avalonia, detrás de una interfaz | C–M |
| Lista de fuentes del sistema (7 lugares) | WPF | Catálogo de fuentes común + tabla de reemplazos | C |
| Portapapeles, arrastrar y soltar archivos | WPF | Avalonia | C |
| `ProcessInstallerLauncher` (ejecuta el instalador como administrador) | Windows | Uno por sistema (sección 10) | M |
| `DocumentImporter` (dibuja PDF con Windows) | Windows | En Windows queda igual; en Mac, PDFium (motor de PDF libre) | M |
| `UiWatchdog` (vigila que la ventana responda) | Mensajes de Windows | Hilo de Avalonia | C |
| Instalador Inno Setup | Windows | En Mac, un `.dmg` (sección 8) | M |
| `SpellChecking` (corrector en español de WPF) | WPF | Avalonia no trae corrector. Hunspell (libre) para los dos sistemas. **Tiene que estar antes de retirar WPF en Windows** | M |

### 2.3 En Mac queda degradado o no está (al principio)

| Pieza | Hoy en Windows | En la primera versión de Mac | Después | Esf. |
|---|---|---|---|---|
| `WebPanel`, `WebLiveFeed`, `WebViewProfile` (panel 🌐 Web) | WebView2 dibujando en una imagen | **No está** | WebKit de Mac; no puede dibujar en una imagen, hay que buscarle la vuelta | **G** |
| `YouTubeSurface`, `YouTubeBrowserWindow`, `YouTubeCookies` | WebView2 con la cuenta de la iglesia | **YouTube sólo descargado** (ya existe) | WebKit de Mac | **G** |
| `MusicaComBrowserSearch` (búsqueda completa de musica.com) | WebView2 invisible | Búsqueda simple + LRCLIB | Con WebKit | M |
| Vista previa de «Estilos de transmisión» | WebView2 | Sin vista previa dentro del programa (se ve en el navegador) | Con WebKit | C |
| `WindowCapture`, `WindowNative`, `WindowPickerWindow` | Captura de Windows | **No está** | ScreenCaptureKit de Apple; pide permiso de «Grabación de pantalla» | **G** |
| `PowerPointConverter` | Maneja PowerPoint por COM | **Se carga el PDF** (el programa lo explica) | Pedirle el PDF a Keynote o PowerPoint de Mac | M |
| `SystemMediaPlayer`, `MediaKeys` (Spotify y lo que suene en la PC) | Controles de Windows | **No está** | Spotify y Música de Apple por AppleScript | M |
| `Listening/SoundInput`, `SpeechEar` (seguir la canción) | NAudio + Whisper | **No está** (igual en Windows queda escondido) | Entrada de sonido de Mac + Whisper | **G** |
| `AudioEndpointWatcher` (cambio de parlantes) | NAudio | Probar si hace falta; LibVLC en Mac sigue solo a la salida elegida | — | C |
| `PowerThrottling` (que Windows no frene el programa) | Windows | Pedirle a macOS que no duerma ni frene (`caffeinate`) | — | C |
| `ShellThumbnail` (miniaturas del Explorador) | Windows | Miniatura sacada por LibVLC, que ya existe | — | C |
| `DarkTitleBar` | Windows | No hace falta: Mac sigue el modo del sistema | — | — |
| Opción «PC de bajo rendimiento» del instalador | Registro de Windows | Sólo desde Ajustes | — | — |
| Corrector de ortografía | WPF | **No está** | Hunspell | M |

## 3. Decisión de framework

**Recomendación: Avalonia 12, sobre .NET 10.** Verificado el 15/11/2026:
versión 12.1.6, licencia MIT (libre y gratis), Mac con chip Apple y con Intel,
macOS 14 a 26.

Por qué:

1. **Es lo más parecido a WPF que existe.** Mismo lenguaje de pantallas
   (XAML), mismos conceptos. Los ViewModels y `CommunityToolkit.Mvvm` no
   cambian. Un modelo que sabe WPF pasa una ventana en una sesión.
2. **Tiene las piezas justas que necesitamos:** imagen que se puede pintar
   cuadro a cuadro (para el video), sombras y desenfoque, lista de pantallas,
   ventanas sin borde, menú global de Mac, arrastrar archivos.
3. **Se puede probar sin abrir ninguna ventana** (`Avalonia.Headless` dibuja
   en memoria y guarda la imagen). Eso calza con la regla de no abrir el
   programa en tu PC, y sirve para comparar la letra en Windows y en Mac
   dentro de GitHub.
4. **Tiene control de página web propio, libre** (`Avalonia.Controls.WebView`,
   MIT): usa WebView2 en Windows y WebKit en Mac.
5. **Se compila para Mac desde Windows.** No hace falta una Mac para programar,
   sólo para probar.

Alternativas que miré y por qué no:

| Alternativa | Qué es | Por qué no |
|---|---|---|
| **Avalonia XPF** | Hace funcionar el WPF actual en Mac casi sin tocarlo | Es pago: **29.500 euros** por programa. Además es cerrado y no trae WebView2 ni lo de Windows |
| **.NET MAUI** | El de Microsoft para celular y escritorio | En Mac funciona como app de iPad adaptada; mal manejo de varias ventanas y pantallas. Hay que reescribir todo igual, en un XAML distinto |
| **Uno Platform** | Parecido a Avalonia, con el XAML de WinUI | Más lejos de WPF, menos usado en Mac de escritorio. No da ninguna ventaja |
| **Reescribir en web** (Electron, Tauri) | Interfaz en HTML | Tirar 45 mil líneas. Otro producto |
| **App nativa de Mac** (Swift) | Como ProPresenter | Dos programas distintos para siempre, en dos lenguajes |
| **Windows dentro de la Mac** (Parallels, CrossOver) | No hacer versión de Mac | Cuesta plata a cada iglesia y WebView2 y el video no funcionan bien. Sirve sólo como respuesta provisoria a quien pregunte hoy |

**¿Reemplazar WPF en Windows, o tener las dos interfaces un tiempo?** Las dos
un tiempo, con fecha de vencimiento (decidido el 15/11):

- Cambiar todo de golpe es el camino más corto, pero una migración de 48
  pantallas hecha por modelos que no pueden abrir el programa en tu PC va a
  tener fallas, y le llegarían a todas las iglesias que ya lo usan.
- Dos interfaces para siempre duplica cada pantalla nueva para siempre.
- **Convivir:** la interfaz nueva es un proyecto aparte (`EcclesiaCast.Desktop`)
  que comparte con la vieja toda la lógica. Primero sale para Mac como beta
  (ahí no hay usuarios que puedan «empeorar»). Cuando en Windows iguala a la
  vieja, la reemplaza. Mientras conviven, **en Windows sólo arreglos y cambios
  chicos** (decidido): la lógica se escribe una vez; lo que toque una pantalla
  se hace en las dos.

**Dónde puede fallar Avalonia** (y por eso hay prototipo): el texto no lo
dibuja el mismo motor que en WPF, así que hay que medir que la letra corte
igual; la pantalla completa en Mac necesita un toque nativo para tapar la
barra de menú; y el control de web no dibuja en una imagen como el de hoy.
Los tres tienen plan B (sección 16).

## 4. Prototipo de validación (tanda 1)

Un programa chico y aparte (`proto/EcclesiaCast.Proto`), hecho con Avalonia,
que usa `Core` y `Data` de verdad. **No toca `src/`.** GitHub lo compila para
Mac y deja un archivo para bajar. Encargo completo:
[plan-mac-1.md](plan-mac-1.md).

**Obligatorios** (los seis tienen que cumplirse en la Mac real):

| # | Qué | Cómo se mide |
|---|---|---|
| P1 | Abre en macOS 15 o más nuevo con chip Apple, después de «Abrir igualmente». Crea la base en `~/Library/Application Support/EcclesiaCast-proto/` y carga una Biblia de las que vienen | La carpeta existe; se ve un versículo; el registro no tiene errores |
| P2 | Proyecta a **pantalla completa en el segundo monitor**: sin barra de menú, sin Dock, sin borde, sin robarle el foco a la ventana del operador | Foto del proyector; se puede seguir escribiendo en la ventana del operador; sigue tapando todo al cambiar de programa |
| P3 | Una **canción y un versículo** con fuente, contorno y sombra del tema; igual en la vista previa y en la salida | Foto comparada con la misma diapositiva en Windows |
| P4 | **Video de fondo en bucle con la letra encima**, 1080p, 10 minutos | El registro anota cuadros por segundo: 28 o más para un video de 30; menos de 1 % de cuadros perdidos; la memoria no sube más de 50 MB; nunca un cuadro negro al volver a empezar |
| P5 | **Control desde el celular** en la misma red wifi | Siguiente y anterior cambian la diapositiva en menos de medio segundo; se anota qué pregunta macOS (firewall, red local) |
| P6 | **Desenchufar y enchufar el proyector** mientras proyecta | No se cierra ni se cuelga; avisa; se puede volver a proyectar sin reiniciar |

**Informativos** (se anotan, no frenan): proyector en espejo; pantalla Retina;
el sonido del video sale por HDMI; con una sola pantalla Esc vuelve; después de
10 minutos con otro programa adelante el video no se frena; tamaño de la
descarga; cuánto tarda en abrir; un video 4K.

**Sin Mac** se comprueba antes: compila en Windows sin advertencias, las
pruebas dibujan una diapositiva en memoria y la comparan, y GitHub arma el
archivo para Mac sin errores.

**Qué decide el prototipo:** si se sigue con Avalonia, y de dónde sale LibVLC
para chip Apple. No pude confirmar que el paquete oficial
`VideoLAN.LibVLC.Mac` (versión 3.1.3.2 del 13/10/2026) lo traiga: su
documentación dice sólo Intel. Es lo primero que verifica la tanda 1; el plan
B es tomar las librerías del VLC oficial para chip Apple al armar el paquete.

## 5. Estructura del código

Hoy:

```
Core  ←  Data  ←  App (WPF: pantallas + ViewModels + servicios, todo junto)
```

A dónde vamos:

```
Core, Data                     lógica y base de datos (ya son comunes)
Presentation      (nuevo)      ViewModels + servicios que no dibujan + interfaces de plataforma
Platform.Windows  (nuevo)      lo que le habla a Windows: captura, PDF, PowerPoint, teclas, sonido
Platform.Mac      (nuevo)      lo que le habla a macOS: no dormir, rutas, y después captura, etc.
Desktop           (nuevo)      las pantallas en Avalonia, para Windows y Mac
App               (el de hoy)  las pantallas en WPF; se borra al final de la fase 5
```

`Presentation` se compila como .NET común, sin WPF: si alguien le mete algo
de Windows, **no compila**. Esa es la garantía de que la lógica es una sola.

### 5.1 Cómo se evita duplicar la lógica de las 48 pantallas

1. **Todo lo que decide algo va al ViewModel.** Las dos interfaces usan los
   mismos ViewModels. Una pantalla sólo ubica controles y los conecta.
2. **Lo que hoy está en el código de una ventana y decide algo** (unas 20
   ventanas tienen más de 100 líneas así) se muda a su ViewModel o a un
   servicio **en la fase 1, en WPF, antes de migrar**. Así se prueba en
   Windows con la interfaz de siempre y la versión de Avalonia nace finita.
3. **Las cuentas de dibujo** (qué tamaño de letra entra, dónde va cada franja,
   el encuadre de una imagen) se mudan a `Presentation` detrás de una interfaz
   que mide texto. La fórmula es una sola; cada interfaz sólo mide.
4. **Las animaciones hechas en código** (65) pasan por un ayudante único
   («fundir este control en tanto tiempo»), con una versión para cada interfaz.

### 5.2 Interfaces de plataforma

Las nuevas van en `Presentation/Platform/`. Las que ya existen se mudan ahí.

| Interfaz | Para qué | Windows | Mac |
|---|---|---|---|
| `IUiDispatcher`, `IUiTimer` | Volver al hilo de la pantalla; relojes | WPF / Avalonia | Avalonia |
| `IDialogs` | Abrir y guardar archivo, elegir carpeta | Los de cada interfaz | Avalonia |
| `INotices` | Avisos y preguntas (hoy `Notice`) | Las dos interfaces | Avalonia |
| `IImageFactory`, `IImageHandle` | Imágenes que guardan los ViewModels | Imagen de cada interfaz | Avalonia |
| `IPlatformPaths` | Carpeta de datos, de videos, de herramientas, de lo que viene con el programa | `%APPDATA%`, carpeta del programa | `~/Library/Application Support`, `Contents/Resources` |
| `IDisplayProvider` (existe) | Lista de pantallas y aviso cuando cambian | WinForms / Avalonia | Avalonia |
| `IOutputWindowNative` | Pantalla completa sin foco, por encima de todo | `SetWindowPos` | Nivel de ventana de macOS |
| `IFontCatalog` | Fuentes instaladas, las que vienen con el programa y los reemplazos | Común | Común |
| `ITextMeasurer` | Medir texto para el ajuste de la letra | WPF / Avalonia | Avalonia |
| `IWindowCapture` (existe) | Capturar otra ventana | Captura de Windows | «No disponible» → después ScreenCaptureKit |
| `IDocumentRenderer` | PDF a imágenes | Windows | PDFium |
| `IDeckConverter` | PowerPoint a PDF | COM | «Exportalo a PDF» → después Keynote |
| `IWebPageHost`, `IYouTubeBrowser` (existen) | Página web y YouTube | WebView2 | «No disponible» → después WebKit |
| `ISystemMedia` | Lo que suena en la computadora y las teclas multimedia | Windows | «No disponible» → después AppleScript |
| `ISoundInput`, `ISpeechEar` | Seguir la canción | NAudio + Whisper | «No disponible» |
| `IPowerGuard` | Que el sistema no frene ni duerma | EcoQoS | `caffeinate` |
| `IThumbnailer` | Miniaturas | Explorador + LibVLC | LibVLC |
| `IUpdateInstaller` | Instalar la versión nueva | Instalador `.exe` | Cambiar el `.app` |
| `ISpellChecker` | Ortografía | WPF → Hunspell | Hunspell |
| `IPlatformInfo` | «¿Esta función existe acá?», para esconder o avisar | — | — |

**Una función que no existe en Mac nunca falla en silencio.** Su servicio
responde «no disponible» con un texto en español y la pantalla esconde el
botón o lo muestra con la explicación (sección 12).

## 6. Fases y tandas

Cada tanda es una sesión propia, lanzada con su encargo. Yo reviso el diff y
las pruebas de cada una antes de fusionar. Los encargos de las fases 0 y 1 ya
están escritos. Los de las fases 2 en adelante los escribo al pasar la puerta
anterior, porque dependen de lo que se aprenda.

**Rama de trabajo:** `mac`, cortada de `main` después de publicar la 1.4.0.
Cada tanda sale de `mac` y vuelve a `mac`. `mac` pasa a `main` en cada puerta,
con tu visto bueno. La tanda 1 es la excepción: sale ya de `main`, porque
sólo agrega una carpeta nueva.

### Convivir con lo que está abierto

| Qué | Dónde está | Qué se hace |
|---|---|---|
| La 1.4.0 (52 cambios) | `claude/determined-mahavira-fd2c61` | **Se publica primero.** Antes, esconder «Seguir la canción» (sección 19) |
| Seguir la canción (beta) | Dentro de la 1.4.0 | Escondido salvo que exista un archivo. En la fase 1 queda detrás de `ISoundInput` e `ISpeechEar`. Sólo Windows |
| YouVersion (en pausa) | `claude/youversion-integracion`, sin fusionar | Sigue en pausa. Si se retoma durante las fases 1 a 3, espera: su pantalla se hace una sola vez en la interfaz nueva |
| Partir la ventana principal | No encontré la rama | Si existe, entra a `main` antes de la tanda 4. Si no, se parte al migrar (tandas de la fase 3) |
| Página web, campaña, métricas | Ramas de `sitio/` | No chocan: son otros archivos |

### Fase 0 — Saber si el camino sirve (no toca el programa)

| Tanda | Objetivo | Modelo | Tam. | Depende de | Riesgo |
|---|---|---|---|---|---|
| **1** | Prototipo en Avalonia + compilación para Mac en GitHub | Opus | M–G | Nada | Alto (para eso está) |
| **2** | Pruebas de Core en Mac dentro de GitHub | Sonnet | C | Nada | Bajo |
| **3** | Pasar todo de .NET 8 a .NET 10 | Sonnet | M | 1.4.0 publicada | Medio: toca Windows |

- **Terminado 1:** los seis obligatorios de la sección 4, en la Mac real.
- **Terminado 2:** las pruebas pasan en Windows y en Mac en cada cambio.
- **Terminado 3:** compila sin advertencias, pruebas en verde, y vos probás un
  instalador de prueba en tu PC como cualquier versión.
- **Sin Mac:** compilar, pruebas, GitHub. **Con Mac:** tu primera visita.

**Puerta 1:** ¿el prototipo cumplió? Si no, se frena acá.

### Fase 1 — Separar lo común (Windows queda igual; es reordenar)

| Tanda | Objetivo | Modelo | Tam. | Depende de |
|---|---|---|---|---|
| **4** | Crear `Presentation` y `Platform.Windows`, las interfaces de plataforma, y mudar los servicios que no dibujan | Opus | G | Puerta 1, tanda 3 |
| **5** | Mudar los ViewModels a `Presentation`, de a una parte, sacándoles WPF | Sonnet | G (2 o 3 sesiones en fila) | 4 |
| **6** | Banco de render de WPF y juego de casos de referencia de la letra | Sonnet | M | 4 |
| **7** | Mudar al ViewModel la lógica que hoy está en el código de las ventanas | Sonnet | M–G (2 sesiones) | 5 |
| **8** | Revisión cruzada de la fase | Opus | C | 4 a 7 |

- **Terminado:** `Presentation` compila sin WPF; `App` lo usa y hace lo mismo
  que antes; las pruebas de Core siguen en verde y hay pruebas nuevas de
  ViewModels que corren en Windows y en Mac; el banco guarda las imágenes de
  referencia.
- **Sin Mac:** todo. **Con Mac:** nada.
- **Riesgo:** medio. No cambia nada a propósito, pero mueve mucho código. Por
  eso termina con **un instalador de prueba que probás en la iglesia**: tiene
  que ser igual a la 1.4.0.

**Puerta 2:** Windows probado en la iglesia sin diferencias. Se puede publicar
como versión normal.

### Fase 2 — El corazón: la salida

| Tanda | Objetivo | Modelo | Tam. |
|---|---|---|---|
| **9** | Proyecto `Desktop` (Avalonia): `SlideView`, `ProjectedView`, ajuste de la letra, fuentes y sus reemplazos | Opus | G |
| **10** | Video (`VlcVideoSurface`), fundidos, logo en video, relleno | Opus | M–G |
| **11** | Ventana de salida y de escenario: pantalla completa, cambio de pantallas, no dormir | Opus | M |
| **12** | Banco de render de Avalonia y comparador con el de WPF | Sonnet | M |
| **13** | Ajustar hasta cumplir la igualdad; revisión cruzada | Opus | M |

- **Terminado:** los criterios de igualdad de la sección 7.2, y los seis
  puntos del prototipo otra vez, ahora con el código de verdad.
- **Sin Mac:** bancos de render en Windows y en GitHub (Windows y Mac).
  **Con Mac:** tu segunda visita, con proyector.

**Puerta 3:** la letra se ve igual y la salida funciona en la Mac real.

### Fase 3 — La ventana del operador y los diálogos

| Tanda | Objetivo | Modelo | Tam. |
|---|---|---|---|
| **14** | Estilos generales (`App.xaml`), menú, y la guía de traducción WPF → Avalonia con ejemplos reales | Opus | M–G |
| **15** | Diálogos simples (12): elegir, texto, aviso, acerca de, carga, actualización, comentarios, texto rápido, cuenta regresiva, transiciones, copia liviana, bajar de YouTube | Sonnet | M |
| **16** | Biblia y canciones (9): importar Biblia, paquetes, ayuda de formato, editor, importar canciones, buscador, papelera, buscar letra, ajustes | Sonnet | M–G |
| **17** | Diseño (7): temas, diseñador de canción, colores, logos, propiedades de un medio, reglas de pestañas, diseñador de escenario | Sonnet | G (2 sesiones) |
| **18** | Remoto y transmisión (4): control remoto, transmisión, estilos, Pixabay | Sonnet | M |
| **19** | Ventana principal, parte 1: armazón, barra, PREVIEW y LIVE, atajos | Sonnet | G |
| **20** | Ventana principal, parte 2: grilla de diapositivas, playlist, arrastrar | Sonnet | G |
| **21** | Ventana principal, parte 3: Biblia, medios, audio, recorrido de bienvenida | Sonnet | G |
| **22** | Cosas de Mac: menú global, Cmd, cerrar y salir, una sola copia, soltar desde Finder | Opus | M |
| **23** | Revisión cruzada de toda la fase | Opus | M |

- **Orden:** 14 primero; 15 a 21 en fila, **un solo Sonnet por vez**; 22 y 23
  al final.
- **Terminado de cada tanda:** compila sin advertencias en Windows y en Mac;
  cada pantalla tiene una prueba que la dibuja en memoria y guarda la imagen;
  yo comparo cada imagen con la de WPF; ninguna pantalla nueva tiene lógica
  que no esté en su ViewModel.
- **Con Mac:** tu tercera visita (un culto entero de mentira) y los probadores.
- **Riesgo:** medio. Mucho volumen, poca dificultad. El peligro es el detalle
  que se pierde; por eso la comparación imagen por imagen.

**Puerta 4:** el programa entero funciona en Mac, con las faltas avisadas.

### Fase 4 — Empaquetar y publicar la beta de Mac

| Tanda | Objetivo | Modelo | Tam. |
|---|---|---|---|
| **24** | El `.app`, el ícono, el `.dmg`, LibVLC adentro, `release.yml` | Opus | M–G |
| **25** | Actualización desde adentro del programa en Mac | Opus | M |
| **26** | Página: botón según el sistema, guía «Cómo abrirlo en Mac», conteo por sistema | Sonnet | M |
| **27** | README, guía de uso, privacidad, pendientes, CHANGELOG, textos | Haiku | C |

**Puerta 5:** beta de Mac publicada, con el mismo número que Windows.

### Fase 5 — Igualar Windows y retirar WPF

| Tanda | Objetivo | Modelo | Tam. |
|---|---|---|---|
| **28** | Panel web en la interfaz nueva de Windows (WebView2 dibujando en una imagen) | Opus | **G** |
| **29** | YouTube, navegador de YouTube y búsqueda de musica.com en la interfaz nueva de Windows | Opus | M |
| **30** | Conectar lo de Windows: captura de ventanas, lo que suena, teclas, miniaturas, seguir la canción | Sonnet | M |
| **31** | Corrector de ortografía con Hunspell, para los dos sistemas | Opus | M |
| **32** | Medir el rendimiento de las dos interfaces en tu notebook | Sonnet | C |
| **33** | Instalador con las dos interfaces y un acceso «interfaz anterior» de respaldo | Sonnet | C |
| **34** | Borrar el proyecto WPF, una versión después | Haiku | C |

- **Terminado:** lista de igualdad completa (cada función de la 1.4.0 marcada
  «igual» o «mejor»); el video no rinde peor (ni 10 % más de procesador ni más
  cuadros perdidos); y **dos domingos tuyos en la iglesia** con la interfaz
  nueva sin volver a la vieja.
- **Riesgo:** alto en la 28: es la única pieza donde hoy WPF tiene algo que
  Avalonia no trae hecho. Plan B en la sección 16.

**Puerta 6:** Windows pasa a la interfaz nueva. La vieja queda una versión más
como respaldo y después se borra.

### Fase 6 — Lo que quedó afuera en Mac

Sin orden fijo: se ordena por lo que más pidan. Cada una es una o dos
sesiones de Opus y una visita tuya a la Mac.

| Función | Camino | Tam. |
|---|---|---|
| YouTube en vivo y panel web | WebKit de Mac en una ventana pegada a la salida | G |
| PowerPoint | Pedirle el PDF a Keynote o a PowerPoint de Mac | M |
| Capturar otra ventana | ScreenCaptureKit, con un ayudante nativo chico | G |
| Control de Spotify | AppleScript | M |
| Seguir la canción | Entrada de sonido de Mac + Whisper | G |
| Versión para Mac con Intel | Segunda descarga; necesita un probador con Intel | C |

## 7. Pruebas

### 7.1 Pruebas automáticas en Mac, dentro de GitHub

- **Qué Mac hay** (verificado el 15/11): con chip Apple, `macos-15` y
  `macos-26` (3 núcleos, 7 GB); con Intel, `macos-15-intel` y
  `macos-26-intel`. Para proyectos públicos son **gratis y sin límite**. (En
  un proyecto privado costarían 0,062 dólares por minuto.)
- **Qué se corre:** en cada cambio, compilar y las pruebas de `Core` y `Data`
  en Windows y en `macos-15` (chip Apple). Desde la fase 1, también las de
  ViewModels. Desde la fase 2, el banco de render de Avalonia en los dos.
- **Intel:** no se corre en cada cambio. Cuando exista la versión Intel, una
  corrida en `macos-15-intel` antes de cada publicación.
- **Qué versión de macOS:** `macos-15`, la más vieja que vamos a aceptar. Si
  funciona ahí, funciona en las más nuevas.

### 7.2 Que la letra proyectada se vea igual

El motor que mide y dibuja el texto es distinto en WPF y en Avalonia. Una
letra unos pixeles más ancha hace que un renglón corte en otra palabra.

**Cómo se compara.** Un juego fijo de unas 60 diapositivas de referencia
(`tests/casos-de-render/`): renglones cortos y largos, dos versiones de
Biblia, franjas, contorno, sombra, fondo desenfocado, mayúsculas, acentos y
eñes, cuenta regresiva, anuncio, logo, escenario. Cada una en 3 temas.

- **Banco de WPF** (proyecto `WinExe`, sin consola y sin mostrar ventana):
  dibuja cada caso a 1920×1080, guarda la imagen y un archivo con los datos
  (tamaño de letra elegido, dónde cortó cada renglón, rectángulo del texto).
- **Banco de Avalonia** (`Avalonia.Headless`, dentro de las pruebas, sin
  ventana): lo mismo. Corre en tu PC y en GitHub, en Windows y en Mac.
- **Comparador:** lee los dos juegos y arma un informe con las imágenes lado a
  lado para las que no pasan.

**Criterio de «igual»:**

| Qué | Tolerancia |
|---|---|
| Cantidad de renglones y palabra donde corta cada uno | **Idéntico** en todos los casos |
| Tamaño de letra elegido | Diferencia de 2 % como mucho |
| Posición y tamaño del bloque de texto | Hasta 1 % de la pantalla (unos 19 pixeles) |
| Contorno, sombra, franjas | A la vista (reviso yo las imágenes) y diferencia promedio de color menor a 3 % en la zona del texto |
| Fondo desenfocado y fundidos | A la vista; no se exige pixel por pixel |

Entre Windows y Mac **con la interfaz nueva**, usando una fuente que viene con
el programa: los mismos cortes y el mismo tamaño, exactos.

**Si no llega a «idéntico»:** el ajuste pasa a medir con el motor nuevo en las
dos interfaces (es la misma fórmula, sección 5.1), y a las canciones donde
cambie el corte se les avisa una vez al abrirlas.

### 7.3 Lo que sólo se prueba en una Mac real

Pantalla completa sobre la barra de menú y el Dock. Proyector extendido, en
espejo, desenchufar y enchufar. Sonido por HDMI. Pantalla Retina junto a un
proyector común. El aviso de «Abrir igualmente». El firewall y el celular.
Teclas F1 a F4 con y sin Fn. Arrastrar desde Finder. Que no se frene con otro
programa adelante. Que no se duerma en un culto de dos horas. Rendimiento con
video 1080p y 4K.

### 7.4 Probadores

- **Quiénes:** de 3 a 5 personas de las que la piden en Instagram y TikTok,
  que proyecten en su iglesia con una Mac. Mejor con Mac distintas (una M1
  básica, una más nueva; después alguna Intel).
- **Cómo se consiguen:** una historia y un video corto: «¿Proyectás con Mac en
  tu iglesia? Buscamos 5 personas para probar EcclesiaCast antes que nadie».
  Se anotan en un formulario (qué Mac, qué macOS, qué programa usan hoy).
- **Qué reciben:** una página (`sitio/probar-en-mac.html`) con cuatro pasos y
  capturas: bajar, arrastrar a Aplicaciones, «Abrir igualmente», conectar el
  proyector como pantalla separada. Y una lista corta de qué probar.
- **Cómo cuentan las fallas:** la ventana **Comentarios** del programa (ya
  existe, manda a un formulario de Google), que en Mac agrega sola el modelo,
  el chip, la versión de macOS y la del programa. Y en **Ayuda → «Abrir la
  carpeta de registros»** para adjuntar el registro.
- **Regla:** una falla que no se puede repetir con el registro no se arregla a
  ciegas; se le pide a esa persona un video del celular.

## 8. Empaquetado y distribución

- **Formato: `.dmg`** (una ventana donde se arrastra EcclesiaCast a
  Aplicaciones). Es lo que espera cualquier persona con Mac. Un `.pkg` sin
  firma es más difícil de habilitar; un `.zip` suelto deja el programa en
  Descargas, donde macOS lo corre desde una carpeta escondida y no se puede
  actualizar.
- **Dos descargas, no una universal:** «Mac con chip Apple» (primero) y «Mac
  con Intel» (después, si aparece quien la pruebe). Una universal pesa el
  doble y .NET no la arma sola.
- **Todo adentro** (self-contained): .NET, Avalonia y LibVLC van dentro del
  `.app`. No se instala nada más. Tamaño estimado: 110 a 150 MB (el instalador
  de Windows pesa 142). Las 4 Biblias incluidas (5 MB) van adentro; los fondos
  (22 MB) también.
- **Ícono `.icns`:** se genera del mismo dibujo que el `.ico`, con un paso
  nuevo junto a `installer/make-icon.ps1`. Hay que mirar que el dibujo quede
  bien en el «cuadrado redondeado» de Mac.
- **Identificador del paquete:** `org.ecclesiacast.app`. No se cambia nunca
  más: macOS ata a él los permisos y las preferencias.
- **Versión mínima:** macOS 15 (Sequoia). La marca .NET 10 (verificado: macOS
  15, 26 y 27; el 14 quedó sin soporte en septiembre de 2026).
- **`release.yml`:** un trabajo nuevo en `macos-15`: pruebas, publicar para
  `osx-arm64`, armar el `.app`, firma local sin cuenta (sección 9), crear el
  `.dmg` y un `.zip` para la actualización, y subirlos a la misma publicación
  que el instalador de Windows. Nombres:
  `EcclesiaCast-X.Y.Z-mac-chip-apple.dmg` y `…-mac-chip-apple.zip`.
- **Página web:**
  - El botón Descargar cambia según el sistema del visitante: «Descargar para
    Windows» o «Descargar para Mac (beta)». Siempre con un enlace chico al
    otro sistema.
  - El navegador **no puede saber** si una Mac es chip Apple o Intel. Mientras
    exista una sola, el botón va directo; cuando haya dos, se muestran las dos
    con «¿Cuál es la mía? Menú  → Acerca de esta Mac».
  - Desde el celular (70 % de las visitas) no se sabe qué computadora tiene:
    se muestran los dos sistemas.
  - Guía nueva `sitio/abrir-en-mac.html`, con capturas del «Abrir igualmente».
  - Requisitos: «macOS 15 o más nuevo, Mac con chip Apple (2020 en adelante)».
  - Una tabla honesta de qué no tiene todavía la versión de Mac.
- **GoatCounter:** el clic de hoy se llama `descargar`. Pasa a
  `descargar-windows` y `descargar-mac-chip-apple` (después
  `descargar-mac-intel`), más `guia-abrir-en-mac`. Se anota en la memoria de
  métricas que desde ese día el total es la suma.

## 9. Firma y notarización de Apple

**Decisión de Alexis (15/11): no pagar.** El plan queda armado sin firma.
Esto es lo que significa, para que sea a conciencia:

- **Qué cuesta la cuenta** (verificado en la página de Apple el 15/11): 99
  dólares por año. Hay exención sólo para organizaciones sin fines de lucro,
  escuelas y gobiernos con personería; **no para personas**. No pude confirmar
  si alguien de Argentina puede anotarse como persona (Apple no publica la
  lista de países).
- **Qué pasa sin firma:** la primera vez que alguien abre el programa bajado
  de internet, macOS dice que no lo puede verificar y **no lo abre**. Hay que
  ir a **Configuración del Sistema → Privacidad y seguridad**, bajar, y tocar
  **«Abrir igualmente»** (así lo escribe Apple en español); después confirmar
  con **«Abrir»** y la clave de la Mac. Es una sola vez. El truco viejo de
  clic derecho → Abrir no figura más en la ayuda de Apple; la guía no lo usa.
- **Qué hacemos igual:** una **firma local** (*ad-hoc*: gratis, sin cuenta,
  la hace GitHub al armar el paquete). No evita el aviso, pero las Mac con
  chip Apple se niegan a ejecutar algo que no tenga al menos eso.
- **Tres consecuencias de no pagar:**
  1. **La actualización desde adentro importa mucho más** (sección 10): lo que
     baja el propio programa no pasa por el aviso; lo que se baja con el
     navegador sí, cada vez.
  2. **Los permisos se vuelven a pedir en cada versión.** macOS recuerda los
     permisos (micrófono, grabación de pantalla) atados a la firma; con firma
     local cada versión es «otro programa». No afecta a la primera versión
     (no usa ninguno), pero sí a capturar ventanas y seguir la canción.
  3. **El firewall de macOS**, si alguien lo tiene prendido (viene apagado),
     puede volver a preguntar después de cada actualización.
- **Riesgo que no manejamos:** Apple complica el «Abrir igualmente» un poco
  más cada año. Si un día lo saca, sin cuenta no hay versión de Mac. Señal
  para volver a pensarlo: que los probadores no logren abrirlo con la guía.
- **Si algún día se paga**, esto es lo que hay que hacer (queda escrito para
  no investigar de nuevo): certificado «Developer ID Application»; firmar con
  «hardened runtime» todas las librerías de adentro, **incluidas las de
  LibVLC**; permisos (*entitlements*) que necesita .NET:
  `com.apple.security.cs.allow-jit` y, si LibVLC queda sin firmar con el mismo
  certificado, `com.apple.security.cs.disable-library-validation`; más
  `com.apple.security.device.audio-input` para el micrófono; enviar a
  notarizar desde GitHub (`notarytool`) y «abrochar» el resultado al `.dmg`
  (`stapler`). Una sesión de Opus.

## 10. Actualización automática

Hoy: busca en GitHub, baja `EcclesiaCast-X.Y.Z-setup.exe` y lo ejecuta. La
búsqueda y la elección (`Core/Updates`) sirven igual. Cambia el final:

- **Qué archivo elige:** `UpdatePicker` aprende a elegir por sistema: el
  `-setup.exe` en Windows, el `-mac-chip-apple.zip` en Mac.
- **Beta de Mac, primera entrega:** avisa que hay versión nueva y el botón
  abre la página de descarga. Simple y seguro.
- **Beta de Mac, segunda entrega (tanda 25):** se actualiza solo. Baja el
  `.zip`, comprueba que esté entero (la huella SHA-256 que publica GitHub), lo
  descomprime al lado, y al cerrar un ayudante chico cambia el `.app` viejo
  por el nuevo y lo vuelve a abrir. No pide «Abrir igualmente» otra vez.
- **Casos que tiene que manejar:** el programa abierto desde Descargas o desde
  el `.dmg` sin copiarlo a Aplicaciones (macOS lo corre desde una carpeta de
  sólo lectura: ahí no se puede cambiar; se explica y se manda a la guía);
  falta de permiso para escribir en Aplicaciones; quedarse sin luz en el medio
  (el viejo no se borra hasta que el nuevo esté completo).
- **Como en Windows:** nunca actualiza mientras se proyecta, y se puede saltear
  una versión.
- **No se usa Sparkle** (el actualizador típico de Mac): pide firma propia y
  una pieza nativa más, y lo nuestro es poco código que ya está a medias.

## 11. Datos y rutas

- **Carpeta de datos:** `~/Library/Application Support/EcclesiaCast` (base,
  registros, Biblias, documentos, modelos, herramientas). Sale de la misma
  línea de hoy. `YtDlp.ToolsFolder` y `SpeechEar` arman su ruta aparte: pasan
  todos por `IPlatformPaths`.
- **Videos bajados:** `~/Movies/EcclesiaCast`.
- **Lo que viene con el programa** (Biblias, fondos): hoy se busca al lado del
  `.exe`; en Mac está en `EcclesiaCast.app/Contents/Resources`.
- **La variable `ECCLESIACAST_DATA_DIR`** (para pruebas) sigue funcionando.
- **Copias de seguridad:** es el archivo de la base (SQLite), que es igual en
  los dos sistemas. Una copia hecha en Windows **se restaura en Mac y al
  revés**: canciones, Biblias, temas, playlists y ajustes pasan enteros.
- **Lo que no pasa solo: los medios.** La base guarda la ruta completa de cada
  video, imagen y audio (`C:\Users\…`), y esos archivos no están dentro de la
  copia. Para eso:
  1. Un ayudante de rutas en Core que entienda `\` y `/` y compare sin
     distinguir mayúsculas.
  2. Lo que vive dentro de la carpeta de datos (páginas de PDF, fondos
     incluidos) se guarda con ruta relativa, así viaja.
  3. Pantalla **«Medios que no se encuentran»**: al abrir una biblioteca
     traída de otra computadora, ofrece elegir la carpeta donde están ahora y
     los vuelve a enlazar por nombre.
  4. Más adelante, si lo piden: «Exportar la biblioteca completa», con los
     medios adentro.
- **Paquetes `.ecbiblias`:** son un archivo común; pasan de un sistema al otro
  sin cambios. Se agrega una prueba en Mac.
- **Mayúsculas:** el disco de Mac, como el de Windows, no distingue mayúsculas
  salvo que alguien lo haya formateado distinto. El código nuevo no depende de
  eso.
- **Fuentes.** Hay que resolver tres cosas:
  1. **Los temas de fábrica usan «Segoe UI», que no existe en Mac.** Se
     incluye en el programa **Selawik**, la fuente libre que Microsoft hizo con
     las mismas medidas que Segoe UI (licencia OFL). Donde un tema dice
     «Segoe UI» y no está, se usa Selawik: mismos cortes de renglón.
  2. **Fuentes de EcclesiaCast:** incluir un juego chico de fuentes libres
     (Selawik, Anton, y dos o tres más para títulos) que estén **en los dos
     sistemas**, y marcarlas en la lista. Un tema hecho con ésas se ve igual
     en cualquier computadora.
  3. **Tabla de reemplazos** para las de Windows que Mac no trae (Calibri →
     Carlito, que tiene sus medidas; Consolas y Cascadia → Menlo). Arial,
     Impact, Verdana, Tahoma, Georgia y Times New Roman sí vienen en Mac. Si un
     tema usa una fuente que no está, el editor lo dice.

## 12. Funciones que cambian o faltan en Mac, y cómo se avisa

**Regla de honestidad:** nada falla callado, nada se promete. Tres lugares:

1. **En la página:** una tabla «Qué tiene la versión de Mac» al lado del botón
   de descarga, con ✔, «todavía no» y «distinto».
2. **En el programa:** lo que no existe en Mac **no aparece en los menús**. Lo
   que está a medias aparece con una explicación corta y qué hacer en cambio.
3. **En Ayuda → «Qué falta en Mac»:** la misma tabla, con la versión.

| Función | En Mac | Qué ve la persona |
|---|---|---|
| PowerPoint | No se puede manejar PowerPoint como en Windows | Al elegir un `.pptx`: «En Mac hay que pasarlo a PDF: en PowerPoint o Keynote, Archivo → Exportar → PDF, y cargá ese archivo» |
| PDF | Funciona, con otro motor (PDFium) | Nada distinto |
| Capturar otra ventana | Todavía no | No aparece. Cuando esté: pide el permiso de «Grabación de pantalla» y se explica con capturas |
| Panel web 🌐 | Todavía no | No aparece |
| YouTube | Sólo descargado | «Bajar de YouTube» funciona igual; «Reproducir directo» no aparece |
| yt-dlp y deno | Se bajan solos, en su versión de Mac | Nada distinto |
| ffmpeg (GIF animados, copias livianas) | Se usa si la Mac lo tiene (Homebrew); el programa no lo instala, igual que en Windows | Si no está: el GIF queda fijo y se explica |
| Buscador de letras | Búsqueda simple y LRCLIB | La búsqueda «por una frase» no aparece |
| Teclas multimedia y Spotify | Todavía no | La pestaña no aparece |
| Seguir la canción | Todavía no | No aparece |
| Corrector de ortografía | Todavía no | No hay subrayado rojo |
| Importar de ProPresenter | Funciona; busca en las carpetas de Mac | Nada distinto (importante: ProPresenter se usa mucho en Mac) |
| Importar de EasyWorship | EasyWorship no existe en Mac | «Copiá la carpeta desde la PC con Windows y elegila acá» |
| Barra de título oscura | La maneja macOS | Nada |
| Modo PC de bajo rendimiento | Sólo desde Ajustes | Nada |
| Sonido y micrófono | El sonido del video sale por la salida elegida en macOS. El micrófono no se usa todavía | Nada |
| Atajos | Cmd en vez de Ctrl; F1–F4 y además ⌘1–⌘4 | Los textos de los botones dicen el atajo de cada sistema |

## 13. Cosas propias de macOS que se escapan fácil

| Tema | Qué pasa | Qué hacemos |
|---|---|---|
| **Pantalla completa** | La pantalla completa «de Mac» crea un escritorio aparte, con animación, y se sale sola al cambiar de programa | **No se usa.** Ventana sin borde del tamaño exacto de la pantalla, puesta por encima de la barra de menú y el Dock (nivel de ventana, llamada nativa) |
| **Spaces** (varios escritorios) | Al cambiar de escritorio, una ventana común desaparece | La salida se marca para estar en todos los escritorios y quedarse quieta |
| **Barra de menú en el segundo monitor** | Con «Las pantallas tienen Spaces separados» (viene prendido) cada pantalla tiene barra | El nivel de la ventana la tapa. Punto P2 del prototipo |
| **Proyector en espejo** | macOS informa una sola pantalla | Igual que con un solo monitor: la salida tapa al operador y Esc vuelve. La guía recomienda «pantalla separada» y muestra dónde se cambia |
| **Enchufar y desenchufar** | La lista de pantallas cambia en caliente | Se escucha el aviso del sistema: si se va la pantalla de salida, se cierra prolijo y se avisa; al volver, se ofrece proyectar de nuevo |
| **Retina** | Una Mac trabaja en «puntos», no en pixeles, y cada pantalla tiene su escala | `DisplayInfo` hoy guarda pixeles: se le agrega la escala. La diapositiva se dibuja siempre a 1920×1080 virtual y se escala |
| **F1 a F4** | En las notebooks Mac son brillo y Mission Control; piden Fn | Se suman **⌘1 a ⌘4**. F1–F4 siguen funcionando para quien los tenga libres |
| **Ctrl o Cmd** | En Mac todo es con ⌘ | Los atajos se definen una vez como «tecla de comando»: Ctrl en Windows, ⌘ en Mac. Ctrl+, pasa a ⌘, (que en Mac es justo el de Ajustes) |
| **Menú global** | El menú va arriba de la pantalla, no en la ventana | El mismo menú se muestra arriba en Mac y en la ventana en Windows. «Acerca de», «Ajustes…» y «Salir» van donde Mac los espera |
| **Cerrar no es salir** | En Mac muchos programas siguen abiertos sin ventana | Para un programa de proyección es peligroso: **cerrar la ventana principal sale** (con la pregunta de siempre si está proyectando). ⌘Q, igual |
| **Una sola copia abierta** | Hoy Windows no lo impide | Un archivo de traba en la carpeta de datos, para los dos sistemas: si ya hay una abierta, la trae al frente |
| **Firewall** | Viene apagado. Prendido, pregunta si se permiten conexiones entrantes | La guía muestra el cartel y qué tocar. Sin firma puede volver a preguntar en cada versión |
| **Permiso de «Red local»** | Verificado en la nota técnica de Apple (TN3179): **recibir** conexiones **no lo pide**; lo pide iniciar conexiones hacia la red. El programa sólo recibe (celular, OBS) | No debería aparecer. Se confirma en el prototipo (P5) |
| **App Nap** | macOS frena los programas que no están adelante | Se le avisa que hay una actividad en curso mientras se proyecta |
| **Que no se duerma** | Un culto de dos horas sin tocar la Mac apaga la pantalla | Mientras la salida está prendida se le pide al sistema que no duerma (`caffeinate`, viene con macOS) |
| **Versión mínima** | .NET 10: macOS 15. Avalonia 12: prueba a fondo en macOS 26, «mejor esfuerzo» en 14 y 15 | Mínimo macOS 15. Se prueba en 15 (GitHub) y en lo que tenga la Mac prestada |
| **Rosetta** | Apple la está retirando | No se depende de ella: la versión de chip Apple es nativa |
| **Tamaño** | Ver sección 8 | 110–150 MB |
| **Arrastrar desde Finder** | Funciona distinto por dentro | Avalonia lo trae; se prueba en la Mac real |
| **Fuentes de la interfaz** | Segoe UI y Cascadia no existen | La interfaz usa la fuente del sistema de cada uno (Segoe UI en Windows, San Francisco en Mac). Lo proyectado, sección 11 |
| **Emojis de los botones** | Los de Windows y los de Apple son dibujos distintos, con otro tamaño | En la fase 3 se revisan uno por uno. Si descuadran, se cambian por íconos dibujados, iguales en los dos |
| **Sombras y desenfoque** | Avalonia tiene los dos (como WPF, uno por elemento) | El contorno de la letra hoy es una sombra «halo»: lo cubre el banco. Plan B: dibujar el contorno como trazo |
| **Animaciones** | Las 65 están en código, con la forma de WPF | Ayudante único (sección 5.1) |
| **Triggers** (439) | Avalonia no tiene; usa estilos con selectores | Guía de traducción con ejemplos (tanda 14) |
| **Plantillas** (160) | Se escriben distinto | Se parte del tema Fluent de Avalonia y se reescriben las propias |
| **DataGrid** | No se usa | Nada |
| **«Oculto pero ocupando lugar»** | Avalonia sólo tiene visible o no visible | Donde importa, se usa transparente y sin clics |
| **Tooltips de 30 segundos** | En WPF hubo que alargarlos | En Avalonia quedan mientras el mouse esté encima |
| **Carpetas protegidas** | macOS pregunta antes de dejar leer Documentos, Escritorio y Descargas | Aparece al importar de ProPresenter. La guía lo muestra |

## 14. Licencias

EcclesiaCast es GPL-3.0. Lo que se suma:

| Pieza | Licencia | ¿Compatible? |
|---|---|---|
| Avalonia, Avalonia.Controls.WebView, Avalonia.Headless | MIT | Sí |
| SkiaSharp, HarfBuzz (vienen con Avalonia) | MIT | Sí |
| LibVLC y LibVLCSharp | LGPL-2.1 o posterior; algunos complementos de VLC, GPL-2 o posterior | Sí |
| PDFium | BSD y Apache 2.0 | Sí |
| Hunspell (versión para .NET) y diccionario de español | MIT/MPL; el diccionario, GPL/LGPL/MPL | Sí |
| Selawik, Anton, Carlito | OFL (fuentes libres) | Sí; hay que incluir su licencia |
| WebKit de macOS, WebView2 de Windows | Son del sistema | No se distribuyen |
| yt-dlp, deno | Los baja el programa; no se distribuyen | — |

Qué hay que hacer:

- **Hoy no existe un archivo de avisos de terceros.** Crear
  `THIRD-PARTY-NOTICES.md` con cada pieza, su licencia y dónde está su código.
  Va en el repositorio, dentro del instalador y dentro del `.app`.
- **En el programa:** «Acerca de» → «Licencias de terceros» lo muestra.
- **En la página:** en el pie, «Software libre (GPL-3.0). Usa Avalonia, LibVLC
  y otras piezas libres», con enlace al archivo y al código.
- **LibVLC en Mac:** dentro del `.app` va el texto de la LGPL y de dónde
  salieron las librerías (qué versión de VLC).
- **Apple:** la GPL no se lleva con la tienda de Apple (Mac App Store). No nos
  afecta: se distribuye desde la página.

## 15. Documentación

| Archivo | Qué cambia | Cuándo |
|---|---|---|
| `README.md` | Windows y Mac; cómo compilar para cada uno; estado «beta» de Mac | Fase 4 |
| `docs/guia-de-uso.md` | Atajos de los dos sistemas; sección «En Mac» (instalar, abrir, conectar el proyector, qué falta) | Fase 4 |
| `docs/privacidad.md` y `sitio/privacidad.html` | La carpeta de datos en Mac; que en Mac no se usa micrófono ni grabación de pantalla (hasta que existan) | Fase 4 |
| `docs/pendientes.md` | §4: enlace a este plan y estado. Cada puerta, una línea | Desde ya |
| `CHANGELOG.md` | Cada tanda anota su línea en «Sin publicar». Lo que no cambia nada para quien usa el programa (fase 1) va como una sola línea al final | Siempre |
| `CONTRIBUTING.md` | La estructura nueva; la regla de que `Presentation` no conoce pantallas | Fase 1 |
| `docs/plan-mac-traduccion.md` | Guía WPF → Avalonia para Sonnet, con ejemplos del propio código | Tanda 14 |
| `docs/pruebas-mac-N.md` | La lista de cada visita tuya a la Mac | Antes de cada visita |
| `sitio/abrir-en-mac.html`, `sitio/probar-en-mac.html` | Guías con capturas | Fases 3 y 4 |
| `THIRD-PARTY-NOTICES.md` | Nuevo | Fase 4 |

## 16. Riesgos y plan B

| Pieza | Riesgo | Plan B | Plan C |
|---|---|---|---|
| **Video** | El paquete oficial de LibVLC para Mac no trae chip Apple (no pude confirmarlo) | Tomar las librerías del VLC oficial para chip Apple al armar el paquete | Otro reproductor para Mac (el del sistema, con un ayudante nativo) |
| **Video** | Copiar cada cuadro a una imagen rinde mal en Mac | Bajar a 720p como hoy en «bajo rendimiento»; pintar directo en la tarjeta de video | — |
| **Ventana de salida** | No tapa la barra de menú o el Dock, o roba el foco | Subir el nivel de la ventana con llamada nativa | Pantalla completa propia de macOS, aceptando la animación |
| **Letra** | Los renglones cortan distinto que en WPF | Medir con el motor nuevo en las dos interfaces | Aceptar diferencias y avisar por canción |
| **Web en Windows** (tanda 28) | El control de web de Avalonia no dibuja en una imagen, y hoy el panel web depende de eso | Armarlo con las piezas de WebView2 y la captura que ya tenemos en `WindowCapture` | Dejar el panel web como ventana nativa (se pierde escalarlo y cubrirlo); o **no retirar WPF** hasta resolverlo |
| **Rendimiento en Windows** | La interfaz nueva rinde peor en tu notebook | Medir temprano (tanda 32 se puede adelantar a la fase 2) | No retirar WPF |
| **Sin firma** | Apple cierra el «Abrir igualmente» | Pagar los 99 dólares | Pausar la versión de Mac |
| **Volumen** (fase 3) | Detalles perdidos al pasar 48 pantallas | Comparación imagen por imagen | — |
| **Probar poco** | La Mac es prestada | Probadores con sus propias Mac | — |

**Cuándo parar:**

- **Puerta 1:** si en 3 sesiones de Opus el prototipo no cumple los 6
  obligatorios en la Mac real, se pausa. No se tocó el programa.
- **Puerta 3:** si la letra no llega a «idéntico en los cortes» ni con el plan
  B, se decide con vos si se aceptan diferencias avisadas o se frena.
- **Puerta 6:** si el panel web o el rendimiento no igualan a WPF, **Windows
  se queda en WPF** el tiempo que haga falta y Mac sigue con Avalonia. Es el
  peor caso: dos interfaces más tiempo. No rompe nada.

Lo que **nunca** pasa en este plan: que una iglesia que hoy usa Windows reciba
una versión peor. Cada cambio que le llega a Windows pasa antes por tu prueba.

## 17. Costos y tiempos

**Sesiones** (S chica, M mediana, L grande):

| Fase | Opus | Sonnet | Haiku | Fable (revisión) | Total |
|---|---|---|---|---|---|
| 0 Prototipo y preparación | 2 (M–L) | 2 (S, M) | — | 1 | 5 |
| 1 Separar lo común | 2 (L, S) | 5 (L, M, M) | — | 1 | 8 |
| 2 La salida | 4 (L, M, M, M) | 1 (M) | — | 1 | 6 |
| 3 Operador y diálogos | 3 (M, M, M) | 9 (M a L) | — | 2 | 14 |
| 4 Empaquetar y beta | 2 (M) | 1 (M) | 1 | 1 | 5 |
| 5 Igualar Windows | 3 (L, M, M) | 3 (M, S, S) | 1 | 1 | 8 |
| **Hasta acá** | **16** | **21** | **2** | **7** | **46** |
| 6 Lo que quedó afuera | 1 o 2 por función | — | — | — | 6 a 10 |

Con suerte son 35; lo honesto es contar 45, y que alguna tanda grande pida
una sesión más. No puedo darte una cifra en tokens: depende de cuánto haya que
releer. Lo que sí la baja: sesión nueva por tanda, encargos que dicen qué
archivos tocar, y un solo Sonnet por vez.

**Calendario.** El ritmo lo ponen tus pruebas, no los modelos:

- Fase 0: dos semanas (una visita a la Mac).
- Fase 1: dos o tres semanas (una prueba en la iglesia).
- Fases 2 y 3: seis a diez semanas (dos visitas a la Mac, probadores).
- Fase 4: dos semanas. **Beta de Mac: entre 3 y 5 meses desde hoy.**
- Fase 5: uno a tres meses más (dos domingos en la iglesia).

**Plata:**

| Qué | Cuánto | ¿Hace falta? |
|---|---|---|
| Cuenta de Apple | 99 dólares por año | No (decidido) |
| Mac de prueba | Prestada | — |
| Adaptador USB-C a HDMI | 10 a 20 dólares | Sólo si la Mac prestada no tiene HDMI |
| Mac de GitHub | Gratis (proyecto público) | — |
| Avalonia, LibVLC, PDFium | Gratis | — |
| Descargas | Gratis (GitHub) | — |

## 18. Qué te voy a pedir y cuándo

**Ahora:**

1. Decime qué Mac te prestan: en esa Mac, menú  → **Acerca de esta Mac**.
   Necesito el **chip** (dice «Apple M1/M2/M3/M4» o «Intel») y la versión de
   **macOS**. Si es Intel o tiene macOS 14 o más viejo, avisame antes de
   seguir: cambia el prototipo.
2. Publicá la 1.4.0 cuando esté lista (con «Seguir la canción» escondido).
3. Decime «lanzá la tanda 1» y arranca el prototipo.

**Visita 1 a la Mac — prototipo (una hora).** Llevás: un cable o adaptador
HDMI, una TV o proyector, tu celular. Te doy `docs/pruebas-mac-1.md` con los
pasos. En resumen: bajar el archivo, abrirlo con «Abrir igualmente» (y sacarle
captura a cada cartel: sirven para la guía), conectar la TV como pantalla
separada, proyectar la canción y el versículo, dejar el video 10 minutos,
manejarlo con el celular, desenchufar y enchufar. Me mandás las fotos y el
archivo de registro.

**Prueba en la iglesia — fin de la fase 1.** Un instalador de prueba de
Windows, como siempre. Tiene que ser igual a la 1.4.0.

**Visita 2 a la Mac — la salida (una hora).** Lo mismo que la visita 1, con el
código de verdad, y fotos de 10 diapositivas para comparar con Windows.

**Conseguir probadores — durante la fase 3.** Una historia en Instagram y
TikTok. Te dejo el texto y el formulario.

**Visita 3 a la Mac — el programa entero (dos horas).** Un culto de mentira:
armar una playlist, canciones, Biblia, video, anuncio, cuenta regresiva,
celular. Y las capturas para la guía «Cómo abrirlo en Mac».

**Publicar la beta — fin de la fase 4.** Vos decidís el número y el día.

**Dos domingos en la iglesia — fase 5.** La interfaz nueva en Windows, con la
vieja a un clic por si algo falla.

**Visita 4 a la Mac — por cada función de la fase 6** que se haga.

## 19. Decisiones de Alexis (15/11/2026)

| Tema | Decisión |
|---|---|
| Mac de prueba | Prestada. Falta saber chip y versión de macOS |
| Cuenta de Apple (99 dólares por año) | **No pagar nunca.** Con guía de «Abrir igualmente» |
| Mac con Intel | Chip Apple primero. Intel después, si aparece quien la pruebe |
| Windows | **Convivir un tiempo.** WPF sigue hasta que la interfaz nueva lo iguale |
| Qué puede faltar en la primera versión de Mac | Capturar otra ventana; panel web y YouTube en vivo; PowerPoint directo; seguir la canción y Spotify |
| Funciones nuevas durante la migración | Sólo arreglos y cosas chicas |
| Orden | Publicar la 1.4.0 primero, con «Seguir la canción» visible sólo para Alexis |
| Numeración | Mismo número en Windows y Mac. Mac lleva «beta» |

**«Seguir la canción» sólo para Alexis.** Su idea: que aparezca sólo si
existe un archivo. Sirve y es simple: si en la carpeta de datos
(`%APPDATA%\EcclesiaCast`) hay un archivo `seguir-la-cancion.txt` (puede
estar vacío), la opción aparece en el menú Proyección, en Ajustes y en la
barra de abajo; si no, no existe para nadie. Es un cambio chico en la rama de
la 1.4.0, aparte de este plan. De paso deja un mecanismo para futuras
funciones en prueba.

## 20. Datos verificados (15/11/2026) y los que no

Verificados en la fuente:

- Avalonia 12.1.6 (9/11/2026), MIT, .NET 8 y .NET 10; macOS 26 «nivel 1», 15 y
  14 «nivel 2», chip Apple e Intel. `Avalonia.Controls.WebView` 12.1.2, MIT:
  WebView2 en Windows, WebKit en Mac, sólo como control nativo.
  `Avalonia.Headless` 12.1.6. Avalonia XPF: 29.500 euros por programa.
  ([docs.avaloniaui.net](https://docs.avaloniaui.net/docs/supported-platforms),
  [avaloniaui.net/xpf](https://avaloniaui.net/xpf))
- .NET 10: macOS 15, 26 y 27; chip Apple e Intel. macOS 14 sin soporte desde
  el 14/9/2026. .NET 8 sin soporte desde el 10/11/2026.
  ([github.com/dotnet/core](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md))
- GitHub: `macos-15`, `macos-26` (chip Apple), `macos-15-intel`,
  `macos-26-intel`; gratis y sin límite en proyectos públicos.
  ([docs.github.com](https://docs.github.com/en/actions/reference/runners/github-hosted-runners))
- Apple: 99 dólares por año; exención sólo para organizaciones. Pasos de
  «Abrir igualmente» en español.
  ([developer.apple.com](https://developer.apple.com/support/fee-waiver/),
  [support.apple.com](https://support.apple.com/es-lamr/102445))
- Red local: recibir conexiones no pide permiso (nota técnica TN3179).
  ([developer.apple.com](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy))
- LibVLCSharp 3.10.2 y LibVLCSharp.Avalonia 3.10.2 (13/10/2026), LGPL.
  ([nuget.org](https://www.nuget.org/packages/LibVLCSharp))

**No pude verificar** (lo resuelve la tanda que corresponde):

- Si `VideoLAN.LibVLC.Mac` 3.1.3.2 trae librerías para chip Apple (tanda 1).
- Si una persona de Argentina puede anotarse en el programa de Apple (no hace
  falta, por la decisión de no pagar).
- Cómo se ve el «Abrir igualmente» en macOS 26 y 27: la ayuda de Apple muestra
  macOS 15 (visita 1).
- Qué entrada de sonido y qué versión de Whisper convienen en Mac (fase 6).
- Fecha en que GitHub retira las Mac con Intel.
