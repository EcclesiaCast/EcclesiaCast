# Plan de trabajo para Sonnet — prueba del fin de semana (11/10/2026)

Leelo entero antes de tocar nada, aunque te toque un solo tramo. Los
diagnósticos los hizo Opus leyendo el código el 13/10; los números de línea
son aproximados (pueden haberse movido): buscá por nombre.

## 0. Reglas

Valen las **reglas de trabajo** de [plan-sonnet.md](plan-sonnet.md) (sección 2):
- No abrir la aplicación ni ninguna ventana visible. Alexis está usando la PC.
  Para lo visual, dibujá a PNG desde un programa de consola STA en tu carpeta
  temporal (nunca dentro del repo), con `Measure`/`Arrange` +
  `RenderTargetBitmap`.
- No escribir nunca en la biblioteca real
  (`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`).
- No ejecutar python: no está instalado y llamarlo abre la Microsoft Store.
- **No ejecutes nunca un instalador** ni nada que descargues.
- Sin push. **Un commit por punto**, mensaje en inglés, en imperativo,
  explicando el porqué, terminado en
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Castellano rioplatense para todo lo que se ve.
- Archivos con CRLF y acentos: **herramienta Edit**, nunca sed/perl/
  Set-Content. Al final de cada punto: `grep -rn "Ã\|â€" src` vacío.
- Antes de cada commit: `dotnet build` sin errores ni advertencias nuevas y
  `dotnet test tests/EcclesiaCast.Core.Tests` en verde.
- Lógica pura en `EcclesiaCast.Core` con pruebas
  (`Una_frase_que_dice_lo_que_pasa`).
- `CHANGELOG.md` `[Sin publicar]`: una entrada por punto, con el estilo de las
  que hay. Lo que sólo se pueda probar con el programa abierto va a
  `docs/pendientes.md` §1 («Para probar»).

**Trabajo en paralelo.** Cada tramo lo hace un agente distinto, cada uno en su
propia copia (worktree) del repo. Tocá sólo lo de tu tramo. Si para tu punto
hace falta cambiar algo que este plan asigna a otro tramo, no lo hagas:
anotalo en el informe. Opus junta los tramos al final y resuelve los choques
de `CHANGELOG.md`, `pendientes.md`, `MainViewModel.cs` y `MainWindow.xaml`.

Ya hecho por Haiku antes de empezar (plan-haiku-1.md): tooltip de la grilla
según el tipo de tarjeta, la imagen de fondo del tema no tapa los medios, los
cuadros extra se van con F2, tipografía de los recuadros del escenario
(notas) y del texto rápido. No los rehagas.

---

## Tramo A — salida: YouTube, salto a negro, audio, transiciones, aviso al pie

Archivos principales: `Views/OutputWindow.xaml(.cs)`,
`Controls/ProjectedView.xaml(.cs)`, `Controls/SlideView.xaml(.cs)`,
`Controls/VlcVideoSurface.xaml(.cs)`, `Controls/YouTubeSurface.xaml.cs`,
`Services/VlcAudioPlayer.cs`. Orden: A1, A2, A3, A4, A5.

### A1. El logo no tapa un YouTube sin descargar

«cuando pongo un video de youtube no descargado, cuando le pongo logo no se
va; tengo que cambiar a otro medio para que sí lo cambie».

Causa: `YouTubeSurface` es un `WebView2` (ventana nativa, HWND) que Windows
dibuja siempre encima de lo WPF. El logo es WPF (`SlideView.LogoLayer`).
`OutputWindow.OnProjectionChanged` sólo llama a `UpdateLogoVideo()`; a
YouTube lo toca únicamente `UpdateVideo()`, que corre cuando cambia el fondo.
Pasa lo mismo con F1 (negro) y el aviso al pie.

Arreglo, en `OutputWindow.xaml.cs`:
- `UpdateYouTubeCover()`, llamado desde `OnProjectionChanged` cuando cambia
  `State` y al final de `UpdateVideo()`:
  - `covered = State is Logo or Black && Background is { Type: YouTube }`.
  - Tapado: `YouTube.Visibility = Hidden` y `YouTube.PauseForCover()`.
  - Si no, y el fondo es YouTube: `Visible` y `ResumeAfterCover()`.
- En `YouTubeSurface`: `PauseForCover`/`ResumeAfterCover` con su propio flag
  `_pausedForCover` (copiá el patrón de `PauseForHiddenOutput`); el flag se
  limpia en `Clear()`.
- `IsYouTubeShowing` hoy mira la `Visibility`: que mire
  `(DataContext as ProjectionViewModel)?.Background?.Type == MediaType.YouTube`,
  si no la barra de transporte le habla a VLC mientras está tapado.
- Descartado: pasar a `WebView2CompositionControl` (baja los fps del video).
- Para probar: YouTube → F3 → F3, y lo mismo con F1.

### A2. El salto a negro al elegir otro medio

«que al seleccionar un nuevo medio no meta ese salto negro».

Causas (todas sobre el `Background="Black"` de `OutputWindow.xaml`):
- a) Imagen → imagen: `ProjectedView.OnBackgroundChanged` cambia
  `BackgroundImage.Source` y anima la opacidad **de 0 a 1** (320 ms): la vieja
  desaparece al instante y la nueva sube desde negro.
- b) Video → imagen: `Video.Show(imagen)` → `Stop()` → `ClearSurface()` borra
  el cuadro, y la imagen sube desde 0.
- c) Imagen → video: la imagen se borra al instante y la superficie VLC queda
  vacía hasta el primer cuadro (100 a 500 ms de negro).
- d) Hacia YouTube: el HWND se muestra negro mientras carga (1 a 3 s).
- Video → video no va a negro (queda el último cuadro).

Arreglo: **quedarse con el último cuadro y fundir**, sin tocar la regla de
`player.Stop()` antes de cada `Play()` (es lo que evita que VLC corrompa la
memoria; leé los comentarios de `VlcVideoSurface` antes de tocarlo) y sin
crear un segundo MediaPlayer:
- `VlcVideoSurface.xaml`: `<Image x:Name="Hold" Stretch="UniformToFill" Visibility="Collapsed"/>`
  después de `Surface`.
- `VlcVideoSurface.xaml.cs`:
  - `HoldLastFrame()` (hilo de UI, con `_sync`): si hay `_bitmap`, copiarlo
    congelado a `Hold.Source` con el mismo encuadre (`MediaFraming.Apply`) y
    desenfoque, mostrarlo; después `Surface.Source = null`, `_bitmap = null`,
    tamaño en 0.
  - En `Show` con un medio nuevo, llamarlo **antes** de `player.Stop()`.
  - `Stop(bool holdLastFrame = false)`: con true, `HoldLastFrame()` en vez de
    `ClearSurface()` (igual dispara `PreviewFrameChanged(null)`).
  - `ReleaseHold(TimeSpan fade)`: anima `Hold` a 0 y al terminar lo limpia.
  - Donde `OnDisplay` crea el `_bitmap` nuevo (el primer cuadro): evento
    `FirstFrame` y `ReleaseHold(fundido)`.
  - Respaldo: un `DispatcherTimer` de 2 s suelta el `Hold` si VLC falla.
    `Dispose` lo limpia.
- `OutputWindow.UpdateVideo`: si el fondo nuevo no es video,
  `Video.Stop(holdLastFrame: true)` y `Video.ReleaseHold(TimeSpan.Zero)` cuando
  termina el fundido de la imagen (b).
- `ProjectedView`: `<Image x:Name="PreviousImage"/>` debajo de
  `BackgroundImage`; guardar el medio anterior. Imagen nueva con imagen
  anterior: la vieja pasa a `PreviousImage` (con su encuadre y desenfoque), la
  nueva funde encima y al terminar se limpia `PreviousImage` (a). En la salida
  (`IsLiveOutput`), si el nuevo es video: mantener la imagen en
  `PreviousImage` hasta `ReleaseHeldImage(fade)`, que `OutputWindow` llama
  desde `Video.FirstFrame`, con respaldo de 2 s (c).
- YouTube (d), opcional: `YouTubeSurface` dispara `Started` con el primer
  mensaje de reproducción (`OnWebMessage`) y `OutputWindow` lo mantiene
  `Hidden` hasta ese evento con lo anterior retenido. Si dudás de que WebView2
  oculto arranque, dejalo como corte directo y anotalo.
- Con «bajo rendimiento» (`AnimateTransitions=false`) se retiene igual pero se
  corta sin fundido.
- Verificación: render PNG de `ProjectedView` a mitad del fundido imagen A →
  B: el centro no tiene que ser negro. Lo de VLC va a «Para probar».

### A3. Sin sonido después de cambiar la salida de audio de Windows

«cambié la salida de audio y luego volví y no reproducía audio; se solucionó
al cerrar y abrir».

Causa probable: un solo `LibVLC` (`App.xaml.cs`) sin dispositivo elegido, y
cada `MediaPlayer` vive todo el programa (`VlcAudioPlayer.EnsurePlayer`,
`VlcVideoSurface.EnsurePlayer`). En LibVLC 3 la salida de audio (mmdevice)
queda guardada en el player; si Windows cambia o invalida el dispositivo,
queda muerta hasta recrear el player. YouTube (Chromium) sigue al
predeterminado solo.

Arreglo:
- `Services/AudioEndpointWatcher.cs`: COM `IMMNotificationClient` registrado
  en `MMDeviceEnumerator` (CLSID BCDE0395-E52F-467C-8E3D-C4579291692E) desde
  `App.OnStartup`; sube un `static int Generation` en
  `OnDefaultDeviceChanged`/`OnDeviceStateChanged`/`OnDeviceRemoved`. Mantené
  viva la referencia y desregistrala al cerrar.
- Antes de cada `Play`, justo después de `player.Stop()`, si cambió la
  generación: `player.SetAudioOutput("mmdevice")` (en `VlcAudioPlayer.Play` y
  en `VlcVideoSurface.Show`). Confirmá en la documentación o fuente de
  LibVLC 3 que eso recrea la salida con el predeterminado actual. Si no
  alcanza, en `VlcAudioPlayer` (sin callbacks de video) se puede recrear el
  `MediaPlayer`; en `VlcVideoSurface` **no** (los delegados).
- Lo que ya está sonando: al recibir el evento (pasado al hilo de UI),
  reabrir la salida guardando la posición (`Time` → Stop → SetAudioOutput →
  Play con `:start-time=`), si no está silenciado.
- Botón de emergencia «Reiniciar el sonido» (menú Ver o panel Música).
- `Log.Information` del cambio de dispositivo.
- Para probar: cambiar la salida de Windows con música, video con sonido y
  YouTube.

### A4. Transiciones configurables en segundos (logo, medios, negro, salida)

«agregar transiciones de diapos o logos, con efectos de desaparición
configurables en segundos».

Hoy: el texto tiene transición por tema (`Transition` + `TransitionMs`, tope
1500 ms, en ms en `ThemeManagerWindow`). El logo aparece y desaparece de golpe,
el negro funde 250 ms fijos, la imagen de fondo 320 ms fijos, el aviso entra
con 280 ms y sale de golpe, apagar la salida es un `Hide()` seco.

Arreglo:
- Core `Presentation/OutputTransitions.cs`: record
  `(double LogoSeconds = 0.5, double MediaSeconds = 0.4, double BlackSeconds = 0.25, double OutputSeconds = 0)`
  con `Clamp` (0–10 s), `ToSetting()`/`Parse()` en cultura invariante
  (`"logo=0.5;media=0.4;black=0.25;output=0"`), valores por defecto si no se
  puede leer. Pruebas.
- App: `Services/TransitionSettings.cs` estático como `PerformanceMode`
  (`Current` + `Changed`), cargado desde la clave `output.transitions`.
  Ventana `Views/TransitionsWindow.xaml` con `NumericField` en segundos
  (un decimal), desde «Proyección → Transiciones…».
- `SlideView.OnStateChanged`: logo entra y sale con fundido de `LogoSeconds`
  (al salir, `Collapsed` en `Completed` **sólo si el estado sigue sin ser
  Logo**); negro con `BlackSeconds`; aviso y cuenta regresiva también salen con
  fundido. `OutputWindow.UpdateLogoVideo`: fundir `LogoVideo`.
- `ProjectedView`: los 320 ms pasan a `MediaSeconds` (encaja con A2).
- Salida: si `OutputSeconds > 0`, al apagar se funde un rectángulo negro
  arriba de todo y después `Hide()` (YouTube ocultarlo antes, por el HWND).
- Tema en segundos: `ThemeManagerViewModel.TransitionSeconds` que convierte a
  y desde `TransitionMs` (la base sigue en ms: sin migración). Tope a 5000 ms
  en `SlideView` y en `ThemeManagerViewModel`. En el XAML, `Step 0.1`,
  `Decimals 1`, etiqueta «s».
- Render PNG de `SlideView` a mitad del fundido del logo.

### A5. Tipografía del aviso al pie

«poder cambiar la tipografía de aviso al pie» (texto rápido y notas ya los
hizo Haiku).

Hoy está fijo en `SlideView.xaml` (`OverlayText`: Segoe UI, 54, Medium). El
dato pasa por `PresentationService.ShowOverlay` → `ProjectionViewModel.Overlay`
→ `ProjectedView.Overlay` → `SlideView`.
- `MainViewModel`: `[ObservableProperty] string _overlayFont` y
  `double _overlayFontSize = 54`, guardados en `overlay.font` y
  `overlay.size`; al cambiar, guardar y copiar a `Projection`. Inicializar al
  cargar.
- `ProjectionViewModel`: `OverlayFontFamily` y `OverlayFontSize` (no tocar
  `Sync()`: no vienen de `IPresentationService`).
- `ProjectedView`: DPs que reenvían a `SlideView`, igual que `Overlay`.
- `SlideView`: DPs `OverlayFontFamily` (string) y `OverlayFontSize` (double,
  54, entre 20 y 160) que aplican en `OnOverlayChanged`.
- Enlaces en `OutputWindow.xaml` y en el LIVE de `MainWindow.xaml`.
- UI en la pestaña Aviso al pie: ComboBox editable de tipografía (reusá
  `SystemFonts` del VM si Haiku lo creó) y `NumericField` de tamaño.
- Render PNG de `SlideView` con aviso en Georgia.

---

## Tramo B — medios y diseñador de diapositivas

Archivos principales: `MainWindow.xaml(.cs)` (menús de medios y pestañas),
`MainViewModel.cs` (logos y pestañas), `Core/Media/MediaTabRule.cs`,
`Views/MediaTabRuleWindow`, `Views/SongDesignerWindow.xaml(.cs)`,
`Core/Presentation/SlideTextBox.cs`, `Controls/SlideTextFit.cs`.
Orden: B1, B2, B3, B4.

### B1. Clic derecho en una imagen o video → «Usar como logo»

- En `MainWindow.xaml`, menú `MediaCardMenu`, después de «Agregar a la
  playlist»: `MenuItem x:Name="UseAsLogoItem" Header="Usar como logo"` con el
  mismo patrón de binding que las demás entradas
  (`PlacementTarget.Tag.UseMediaAsLogoCommand`).
- En `MediaMenu_Opened` (MainWindow.xaml.cs): visible sólo si es imagen o
  video, no YouTube ni web, uno solo elegido, y **no** una página de PDF/PPT
  (`DocumentImporter.IsRenderedPage(media.Path)`: al quitar el medio se borra
  el archivo y el logo quedaría roto).
- `MainViewModel`, junto a los logos: `[RelayCommand] UseMediaAsLogo(MediaItem? item)`:
  - Si ya hay un logo con el mismo `Path` (sin mirar mayúsculas), elegirlo
    (`SelectedLogo`) y salir.
  - Si no: `new Logo { Name, Kind = Video ? LogoKind.Video : LogoKind.Image, Path, Scaling, Muted = true, Order = Logos.Count }`.
    Para video, póster **nuevo** con `MediaThumbnails.Create(...)` dentro de
    `Task.Run` (no reusar `item.ThumbnailPath`: borrar el logo borra su póster).
  - `_logos.Save`, `LoadLogos(created.Id)`, `RefreshLightCopies()` y
    `StatusText = "«X» agregado como logo (F3 lo muestra)."`. No lo muestra
    solo.

### B2. Silenciar una pestaña de medios entera, con ícono

Cada pestaña guarda sus opciones en settings (`ContinuousKey`, `RuleKey` →
`MediaTabRule`) y **todo** medio pasa por `Playable(item) = TabRule(item.Category).Resolve(item)`
antes de salir. `MediaItem.Muted` ya lo respetan VLC y YouTube.
- Core: `MediaTabRule(MediaTabMode Mode, bool LogoAtEnd, bool Muted = false)`.
  `Resolve`: si `Muted && !item.Muted`, devolver un clon silenciado (y el mismo
  objeto si no cambia nada). `ToSetting`: sufijo `+mute`; `Parse`: separar
  por `+`, compatible con los textos viejos. Pruebas (ida y vuelta, textos
  viejos, `Resolve`).
- `MediaTabRuleWindow`: hoy arma `new MediaTabRule(mode, logo)` y perdería el
  silencio. Agregá la casilla «Silenciar todos los medios de esta pestaña».
- Menú de la pestaña (`MediaTab_Loaded`): «🔇 Silenciar la pestaña» /
  «🔊 Quitar el silencio», con el encabezado actualizado al abrirse.
- `MainViewModel.ToggleMediaTabMute(tab)`: guarda la regla; si el medio en
  pantalla es de esa pestaña, `_presentation.RefreshBackground(...)` como se
  hace al cambiar la regla, para que se aplique en vivo; notifica
  `MutedMediaTabs` (propiedad nueva, conjunto de nombres).
- YouTube: `YouTubeSurface.Play` sale temprano si es el mismo video; antes de
  ese `return`, `Run(media.Muted ? "mute()" : "unmute()")` y sumar `mute`/
  `unmute` a `window.ec` en `PlayerHtml`. (Ojo: el tramo A también toca
  `YouTubeSurface`; tocá sólo esas líneas.)
- Ícono: en la plantilla de la pestaña, un «🔇» a la derecha del nombre,
  visible con un `MultiBinding` (nombre + `MutedMediaTabs`) y un convertidor
  nuevo.
- Render PNG de la lista de pestañas con una silenciada.

### B3. Varios cuadros de texto en el diseñador

«cuando agrego otro cuadro de texto en una misma diapositiva no puedo editar
la anterior; si guardo y entro de nuevo puedo editar la principal pero no la
secundaria» y «cuando una diapositiva tiene más de un cuadro de texto, uno de
esos recuadros queda sobre el resto».

El commit 05dea8e (posterior a la beta que probó Alexis) ya permite elegir un
cuadro haciendo clic sobre él (`BoxAt`, gana el más chico). Falta:
- a) **Dos cuadros nuevos caen en el mismo lugar**: `SlideTextBox.Fresh()`
  devuelve siempre 660,820,600×140, así que el segundo tapa al primero y
  empatan en `BoxAt`. Core: `SlideTextBox.FreshAvoiding(IReadOnlyList<SlideTextBox> others, string text = "Texto")`
  que prueba en orden abajo-centro (660,820), arriba-centro (660,120),
  abajo-izquierda (60,820), abajo-derecha (1260,820), arriba-izquierda
  (60,120), arriba-derecha (1260,120), centro (660,470) y devuelve el primero
  que no se cruza con ninguno; si no hay, cascada +40,+40 dentro del lienzo.
  Usarlo en `AddTextBox_Click`. Pruebas.
- b) **Clics ignorados mientras se escribe**: con `_editing`,
  `Overlay_MouseDown` y `Box_MouseDown` hacen `return` y el operador queda
  atrapado. En `Overlay_MouseDown`: si `_editing`, `CommitEdit()` **antes** de
  cambiar `_selectedBox` y seguir con la selección; al principio de
  `SelectBoxAt`, lo mismo. En `Box_MouseDown` dejar el `return` (el clic cae
  en el propio editor).
- c) **Contornos de los otros cuadros**: `<Canvas x:Name="GhostLayer" IsHitTestVisible="False"/>`
  dentro de `Overlay`, antes de `BoxBorder`. `DrawGhosts()` dibuja un
  rectángulo punteado (#66FFFFFF, 4,3) y un rótulo chico «Letra» / «Cuadro N»
  por cada cuadro no elegido; llamarlo al final de `PlaceBox` y de
  `RefreshSelected`.
- d) Tab sobre el cuadro elegido recorre los cuadros; botones «Traer
  adelante» / «Enviar atrás» junto a ➕ y 🗑 (intercambian `Boxes[i]` con
  `Boxes[i±1]`).
- e) `BoxPicker_Changed`: `RefreshSelected()` después de `LoadControls`. Y si
  hay una tipografía escrita sin confirmar en `FontCombo`, confirmarla antes
  de cambiar de cuadro.
- Texto de ayuda: «clic en un cuadro para elegirlo · doble clic para escribir».
- Render PNG: ventana del diseñador sin `Show()` (crear `new App()` +
  `InitializeComponent()` sin `Run()`), dos `AddTextBox_Click` por reflexión,
  `Measure`/`Arrange` del contenido y `RenderTargetBitmap` del lienzo.

### B4. Que la letra no pise los cuadros extra en la salida

`SlideTextFit.TextArea` aparta la letra de la referencia pero ignora
`Override.TextBoxes`; una letra larga se dibuja encima de un cuadro de abajo.
- Core `Presentation/TextAreaCarver.cs` con su propio
  `record struct BoxRect(double X, double Y, double W, double H)`: por cada
  cuadro que se cruza con el área de la letra **y cubre al menos el 50 % de su
  ancho**, achicar el área (cuadro debajo del centro: `h = max(60, box.Y - 12 - y)`;
  arriba: `y = box.Bottom + 12` y descontarlo de `h`). Los cuadros angostos de
  una esquina no recortan nada. Pruebas (cuadro abajo a lo ancho, en una
  esquina, sin cuadros).
- Usarlo en `TextArea` después del bloque de la referencia.
- CHANGELOG: avisar que cambia cómo se ven las canciones con un cuadro a lo
  ancho.
- Render PNG de `SlideView` con letra larga y un cuadro abajo.

---

## Tramo C — Biblia: flechas e historial

Archivos: `MainViewModel.cs` (`Navigate`, `ProjectReference`,
`OnBibleQueryChanged`, `ProjectBibleResult`), `MainWindow.xaml(.cs)` (caja
de búsqueda bíblica, `OnPreviewKeyDown`), Core nuevo. Orden: C1, C2.

### C1. La flecha no pasa al versículo siguiente después de buscar uno

Caso A (el de Alexis): Enter en `BibleQueryBox` proyecta bien
(`ProjectReference` → `GoLiveSlide`), pero el foco sigue en la caja y
`OnPreviewKeyDown` sale antes de navegar
(`if (e.OriginalSource is TextBox || WebHasKeyboard) return;`): la flecha
mueve el cursor del texto.

Caso B (foco afuera, versículo marcado sin Enter): `Navigate` ignora
`PreviewSlideIndex` (`LiveSlideIndex < 0 && direction > 0 ? 0 : ...`) y el
índice 0 es la tarjeta «ANTERIOR», así que → salta al capítulo anterior.

Arreglo:
- Core `Playlists/SlideStep.cs`:
  `static int Target(int live, int preview, int direction, IReadOnlyList<bool> isJumpCard)`:
  con `live >= 0`, `live + direction`; con `live < 0` y `preview >= 0`, →
  devuelve `preview` y ← `preview - 1`; sin nada, → la primera tarjeta que no
  sea de salto y ← -1. Usarlo en `Navigate`. Pruebas (incluida la tarjeta de
  salto en el índice 0 y la lista vacía).
- VM: `ArrowsDriveSlidesFromSearch`, true al final de `ProjectReference` si
  quedó algo en vivo, false en `OnBibleQueryChanged` antes de buscar.
- `OnPreviewKeyDown`: si el origen es `BibleQueryBox` y ese flag está en
  true, las flechas navegan (como BibleShow); apenas se tipea, vuelven a
  editar el texto. Apagar también el flag en `PreviewMouseDown` de la caja.
- No cambiar `LeavesPassage` ni la lógica de playlist.

### C2. Historial de versículos buscados (como BibleShow)

- Core `Bible/BibleHistory.cs`:
  `record BibleHistoryEntry(int BookNumber, int Chapter, int VerseStart, int? VerseEnd, int? VersionId, string VersionAbbreviation, int? SecondaryVersionId, DateTimeOffset At)`
  con `Display` («Juan 3:16 · NTV», usando `BibleReferenceFormatter`);
  `Add(list, entry, capacity = 30)` (saca el duplicado por libro, capítulo y
  versículos sin mirar la versión; la nueva va arriba; recorta al tope);
  `Serialize`/`Parse` con System.Text.Json (null o basura → vacía; descarta
  libros fuera de 1..66 o capítulo/versículo < 1). Pruebas, más un ida y
  vuelta formatter → parser con «1 Corintios».
- VM: clave `bible.history` en settings; `ObservableCollection<BibleHistoryEntry> BibleHistory`
  cargada al arrancar; `RememberVerse` en `ProjectReference` (cuando proyectó)
  y en `ProjectBibleResult`. **No** registrar el avance con flechas ni los
  clics en tarjetas.
- `OpenHistoryEntry(entry)`: si la versión guardada está instalada y no es la
  principal, elegirla (y la secundaria si estaba); después
  `BibleQuery = BibleReferenceFormatter.Format(...)` (reusa `RunBibleQuery`:
  carga el capítulo y marca el versículo). `ProjectHistoryEntry`: lo mismo +
  `ProjectReference()`. `ClearBibleHistory`.
- UI: botón «🕘» a la derecha de la caja de búsqueda que abre un `Popup`
  (mismo patrón que `AddPassagePopup`, `StaysOpen=False`) con la lista (la
  más reciente arriba) y «Borrar historial». Clic: carga y marca. Doble clic o
  Enter: proyecta. ↓ en la caja vacía abre el popup.

---

## Tramo D — presentaciones, «en vivo» por sección, celular, GIF

Archivos: `MainViewModel.cs` (+ parcial nuevo `MainViewModel.Presentation.cs`),
`SlideItemViewModel.cs`, `MainWindow.xaml(.cs)`, `Core/Playlists/Playlist.cs`,
`Core/Presentation/PresentationService.cs`, `Remote/*`,
`Views/MediaTabRuleWindow` (sólo la casilla de presentación; el tramo B le
agrega la de silencio: tocá lo mínimo). Orden: D1, D2, D3, D4.

### D1. Una pestaña de medios como presentación en la grilla central

«que una carpeta de medios se pueda pasar a la parte central de las
diapositivas y usar como una presentación; por ejemplo al importar un pdf,
hacerlo a una carpeta nueva, y ahí queda como presentación y funciona en la
parte central como la Biblia o una canción, sin poder ponerle letras arriba».

Hoy: `ImportDocumentsAsync` crea un `MediaItem` Image/Foreground/Fit por
página («{doc} · N») en la pestaña abierta; la grilla central (`Slides`) sólo
la llenan canciones y Biblia, y sus tarjetas son sólo texto. Una pestaña es el
texto `MediaItem.Category`; sus opciones van en settings
(`media.tab.{nombre}.continuous` / `.rule`) y se mudan al renombrar.
Riesgo actual: con un PDF en pantalla, F2 vuelve a mostrar la letra anterior
encima (`ShowBackgroundOnly` deja `CurrentSlide`).

Decisiones (tomadas por Opus; Alexis puede cambiarlas después):
- Al importar PDF/PPT se pregunta, con la primera opción recomendada:
  «📑 Pestaña nueva «{archivo}», como presentación (recomendado)» o «Agregar
  las páginas a «{pestaña actual}»».
- En una pestaña marcada, un clic en la tarjeta de la barra abre la
  presentación en la grilla **y** proyecta esa página.
- Al salir de la presentación (una diapositiva de texto la saca) se restaura
  el fondo que había antes.
- Los segundos por imagen no avanzan solos en una presentación.
- Entran las imágenes y los videos locales de la pestaña; YouTube y web no.

Diseño:
1. Ajuste `media.tab.{nombre}.presentation = "1"`, con el patrón de
   `ContinuousKey`/`RuleKey`; se muda al renombrar y se borra al eliminar.
   Casilla en «Configurar…» de la pestaña. `MediaTabNames.Unique(tabs, base)`
   en Core («X», «X (2)»…) con pruebas.
2. `OpenPresentation(string tab, int? startMediaId)` en
   `MainViewModel.Presentation.cs`: limpia la grilla como `BuildSongSlides`
   (`SelectedSong=null`, `Slides.Clear()`, índices en -1, `_currentPassage=null`,
   `PreviewMedia=null`, `IsWebPanelOpen=false`), `_presentationTab = tab`,
   `GridTitle = "📑 " + tab`, una tarjeta por página
   (`new SlideItemViewModel(..., new SlideContent($"Página {i+1}"), ...) { Page = media }`;
   esa `SlideContent` es sólo para escenario y celular: **nunca** va a
   `GoLive`). `_presentationTab = null` al cargar canción, pasaje, medio en el
   centro o web. Después de `LoadMedia()`, si hay presentación abierta, se
   reconstruye conservando la página en vivo por id.
3. Tarjetas: `SlideItemViewModel.Page`, `PageImage` (**la miniatura**, no el
   PNG 4K; si no hay, `DecodePixelWidth≈220`), `IsPage`; en la plantilla un
   `Image` visible con `IsPage` en lugar del texto. Clic derecho en una página:
   sólo «Propiedades…». No se arrastran.
4. Proyectar: al comienzo de `GoLiveSlide`, `if (item.Page is {} page) { GoLivePage(index, page); return; }`.
   `GoLivePage` aplica la página como primer plano por `ApplyBackgroundCore` y
   llama a `_presentation.ClearText()`, **nuevo en Core**
   (`IPresentationService`/`PresentationService`: `CurrentSlide=null`, sin
   resaltado, estado Clear, `OnChanged()`; prueba de que después `ToggleClear`
   no vuelve a Content). Actualiza `LiveTitle`, `LiveSlideIndex`, `IsLive`,
   `Projection.SlideLabel = "Página n de N"`, `Projection.NextSlide`, estado.
   PREVIEW: `PreviewPageMedia` con la página siguiente y un
   `FirstNonNullConverter` para el `BackgroundMedia` del PREVIEW.
   El texto rápido (`GoLive`) también tiene que sacar un primer plano.
5. Playlist: `PlaylistItemType.Presentation = 3` (al final del enum; columna
   INTEGER, sin migración), `MediaId` = primera página como ancla, `Caption`
   = nombre de la pestaña. Actualizá **todos** los `switch` sobre el tipo
   (`grep "PlaylistItemType\."`), incluidos remoto y XAML (ícono «📑»).
   «Agregar a la playlist como presentación» en el menú de la pestaña y
   «➕ Playlist» junto al título de la grilla. Las flechas ya recorren las
   páginas y pasan al ítem siguiente en el borde: verificalo leyendo
   `Navigate`, `TryAdvancePlaylist` y `ProjectPlaylistItem`.

### D2. «En vivo» por sección

«cuando pusimos un versículo y pasamos a una ppt, sigue diciendo "en vivo"
desde la lista de versículos. Propongo que, dependiendo de si se está
mostrando la ppt, la sección completa quede en rojo o "en vivo"».

Hoy: el badge de la grilla es `ShowGridLiveBadge => IsProjecting && IsGridLive`
(`LiveTitle == GridTitle`); `LiveTitle` y el `IsLive` de las tarjetas sólo
cambian en `GoLiveSlide`, texto rápido, `ProjectMedia` (sólo desde la
playlist) y web. Un medio elegido en la barra entra por `ApplyBackgroundCore`
y no toca nada de eso.

Arreglo (derivado del estado real):
- Core `Presentation/OnAirSource.cs`: `enum OnAirSource { None, Song, Bible, QuickText, Media, Web, Presentation }`,
  `enum SlideOrigin`, y `Resolve(bool isProjecting, OutputState state, SlideOrigin liveSlide, MediaItem? background, bool backgroundIsPresentationPage)`:
  sin salida, Black o Logo → None; fondo web → Web; página de presentación →
  Presentation; primer plano o Clear con fondo → Media; Content → el origen de
  la diapositiva; Clear sin fondo → None. Pruebas.
- VM: `_liveSlideOrigin` (en `GoLiveSlide`: Bible si tiene versículos, Song si
  tiene sección; en texto rápido, QuickText); `OnAir` recalculado en
  `UpdateStateFlags`, `OnIsProjectingChanged`, `GoLiveSlide`/`GoLive`.
  Derivados `IsSongsOnAir`, `IsBibleOnAir`, `IsMediaOnAir`, `IsGridOnAir`,
  `LiveMediaId`. `ShowGridLiveBadge => IsGridOnAir`. En `ApplyBackgroundCore`,
  `LiveTitle = item.Name` si es primer plano; guardar aparte el título del
  texto para restaurarlo al volver.
- UI: borde rojo (`LiveBrush`, 2 px) en el panel de la grilla con
  `IsGridOnAir` y en el de medios con `IsMediaOnAir`; punto o «EN VIVO» rojo en
  las pestañas Canciones/Biblia; la tarjeta que estuvo en vivo pero ya no
  está en el aire queda con un borde apagado («último proyectado»); tarjeta de
  medio en el aire con borde rojo (`MediaCardState.IsLive`, refrescado como
  `RefreshMediaSelectionVisuals`).
- Celular: `RemoteSlide.IsLive = s.IsLive && IsGridOnAir`.
- Revisá cada `LiveTitle =` e `IsLive =` (grep).

### D3. Ver y cambiar las presentaciones desde el celular

«en modo mobile todavía no se visualizan las ppt para poder cambiar».

Con D1, una presentación abierta ya llena `Slides`, que el celular recibe.
Falta:
- `GET /api/thumb?pin=&id=` en `RemoteControlServer`: PIN como en
  `/api/state`; el id se resuelve en el hilo de UI con
  `IRemoteHost.GetThumbnailPath(int mediaId)` que devuelve **sólo** la
  miniatura de un medio conocido (nunca una ruta tomada de la request; no
  servir el PNG 4K); el archivo se lee fuera del hilo de UI;
  `WriteBytesAsync` con `image/png` y `Cache-Control: private, max-age=3600`.
- `RemoteSlide` suma `MediaId` (null si no es página). La página del celular
  dibuja `<img>` en las tarjetas de página **sin regenerarlas en cada
  consulta** (hoy `render` hace `innerHTML = ''`: comparar antes).
- Lista de presentaciones (pestañas marcadas) con un comando
  `presentation` que la abre (`OpenPresentation`) en la PC.
- «En vivo» del celular: con una página en el aire, mostrar su miniatura en
  lugar del texto viejo.
- JS en ES5, escapando textos con `textContent`, como ya hace la página.

### D4. GIF animados

Hoy un `.gif` es una imagen fija (WPF y VLC 3 sólo muestran el primer cuadro).
Al importar un `.gif` animado (`GifBitmapDecoder.Frames.Count > 1`) con ffmpeg
disponible (`YtDlp.FindFfmpeg()`), convertirlo a MP4 en la carpeta de datos
(`ffmpeg -i in.gif -movflags +faststart -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" out.mp4`)
y agregarlo como video en bucle y silenciado. Sin ffmpeg, queda como hoy.
La decisión de qué es «GIF animado» va en Core si se puede (con pruebas).

---

## Tramo E (Haiku) — atajos a la vista y tamaño de las miniaturas

Archivos: `App.xaml` (plantilla `DarkButton`), `MainWindow.xaml`,
`MainWindow.xaml.cs`, partials nuevos. Ningún punto necesita migración (los
ajustes son clave/valor). **Cada `_settings.Set` abre la base y guarda: nunca
guardar en cada movimiento de un slider** (demora de 400 ms). Números con
decimales en los ajustes: siempre `CultureInfo.InvariantCulture` (en es-AR el
decimal es la coma). Orden: E1, E2.

### E1. Modo aprendizaje / modo avanzado

«learning layout (con los atajos de teclado visibles) y complex layout (sin
los atajos visibles, disminuyendo la carga visual)».

Dónde se ven hoy: texto dentro de botones («Black  (F1)», «Clear  (F2)»,
«Logo  (F3)», «Sin fondo  (F4)», «Apagar salida  (Esc)» en el Setter de un
DataTrigger, «Proyectar ▶  (Ctrl+Enter)»); la etiqueta «Ctrl+clic: sumar una
segunda versión»; los textos de ayuda siempre visibles de Aviso al pie,
Resaltar, Spotify y Notas.
- `Controls/ShortcutHint.cs`: propiedad adjunta `ShortcutHint.Text`
  (string, "").
- `App.xaml`: `xmlns:controls`, recursos
  `<Visibility x:Key="ShortcutHintVisibility">Visible</Visibility>` y
  `LearningHintVisibility`. En la plantilla de `DarkButton`, el
  `ContentPresenter` pasa a un `StackPanel` horizontal con el
  `ContentPresenter` y un `TextBlock` (texto de `ShortcutHint.Text` del
  `TemplatedParent`, margen 8 a la izquierda, opacidad 0.6, letra 11) cuya
  `Visibility` es `{DynamicResource ShortcutHintVisibility}` y que se colapsa
  si el texto está vacío (así los botones sin atajo no cambian de tamaño).
  Estilo `LearningHint` (TextBlock) con
  `Visibility="{DynamicResource LearningHintVisibility}"`.
- `MainWindow.xaml`: `Content="Black" controls:ShortcutHint.Text="F1"`, etc.;
  en el DataTrigger de «Apagar salida», otro Setter para el atajo «Esc». La
  etiqueta de Ctrl+clic y los textos de ayuda con `Style="{StaticResource LearningHint}"`
  (sólo los que no tienen estilo propio).
- `MainWindow.Hints.cs` (partial nuevo): clave `ui.mode`, `learning` (el de
  siempre) o `complex`; `ApplyUiMode()` cambia los dos recursos en
  `Application.Current.Resources`. Restaurar al cargar (junto a
  `AttachLayoutPersistence`). Menú Ver, después de «Modo PC…»: «Diseño de
  aprendizaje (con atajos a la vista)» y «Diseño avanzado (menos texto, sin
  atajos)», tildados al abrir el menú. «Restablecer diseño» vuelve a
  aprendizaje.
- Quedan igual en los dos modos: los atajos de los menús, los tooltips, los
  textos de lista vacía y los mensajes de estado.
- Render PNG desde consola STA (crear `App` + `InitializeComponent()` sin
  `Run()` para tener los estilos): un botón `DarkButton` «Black» con atajo F1,
  con el recurso Visible y Collapsed: una imagen con «F1» y otra sin él; y un
  botón sin atajo del mismo tamaño que antes.

### E2. Tamaño de las miniaturas de diapositivas y medios

«que se pueda agregar una barra de tamaño para las miniaturas de los medios y
las diapositivas».

Hoy: tarjeta de diapositiva fija (`Width="220" Height="118"`, texto con
`TextFit.MaxFontSize="12"`, rótulo 10); tarjeta de medio fija (`98×56`,
`FramedMediaPreview DecodeWidth="240"`, nombre 9). Nada más depende de esos
tamaños (todo usa `ActualWidth`).
- Core `Presentation/CardZoom.cs`: `SlidesMin=0.6`, `SlidesMax=1.8`,
  `MediaMin=0.7`, `MediaMax=2.5`, `Step=0.1`, `Default=1.0`;
  `Parse(string?, min, max)` invariante (nulo o inválido → 1.0, recortado);
  `Format(double)` invariante `"0.0"`; `Nudge(current, direction, min, max)`
  redondea a 0.1. Pruebas, incluida la cultura es-AR («1,5» y «1.5»).
- `ViewModels/MainViewModel.CardZoom.cs` (partial nuevo):
  `SlideCardScale` y `MediaCardScale` con las calculadas
  `SlideCardWidth = 220*s`, `SlideCardHeight = 118*s`,
  `SlideCardFontSize = 12*s`, `SlideLabelFontSize = Max(9, 10*s)`,
  `MediaCardWidth = 98*s`, `MediaCardHeight = 56*s`,
  `MediaNameFontSize = Max(8, 9*s)`. Guardado con `DispatcherTimer` de 400 ms
  en `view.slides.zoom` y `view.media.zoom`. `InitializeCardZoom()` llamado
  junto a `InitializeWeb()`.
- XAML: tamaños de las dos tarjetas enlazados con `ElementName=SlideGrid` /
  `ElementName=MediaScroll` (el patrón ya se usa en esas plantillas);
  `DecodeWidth` de medios a 320. Slider de diapositivas en una fila nueva al
  pie de `SlidesPanel`, alineado a la derecha («🔍» + `Slider Width="110"`,
  `TickFrequency 0.1`, `IsSnapToTickEnabled`, nombre accesible «Tamaño de las
  diapositivas»; ojo, `WebPanelView` tiene `Grid.RowSpan="2"` y pasa a 3).
  Slider de medios abajo de la columna de pestañas (`DockPanel.Dock="Bottom"`).
- Ctrl+ruedita sobre la grilla o los medios cambia el tamaño
  (`PreviewMouseWheel` con `CardZoom.Nudge`, `e.Handled = true`).
- Render PNG de tarjetas a 0.6, 1.0 y 1.8.

---

## Tramo F — secciones colapsables y proyectar otra ventana

Archivos: `MainWindow.Layout.cs`, `MainWindow.xaml` (columna izquierda,
cabecera y cuerpo de medios, herramientas de texto), `App.xaml` (estilo
nuevo), `Controls/ProjectedView.xaml.cs`, `Views/OutputWindow.xaml.cs`,
archivos nuevos. Orden: F1, F2.

### F1. Secciones que se achican a íconos

«un sistema de cards colapsables, donde las distintas secciones puedan ser
colapsadas en mínimos íconos (igual que como está en playlist y
canciones/biblia)».

Hoy en `MainWindow.Layout.cs`: `_libraryCollapsed` y `_playlistCollapsed`
con sus claves, restaurar/guardar, `CaptureSizes` (no captura la playlist si
está colapsada), `ApplyLayout` (fila en `Auto`, cuerpo colapsado, chevron
▸/▾, splitter oculto), menú y reset. La barra de medios y las herramientas
de texto no se pueden achicar.
- Generalizar: `private sealed record Fold(string Key, Func<bool> Get, Action<bool> Set, Func<MenuItem> Item)`
  y una lista `Folds` que reemplaza las repeticiones (restaurar, guardar,
  tildar, resetear). Las dos de hoy entran en la lista. Estilo `FoldButton`
  en `App.xaml` basado en `DarkButton`.
- **Barra de medios → sólo la cabecera** (`layout.media.collapsed`): título
  «▸ MEDIOS» clicable; se esconden pestañas, tarjetas, barra de desenfoque y
  casillas; los botones 📥 ▶ 🌐 🔗 🎲 quedan. Fila en `Auto`, `MinHeight 0`,
  splitter oculto. **`CaptureSizes` no guarda el alto mientras está
  achicada** (si no, se guarda el alto de la cabecera). Igual con medios
  arriba.
- **Herramientas de texto → tira de íconos** (`layout.tools.collapsed`):
  cuerpo colapsado, fila en `Auto`; las pestañas muestran sólo el ícono
  (✍ Texto rápido, 📢 Aviso, 🖍 Resaltar, 📝 Notas, 🎵 Música, ⏱ Tiempos), con
  el nombre en `AutomationProperties.Name` y el tooltip; un clic en cualquier
  pestaña la expande en esa pestaña; botón ▾/▸ a la derecha.
- **Canciones y playlist → riel de íconos** (`layout.left.rail`): `LeftRail`
  (Border con estilo `Panel`, botones de 36×36 🎵 Canciones, 📖 Biblia,
  📋 Playlist) en la misma celda que `LeftPanel`; columna de 44 px, `LeftPanel`
  colapsado, splitter vecino desactivado (ojo con `_mirrored`); `CaptureSizes`
  no guarda el ancho. Cada botón sale del riel y abre lo suyo. Entrada: botón
  «⇤» en la cabecera de la biblioteca y menú Ver.
- Menú Ver: «Achicar la barra de medios», «Achicar las herramientas de
  texto», «Canciones y playlist como íconos». «Restablecer diseño» los apaga.
- LIVE, PREVIEW y la grilla no se achican.
- Verificación: build, y si se puede, PNG de `MainWindow` sin `Show()` con
  datos falsos en las combinaciones (medios achicada × arriba/abajo, riel ×
  espejado, herramientas achicadas). Si armar `MainWindow` es inviable, decilo
  en el informe.

### F2. Proyectar otra ventana de Windows

«agregar opción de proyectar otra ventana».

Precedente: la web en vivo (`MainViewModel.Web.cs` arma un `MediaItem` con
id negativo, `Type=Web`, primer plano, y lo aplica con
`ApplyBackgroundCore`; `Services/WebLiveFeed.cs` publica un `ImageSource` que
`ProjectedView` dibuja; `UpdateStateFlags` apaga `IsWebLive` cuando entra
otro fondo). El SDK del TFM (19041) trae `Windows.Graphics.Capture`
(`GraphicsCaptureItem`, `GraphicsCapturePicker`, `GraphicsCaptureSession`
con `IsSupported()` e `IsCursorCaptureEnabled`,
`Direct3D11CaptureFramePool.CreateFreeThreaded`,
`SoftwareBitmap.CreateCopyFromSurfaceAsync`, `InitializeWithWindow`). **No
sumes paquetes NuGet.** No están `IsBorderRequired` ni `TryCreateFromWindowId`
(Windows 11): el borde amarillo alrededor de la ventana capturada queda (sólo
se ve en la pantalla del operador).

1. Core (con pruebas):
   - `MediaType.Window = 4` (INTEGER: sin migración). `Path` =
     `"proceso.exe|Título"`.
   - `Core/Capture/WindowTarget.cs`: `Format`, `TryParse`, `Best(candidatos, objetivo)`
     (mismo proceso y título; si no, mismo proceso y título que contiene el
     guardado; si no, el único del proceso; si no, null).
   - `Core/Capture/WindowCandidates.cs`: `Filter(IEnumerable<RawWindow>, int ownProcessId)`
     que descarta invisibles, ocultas por DWM, de herramientas, con dueño, sin
     título o de tamaño cero, y **las de este proceso** (si no, espejo
     infinito); ordena por título.
   - `PosterPolicy`: `Window` sin póster.
2. `Services/IWindowCapture.cs` + `Services/WindowCapture.cs`:
   - D3D por P/Invoke `D3D11CreateDevice` (hardware, con WARP de respaldo,
     flag BGRA 0x20) → `IDXGIDevice` {54ec77fa-1377-44e6-8c32-88fd5f44c84c} →
     `CreateDirect3D11DeviceFromDXGIDevice` → `IDirect3DDevice`. Liberar los
     punteros.
   - Ítem: `IGraphicsCaptureItemInterop` (Guid 3628E81B-3CAC-4C60-B7F4-23CE0E0C3356,
     `CreateForWindow`/`CreateForMonitor`) desde la fábrica
     `Windows.Graphics.Capture.GraphicsCaptureItem`; IID de IGraphicsCaptureItem
     79C3F95B-31F7-4EC2-A464-632EF5D30760. Alternativa: `GraphicsCapturePicker`
     con `InitializeWithWindow`.
   - Sesión: pool free-threaded B8G8R8A8 de 2 cuadros, `IsCursorCaptureEnabled`
     según el ajuste `window.cursor`, `item.Closed` → evento `Ended`.
   - Por cuadro (hilo de trabajo): si hay una conversión en curso, descartar;
     tope 30 fps (12 en bajo rendimiento); si cambió `ContentSize`,
     `Recreate`; copiar a un `byte[]` reutilizado y en el hilo de UI
     (`DispatcherPriority.Render`) escribir en un `WriteableBitmap` Pbgra32
     reutilizado. Nada pesado en el hilo de UI (el vigía `UiWatchdog` lo
     detectaría).
   - `Stop()` y la salida del programa liberan pool, sesión, ítem y bitmaps.
   - `Services/WindowCaptureFeed.cs`, espejo de `WebLiveFeed` (con
     `Snapshot(width)` y `SetSourceForTest`).
3. UI: botón «🪟 Ventana» en la cabecera de medios, junto a «🌐 Web», que abre
   `Views/WindowPickerWindow.xaml` (lista de ventanas con título, proceso e
   ícono; «Elegir con la ventana de Windows…» abre el picker; casilla
   «Mostrar el puntero»). `ViewModels/MainViewModel.Window.cs`:
   `ProjectWindow()` → captura → `ApplyBackgroundCore(new MediaItem{Id=-3_000_000, Type=Window, Scaling=Fit, Behavior=Foreground, ...})`,
   `LiveTitle = "🪟 {título} (en vivo)"`; `IsWindowLive` en
   `UpdateStateFlags` (al pasar a false, `Stop()`); `Ended` →
   `ClearBackground()` y aviso; minimizada: queda el último cuadro y aviso.
   Si `!GraphicsCaptureSession.IsSupported()`, el botón explica por qué no.
4. Dibujo: en `ProjectedView`, `isWeb` pasa a `isFeed = Type is Web or Window`
   y `WebFrame()` a `FeedFrame()`; engancha el `SourceChanged` que
   corresponda y reusa el timer de bajo rendimiento. `OutputWindow`: `Window`
   fuera del relleno. Revisá cada `switch`/`if` sobre `MediaType`
   (`grep "MediaType\."`) para que `Window` no arranque VLC ni YouTube ni pida
   miniatura. (El tramo A también toca `ProjectedView`: tocá lo mínimo.)
5. Guardar la ventana como tarjeta de medios y en la playlist: **segunda
   fase**, sólo si lo anterior quedó firme.
- Verificación: pruebas de Core; consola STA de humo que enumera ventanas,
  elige una de otro proceso (por ejemplo el Explorador), captura 3 s
  bombeando el Dispatcher, cuenta cuadros (fps) y guarda el último a PNG —
  **sin mostrar ninguna ventana**; PNG de `ProjectedView` con
  `WindowCaptureFeed.SetSourceForTest`. Actualizar `docs/privacidad.md` (la
  captura no sale de la PC) y `docs/guia-de-uso.md`.

## Para más adelante (necesita decisiones de Alexis)

- **Buscador de imágenes y GIFs (Unsplash / GIPHY).** Necesita que Alexis
  registre las claves. GIPHY prohíbe guardar los GIF (sólo se pueden
  reproducir por internet) y su clave de producción es paga; Unsplash exige
  atribución y avisar cada descarga. Detalle en el informe de Opus del 13/10
  (anotado en `pendientes.md`).

## Informe final de cada tramo

Por punto: commit, qué hiciste, cómo lo verificaste, qué queda «para probar»
y qué tocarías de otro tramo.
