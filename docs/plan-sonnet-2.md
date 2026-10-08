# Plan de trabajo para Sonnet — segunda tanda (pruebas de la beta.6)

Alexis instaló la beta.6, la probó y mandó esta lista. Leé el plan entero
antes de tocar nada. Las **reglas de trabajo** son las mismas de
[plan-sonnet.md](plan-sonnet.md), sección 2 (rama `beta6`, sin push, un commit
por tema, castellano rioplatense para todo lo que se ve, comentarios en
inglés, pruebas en Core, CHANGELOG y pendientes al día). Lo que se suma:

- **No abras la aplicación ni ninguna ventana** salvo que el encargo diga lo
  contrario: Alexis usa la PC. Para lo visual, dibujá controles a PNG desde un
  programa de consola STA (`RenderTargetBitmap`), como ya se hizo.
- **La biblioteca real no está en `%APPDATA%`.** Desde Claude, `%APPDATA%` es
  una copia vieja que la app empaquetada redirige. La real está en
  `\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`. **Nunca escribas
  ahí.** Si necesitás datos reales, copiá sólo `ecclesiacast.db` (sin `-wal` ni
  `-shm` de otra copia: mezclarlos daña la base) a una carpeta temporal.
- Para diseño hay dos skills en `.claude/skills/`: **impeccable** (usá su modo
  *Operate*, que es el de programas, y sus guías `*.native.md`) y
  **redesign-existing-projects**. Están pensadas para la web: tomá los
  criterios (jerarquía, contraste, estados, consistencia), no el CSS.
- Antes de programar cada punto, mirá **todos** los puntos de su tanda: varios
  tocan los mismos archivos (`MainWindow.xaml`, `MainViewModel.cs`).

Se trabaja **una tanda por vez**. Al terminar una, frená y entregá el informe
(sección 5): Opus la revisa antes de la siguiente.

## 1. Tanda 1 — lo que falla

### 1.1 YouTube: que descargue y que se arregle solo
Registro del 7/10 22:28: la sesión de YouTube sigue bloqueada («Could not copy
Chrome cookie database», aun con `--disable-features=LockProfileCookieDatabase`
en `WebViewProfile`). Sin sesión, YouTube devuelve **403** porque el yt-dlp de
la PC es de julio. Alexis no tiene que escribir comandos nunca.
- **EcclesiaCast maneja su propio yt-dlp** en `YtDlp.ToolsFolder`: si no está,
  lo baja de la versión oficial (releases de GitHub de yt-dlp, `yt-dlp.exe`)
  mostrando el progreso. Se actualiza solo con `yt-dlp -U`: antes de descargar
  si pasaron más de 3 días desde la última revisión, y siempre que aparezca un
  403 (y entonces reintenta una vez). Fijate en la documentación actual de
  yt-dlp si para YouTube necesita un intérprete de JavaScript (deno); si lo
  necesita, que el programa lo resuelva igual que yt-dlp, sin pedirle nada a
  Alexis.
- **La sesión, sin leer el archivo bloqueado**: pedile las cookies de YouTube
  y Google al propio navegador del programa (`CoreWebView2.CookieManager`,
  mismo entorno que `WebViewProfile`), escribilas a un `cookies.txt` temporal
  en formato Netscape, pasalo con `--cookies` y borralo al terminar. Si no hay
  ningún WebView2 creado, creá uno oculto con ese entorno sólo para eso.
- Los mensajes de error, en castellano y sin instrucciones técnicas: si igual
  falla, decí qué pasó y que se puede proyectar por internet.

### 1.2 YouTube en medios
- Lo que se descarga queda en **la pestaña de medios que está elegida**, no en
  «Descargados».
- Los videos de YouTube que se reproducen por internet llevan una marca a la
  vista en su tarjeta (por ejemplo 🌐 «por internet»); los descargados no.
- Al **agregar** un video de YouTube, preguntá: «Reproducir por internet» o
  «Descargar para usar sin internet».
- «Descargar para uso local» aparece **sólo** en el menú de los videos de
  YouTube (hoy aparece en todos).

### 1.3 Las flechas no saltan solas al siguiente ítem de la playlist
Registro del 7/10 22:51:58: estando en Juan 8:14, en menos de un segundo el
programa puso el video «06» de fondo y saltó a 8:3. La tecla quedó apretada:
las flechas pasaron el final del pasaje de la playlist (`LeavesPassage`,
`TryAdvancePlaylist`, `NextSlide`/`PreviousSlide` en `MainViewModel.cs`) y
siguieron al ítem siguiente (un medio) y al de después.
- Al llegar al final (o al principio) de un ítem de la playlist, las flechas
  **se frenan** y la barra de estado dice «Fin del pasaje: → otra vez pasa a
  «…»». Recién otra pulsación **nueva** (no la repetición automática de la
  tecla: `KeyEventArgs.IsRepeat`) pasa al siguiente.
- Moverse entre versículos o diapositivas **nunca** cambia el fondo.

### 1.4 El vigía (cuelgues)
Alexis vio que el programa se congeló unos 5 segundos y volvió solo; el
registro no dice nada. Agregá un vigía:
- Un hilo aparte le pregunta a la ventana cada medio segundo si está viva
  (`Dispatcher.BeginInvoke` con prioridad normal). Si pasan más de 2 s sin
  respuesta, anota en el registro `[WRN] Ventana congelada…` y, cuando vuelve,
  cuánto duró en total.
- Para saber qué la congeló, llevá una lista corta de las últimas acciones
  (proyectar diapositiva, poner fondo, abrir capítulo, guardar…, con su
  nombre) y volcala en ese aviso.
- Que no cueste nada mientras todo anda bien.

### 1.5 La barra azul al reordenar se escapa
Si se mueve rápido, la barra de `SlideDragAdorner` aparece encima de la barra
de medios. Recortá el dibujo del adorno al área visible del panel de
diapositivas (`SlidesScroll`) y no dibujes la barra si el mouse está fuera.

### 1.6 El PREVIEW con un anuncio en primer plano
Con un medio en primer plano (anuncio) en pantalla, el PREVIEW («Siguiente»)
mostraba la letra encima de la imagen del anuncio. La próxima diapositiva
**saca** el primer plano (ver `GoLiveSlide`), así que el PREVIEW tiene que
mostrar la letra sobre el fondo que va a quedar (el fondo normal, o ninguno),
nunca sobre el anuncio.

### 1.7 LIVE y PREVIEW sin deformarse
Al cambiar el ancho del panel derecho, lo que se ve en LIVE y PREVIEW se
estira o se corta. Los dos cuadros mantienen **siempre** la proporción de la
salida (la pantalla elegida en «Salida», por ejemplo 16:9). Si el panel se
ensancha, el cuadro **no crece hacia abajo**: queda del alto que tenía y
centrado, con espacio a los costados; si se angosta, se achica entero. Mirá
`RightPanel` en `MainWindow.xaml` y `MainWindow.Layout.cs`.

### 1.8 Arrastrar medios dentro del programa
Arrastrar desde el Explorador ya anda. Falta:
- **Un medio a la playlist**: se suelta en el lugar exacto, con la misma línea
  azul que usan las canciones (`PlaylistBody_DragOver`/`Drop` en
  `MainWindow.xaml.cs`).
- **Un medio a otra pestaña** de la lista de la izquierda: se mueve a esa
  pestaña (cambia su `Category`). La pestaña de destino se resalta mientras
  pasa por encima.

## 2. Tanda 2 — Biblia, playlist, resaltado, medios y tiempos

### 2.1 Versiones de la Biblia en una fila
- Una fila de botoncitos con las abreviaturas (`RVR1960 · NVI · NTV · TLA`):
  clic = usar sólo esa; Ctrl+clic = sumar o sacar la segunda. Las activas se
  ven encendidas, la primera distinta de la segunda.
- Un botón para **expandir** la fila y ver la lista con los nombres completos
  (que se recuerde abierta o cerrada).
- **Clic derecho** sobre una versión: Editar (nombre y abreviatura, ya existe
  `RenameBibleVersionCommand`), Eliminar y **Exportar** a un archivo que el
  mismo programa sabe volver a importar (el JSON que lee `JsonBibleImporter`).
  Prueba en Core: exportar e importar da lo mismo.

### 2.2 «➕ Playlist» de la Biblia con cajita
Al tocar «➕ Playlist» se abre una cajita (popup) con un campo ya completado
con lo que agregaría hoy (el tramo escrito, el versículo en pantalla o el
capítulo; ver `AddCurrentPassageToPlaylist`). Ahí se puede escribir otra cita
(«juan 8 del 5 al 10»), se ve el texto del pasaje y la versión, y «Agregar».
El clic derecho sobre un versículo queda como está.

### 2.3 Resaltado: varias cosas y sólo la elegida
- Al marcar en el LIVE (clic o arrastre) se resalta **sólo esa aparición** (la
  posición en el texto), no todas las veces que aparece la palabra.
- Con **Shift** se suma otra marca sin perder las anteriores. Volver a marcar
  algo ya resaltado lo quita.
- En el cuadro de la pestaña Resaltar se pueden escribir varias cosas
  separadas por **;**. Lo que se escribe ahí sí marca todas las apariciones.
- Al cambiar de diapositiva se borra todo (ya es así).
- Cambia el modelo: hoy `IPresentationService.HighlightTerm` es un texto; pasa
  a ser una lista de términos (del cuadro) más una lista de posiciones (del
  LIVE). Revisá **todos** los que dibujan el resaltado: `SlideView`, la salida,
  el LIVE, el escenario, el control remoto y la salida web para OBS/vMix.
  Pruebas en Core para el modelo.

### 2.4 Canción repetida
Al guardar una canción nueva o importarla (desde cero, internet, ProPresenter,
archivo) con un título que ya existe (sin distinguir mayúsculas ni acentos),
preguntá: **Reemplazar**, **Guardar como «Título (1)»** (el primer número
libre) o **Descartar**. Si se importan varias, una casilla «Hacer lo mismo con
las demás». La regla del nombre libre va en Core con pruebas.

### 2.5 Pestañas de medios y qué pasa después de un medio
- **Clic derecho** sobre una pestaña: **Cambiar nombre** (se pueden usar
  emojis) y **Eliminar**. Al eliminar, un aviso simple: «Los medios de esta
  pestaña ya no se van a ver en el programa (los archivos no se borran)».
- **Regla por pestaña** (en el mismo menú, «Configurar…»): todo lo de esta
  pestaña va como **Fondo**, como **Primer plano** o **como diga cada medio**.
  Para primer plano, una casilla «Al terminar, mostrar el logo».
- **Qué sigue después de un medio** (en Propiedades del medio): nada, el
  logo, u **otro medio** que se elige de la biblioteca. Para las imágenes en
  primer plano, cuántos segundos se muestran antes de seguir. Ya existe
  `VideoEndBehavior` (Loop, Stop, Logo): sumá lo que falte y la migración.

### 2.6 Sacar la lista inteligente
Se elimina del todo: botón ✨, ventana, comandos, `SmartPlaylist` y lo que
quede en la base (las listas inteligentes que existan se borran al arrancar).

### 2.7 Pestaña «Tiempos»
Una pestaña nueva abajo a la derecha (junto a Texto rápido, Aviso al pie…):
- **Secciones de la reunión** con su tiempo (por ejemplo Alabanza 25 min,
  Anuncios 5, Prédica 35), editables y guardadas.
- Se elige una sección y se maneja: iniciar, pausar, volver a cero, +1 y −1
  minuto, pasar a la siguiente sección. Si se pasa del tiempo sigue contando
  en negativo y se pone en rojo.
- **Cronómetro**: iniciar, pausar, poner en cero.
- En el escenario, el recuadro «Temporizador» muestra lo que le queda a la
  sección en marcha. La «cuenta regresiva» que se proyecta antes de empezar
  sigue siendo otra cosa, con su propio tipo de recuadro.

## 3. Tanda 3 — lo nuevo y más grande

### 3.1 Menús y listas oscuros
Hoy los menús de clic derecho, las listas desplegables (ComboBox) y los menús
de arriba salen con el estilo blanco de Windows. Estilos globales en
`App.xaml` para `ContextMenu`, `MenuItem` (submenús, separadores, tildes,
atajos), `ComboBox` y su lista, `ToolTip` y el `Menu` de arriba, con la
paleta que ya existe (`BgBrush`, `SurfaceAltBrush`, `LineBrush`,
`AccentBrush`, `MutedBrush`). Revisá el resultado con los criterios de la
skill impeccable (modo Operate).

### 3.2 Pantalla de carga y «Acerca de» la primera vez
- Al abrir: una pantalla de carga con el logo actual del programa
  (`app.ico` / `installer/make-icon.ps1`), el nombre y la versión, mientras
  carga; se cierra cuando aparece la ventana principal.
- La **primera vez** que se abre el programa se muestra «Acerca de», con el
  botón **«Aceptar»** en vez de «Cerrar».

### 3.3 LIVE, PREVIEW y Escenario fusionados
De entrada, **un solo recuadro** con pestañas **LIVE · PREVIEW · ESCENARIO**.
En el menú Ver, «Separar el PREVIEW del LIVE» vuelve al diseño actual de dos
recuadros. El programa **recuerda** la elección (como los demás ajustes de
`MainWindow.Layout.cs`).

### 3.4 Videos pesados
Al importar un video de 4K o de 50 cuadros o más (ya lo mide ffprobe; mirá
`LightCopyPolicy` y `LightVideoCache`), avisá: «Este video es muy pesado para
proyectar. ¿Hago una copia más liviana?». Sí → se hace la copia ya, con
progreso; la biblioteca guarda **los dos** (el original y la copia liviana para
proyectar, como hoy). En el LIVE, los videos pesados se muestran con su
póster quieto, como YouTube.

### 3.5 Audios en la pestaña Música
Además de los botones para Spotify: una **lista de audios** propia. Se
importan (mp3, wav, m4a, ogg, flac), se ordenan, y se reproducen con su barra
(reproducir, pausa, anterior, siguiente, adelantar y atrasar, volumen propio).
Con LibVLC (`App.VideoEngine`), un reproductor aparte del de video. Se guardan
en la base.

## 4. Lo que no se toca
- «Acerca de: una forma de colaborar» sigue pendiente (todavía no está
  decidido a dónde mandar a la gente).
- **Biblias**: nada de agregar, descargar ni incluir Biblias en el instalador.
  Por las licencias, Alexis lo decide más adelante.

## 5. Informe al terminar cada tanda
En castellano, para Alexis: qué quedó hecho (con los commits), cómo lo
probaste, qué no pudiste probar (eso también va a `docs/pendientes.md`,
sección «Para probar»), qué decidiste vos y qué quedó pendiente.
