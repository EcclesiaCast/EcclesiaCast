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
