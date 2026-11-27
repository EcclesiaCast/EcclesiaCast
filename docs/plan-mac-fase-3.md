# Fase 3: lo que la 1.5.0 le dejó pendiente a la versión de Mac

Se escribió el 26/11/2026, al traer la 1.5.0 a `mac`. Las tandas de la
fase 3 están en [plan-mac.md](plan-mac.md), sección 6. Acá va sólo **lo que
sumó la 1.5.0**, para que ninguna tanda se lo olvide.

Windows quedó con todo lo de la 1.5.0. La lógica de esas funciones ya está en
la parte común (`EcclesiaCast.Core` y `EcclesiaCast.Presentation`) y compila
sin WPF. Lo que falta en Mac es lo que **dibuja** o lo que **depende de
Windows**.

## Salida y escenario (no tiene tanda: agregarlo antes de la puerta 3 o a la fase 3)

| Falta | Hoy en Mac | De dónde copiarlo |
|---|---|---|
| **Placas de nombre en la salida de Avalonia** (la tarjeta, su imagen o video, cómo entra y sale, «sólo proyector» o «sólo transmisión») | No se dibujan en la salida. La página de OBS sí las muestra (es común) | `App/Controls/SlideView.NamePlate.cs`, la capa en `SlideView.xaml` y `PlaceNamePlate` en `ProjectedView.xaml.cs` |
| **Salida en una ventana** («⧉ En una ventana»), con 16:9, 4:3 y 9:16 | No aparece en la lista de pantallas (`Feature.FloatingOutput` da falso en `AvaloniaPlatformInfo`). La cuenta del tamaño ya es común (`Core/Displays/FloatingOutput.cs`) | `App/Views/OutputWindow.Floating.cs` |
| **YouTube rechazado** (pantalla en negro y ofrecer la descarga) | La salida de Mac no tiene reproductor de YouTube: `YouTubeRefused` nunca avisa | `App/Controls/YouTubeSurface` (va con la fase 6, YouTube en Mac) |

Para las placas, antes conviene sumar al banco de render casos con placa, así
se comparan contra WPF como la letra.

## Ventanas y diálogos

| Falta | Interfaz común que ya existe | Tanda |
|---|---|---|
| Diseñador de placas («✏ Diseñar…») | `INamePlateDesigner` (en Mac responde «todavía no está») | 17 |
| Importar canciones de **Holyrics** | `SongImportViewModel.ReadHolyricsFiles`. Falta pedir los archivos (varios a la vez) en la pantalla; en Mac la carpeta `C:\Holyrics` no existe: empezar en Documentos | 16 |
| **Diapositiva nueva vacía** en el diseñador de canciones | `SongDesignerViewModel.InsertSlideAfter` | 17 |
| **Cambiar el PIN** del celular y el **PIN de OBS** | `IRemoteControlDialog.Show(server, pinChanged)` y `IBroadcastDialog.ShowPages(server, obsPinChanged)`. La ventana de Mac recibe `pinChanged` y todavía no lo usa | 18 |
| Aviso de la dirección fija de OBS y vMix (`StableNote`) | `RemoteControlServer` | 18 |
| Importar Biblias XML de OpenSong y la ayuda de formato nueva | El importador es común | 16 |
| El ícono de «va encima de todo» en los avisos (`Notice.OnTopIconMark`) y en las pestañas de medios | La marca viaja en el texto del aviso: la ventana de avisos de Mac tiene que cambiarla por el ícono (hoy se vería escrita) | 15 y 21 |
| Comentarios: ahora el texto es obligatorio | — | 15 |

## Ventana principal

| Falta | Qué ya está en la parte común | Tanda |
|---|---|---|
| Barra de las diapositivas: candado 🔒, buscar en la letra, deshacer y rehacer, fondo de la canción | `MainViewModel.SongTools` y `.SongBackground` | 20 |
| Playlist y buscadores sin fijarse en tildes | Común | 20 |
| «Dividir versículos largos» y «🖼 Fondo de la Biblia» | `MainViewModel.VerseSplit` y `.BibleBackground` | 21 |
| Pestaña **Placas** | `MainViewModel.NamePlates` | 21 |
| **Música de YouTube**, repetir y el menú de la lista de audios | `MainViewModel.YouTubeMusic` y `.Audio`, `YtDlp.Music`, `RangedHttpStream`. Falta además yt-dlp y su programa de apoyo **para Mac** (hoy son los de Windows) | 21 |
| Un solo volumen debajo del EN VIVO | `PlayerSound`, `MainViewModel.OutputVolume` | 19 |
| **Tutoriales** (menú, tarjetas, marco que titila, dibujos del celular y abrir ventanas reales al lado) | Los pasos y el recorrido (`Core/Ui`). Las ventanas las arma cada interfaz: en Windows, `App/Services/TutorialWindows.cs`, con `RemoteForTutorial`, `KeepRemotePin` y `KeepObsPin` | 21 |

Los textos de los tutoriales nombran botones de Windows y atajos con Ctrl:
revisarlos para Mac (Cmd).

## Para la tanda 19: conectar `MenuDialogs` y `FeedbackDialogs`

La tanda 15 dejó hechos y registrados en `Composition.cs` los dos servicios, y
**todavía no los llama nadie**. El modelo es `App/App.xaml.cs` (arranque y
`OnExit`) y `App/MainWindow*.cs`.

1. **Del menú:** `MenuRequest.About` → `MenuDialogs.ShowAbout()`,
   `.Transitions` → `ShowTransitions()` y `.Feedback` →
   `FeedbackDialogs.ShowFromMenu()`.
2. **La primera vez que se abre el programa:** «Acerca de» sola, una única vez
   (`OneTimeNotice.ShouldShow`, clave `app.about.seen`, que hoy es una constante
   de WPF; con `firstRun: true`). En ese orden con lo demás, como en Windows.
3. **Pedir `FeedbackDialogs` al arrancar**, aunque no se muestre nada. Al crearse
   `FeedbackSession` **se cuenta cada apertura** y empieza a correr el reloj de
   la sesión. Si se pide recién al abrir el menú o al cerrar, las aperturas y los
   minutos salen mal y el aviso del cierre no aparece nunca. (Pedirlo después de
   saber que no es una segunda copia del programa.)
4. **El cierre anterior:** el programa de Avalonia todavía no usa `CrashMarker`
   (de `Core`). Va `Begin()` al arrancar, `End()` al salir (también cuando el
   sistema se apaga) y, con la ventana ya en pantalla,
   `FeedbackDialogs.AskAfterCrash(lastRunCrashed)`.
5. **Antes de cerrar la ventana principal:** `AskOnClosing()`. Abre un diálogo
   desde adentro de `Closing`, que en Avalonia no está probado: comprobar con una
   prueba que la ventana espera la respuesta, que un segundo pedido de cierre
   mientras tanto no abre dos y que después se cierra. Cuando el que cierra es el
   sistema que se apaga (`CloseReason`), no se pregunta.
6. **Registrar el fin de la sesión:** al salir, `FeedbackSession.RecordEnd()`
   **una sola vez**, que suma los minutos de uso. `FeedbackDialogs` no lo expone:
   pedir `FeedbackSession` o sumarle el método.
7. **No preguntar cuando el programa se cierra solo:** poner
   `FeedbackSession.Quiet = true` **antes** de cerrarlo para restaurar una copia
   (la tanda 16 muda esa lógica) o para actualizarse (fase 4), y en cualquier
   otro cierre que no pida el operador.
8. El portapapeles (`AvaloniaClipboardText.Window`) hoy lo apunta `App.axaml.cs`
   a la ventana de prueba: la ventana del operador tiene que seguir haciéndolo.

## Cosas de Mac (tanda 22)

- **Una sola copia del programa.** En Windows es `App/Services/SingleInstance.cs`
  (usa piezas de Windows). Un `.app` abierto desde el Finder ya se comporta
  así, pero hay que probarlo, también abriéndolo desde la terminal.
- **Submenús para zurdos** (`SubmenuPlacement`): es un arreglo propio de WPF;
  no hace falta.

## Cosas que dependen de la máquina y nadie probó en una Mac

- **Volumen general.** Windows lo pone en la sesión de sonido del programa
  (`IProgramVolume`, `WindowsProgramVolume`). En Mac no hay nada así
  (`ProgramVolume` queda vacío): cada reproductor baja su propio volumen. Hay
  que oírlo en la Mac con un video y con la música a la vez; la curva puede no
  ser igual para los dos.
- **Cookies de YouTube.** Windows le pasa a yt-dlp la sesión de su navegador
  para los videos que piden cuenta (`IYouTubeSession`). En Mac no hay
  navegador propio todavía (`NoYouTubeSession`): esos videos van a fallar.
- **Contador anónimo de uso.** En Windows cuenta sólo la copia instalada
  (mira si está el desinstalador). Fuera de Windows ese control no se hace:
  decidir en la fase 4 cómo se reconoce la copia instalada del `.dmg`.
- **Carpetas de la música.** `~/Music/EcclesiaCast` y
  `~/Library/Caches/EcclesiaCast/audio-youtube` (`MacPlatformPaths`).
- **Audio de YouTube mientras se baja** (`PlayStream`): probar que LibVLC.Mac
  lo reproduzca.
