# Tanda 16 (Sonnet) — Biblia y canciones

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (todas). Cómo se traduce
cada pantalla: [guia-wpf-a-avalonia.md](guia-wpf-a-avalonia.md) (**leela
entera antes de empezar**: se corrigió con lo que dejó la tanda 15). Contexto:
[plan-mac-fase-3.md](plan-mac-fase-3.md).

**Rama:** la de esta sesión, cortada de `mac` (con la tanda 15 revisada).
**Tamaño:** mediano a grande. **Una sola sesión.** No arranques la tanda 17.

Lo que sigue se comprobó contra el código el 26/11/2026. Si algo no coincide,
gana el código y se anota en el informe.

## Para qué

Que en la Mac funcionen las pantallas de la Biblia, las canciones y Ajustes,
que hoy responden «Esto todavía no está en la versión de Mac», con el mismo
aspecto que en Windows.

## 1. Mudar primero la lógica que vive en WPF

Pasa a `EcclesiaCast.Presentation`, con pruebas en `Presentation.Tests`, y WPF
la usa **sin cambiar lo que hace ni cómo se ve**. Un commit por punto. Antes de
tocar WPF, sumá las pantallas al banco de WPF «como están hoy», en un commit
aparte, para tener con qué comparar (la tanda 15 hizo lo mismo).

1. **`SongEditorWindow`:** la lista de temas («(Tema principal)» y «(Tema propio
   de esta canción)»), validar título y letra (con su mensaje y a qué campo va el
   foco), conservar el diseño de cada diapositiva por posición (`StyleJson`) y
   armar la `Song`. Los botones «Coro», «Estrofa»… ya usan `LyricsTags.Insert`
   (común).
2. **`BibleImportWindow`:** nombre y abreviatura obligatorios (mensaje y foco) e
   idioma `es` si queda vacío.
3. **Paquetes de Biblias:** no es sólo `BiblePackageWindow` (grupos por idioma,
   «Marcar todas / ninguna» sobre **las que se ven**, el número en el botón y
   «Marcá al menos una versión»). `Services/BiblePackageDialogService.cs` arma
   las filas de exportar, importar y bajar: textos, qué viene tildado, tamaños
   en KB y MB, el orden por idioma y qué se devuelve. Todo eso es común.
4. **`BibleFormatHelpWindow`:** guardar los tres ejemplos (nombre, filtro,
   escribir sin BOM, el «✓ Guardado» y el aviso si falla), con `IDialogs`; y
   copiar las instrucciones con `IClipboardText`, con su mensaje si no se pudo
   (en WPF salta una `COMException`). El texto «(Ctrl+V)» en la Mac es «(⌘V)».
5. **`GlobalSearchWindow`:** esperar 180 ms sin teclear antes de buscar
   (`IUiTimer`), el texto del resumen, que quede elegido el primero, moverse con
   ↑ y ↓ sin pasarse de los bordes y qué se devuelve al aceptar.
6. **Copia de seguridad:** `Services/BackupDialogService.cs`. No es una pantalla
   y ninguna otra tanda la tiene, pero hoy en Mac es `NotOnMacYet` y decide
   mucho: mirar el archivo, rechazar una copia de una versión más nueva, la
   pregunta destructiva, preparar la restauración y reiniciar. Va a un servicio
   común que implemente `IBackupDialog` con `IDialogs` e `INotices`; reiniciar y
   cerrar entran como delegados. Antes de cerrarse tiene que dejar
   `FeedbackSession.Quiet` (en WPF, `FeedbackCoordinator.Quiet`) en verdadero.
   En la Mac, volver a abrir el programa queda sin probar: decilo.
7. **`SettingsWindow`:** hoy pide la `MainWindow` entera en el constructor, así
   que ni el banco de WPF la puede armar. Cambiá eso por una interfaz chica con
   lo que usa: diseño de aprendizaje o avanzado (leer y cambiar), reiniciar el
   sonido, abrir las transiciones, elegir el color de los videos nuevos y abrir
   «Seguir la canción». En WPF la cumple `MainWindow` con los métodos que ya
   tiene. Lo que decide (reiniciar el sonido y su línea de estado; guardar el
   diseño con `MainWindowSettings`) va a la parte común. El menú de la tanda 19
   (`MenuRequest.RestartSound`, `.LearningMode`, `.AdvancedMode`) tiene que
   poder usar lo mismo.

`TrashWindow`, `SongImportWindow` y `WebSongSearchWindow` ya tienen ViewModel.
Mirá qué les quedó en el `.xaml.cs` (por ejemplo, cuándo se prenden los botones
de la papelera, o qué pasa si se cierra una ventana mientras importa o busca) y
mudalo si decide algo.

## 2. Traducir las pantallas (9)

Cada una con su servicio en `Desktop/Services`, su línea en `Composition.cs` en
vez de `NotOnMacYet` y su prueba en el banco de pantallas de los dos lados. Un
commit por ventana.

| Pantalla | Interfaz |
|---|---|
| Importar Biblia | `IBibleImportDialog` |
| Paquetes de Biblias (exportar, importar y bajar) | `IBiblePackageDialog` |
| Ayuda de formato | `IBibleFormatHelpDialog` (devuelve si pidió importar) |
| Editor de canción | `ISongEditor` |
| Importar canciones | `ISongImportDialog` |
| Buscador | `IGlobalSearchDialog` |
| Papelera | `ITrashDialog` |
| Buscar letra en internet | `IWebSongSearchDialog` |
| Ajustes | La abre la tanda 19 (`MenuRequest.Settings`): dejá hecha la forma de abrirla |

Más la línea de `IBackupDialog` (punto 1.6).

### Importar canciones

- **Holyrics:** se piden **varios archivos a la vez** (`.json` y `.txt`) con
  `IDialogs.OpenFiles`. En Windows empieza en `C:\Holyrics` si existe y si no en
  Documentos; en la Mac, en Documentos. `OpenFileRequest` **no tiene carpeta de
  inicio**: sumale una opcional y que `AvaloniaDialogs` la use
  (`SuggestedStartLocation`, esperando la respuesta como el resto). La ventana
  de WPF puede seguir con su diálogo propio; lo que no puede es cambiar dónde
  empieza.
- Elegir carpeta es `IDialogs.PickFolder`; abrir una carpeta,
  `IShell.OpenFolder`.
- `ImportAsync` pide una forma de dejar respirar a la pantalla: en Avalonia,
  `Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background)`.
- Mirá qué ofrece `DiscoverAll` en una Mac (EasyWorship es sólo de Windows).
- **Ojo en las pruebas:** al abrirse busca solo en Documentos y en la carpeta de
  EasyWorship. En el banco y en las pruebas van **carpetas temporales** con
  `IPlatformPaths` falso: nunca las de esta computadora (guía, sección 5).

### Importar Biblias

La 1.5.0 sumó las Biblias XML de OpenSong y la ayuda de formato nueva (con su
tercer ejemplo). El importador es común: comprobá que en la Mac se llegue a
elegir esos archivos y que la ayuda de Avalonia tenga los tres. La ventana de
WPF limita su alto al de la pantalla para que no queden botones afuera
(`SystemParameters.WorkArea`): hacé lo mismo en Avalonia.

### Buscar letra en internet

La búsqueda completa de musica.com usa un navegador invisible de Windows
(`MusicaComBrowserSearch`, WebView2). En Mac `BrowserSearch` queda en `null`,
como en una PC sin WebView2: comprobá con una prueba que así busca igual y que
los textos no prometen lo que no hay. La dirección de la fuente se abre con
`IShell.OpenUrl`.

### Ajustes

- **No es modal** (`Show`, no `Modal.Run`), hay **una sola** y si ya está abierta
  se trae al frente. Enter y Esc la cierran.
- «Cambiar las transiciones…» usa `MenuDialogs.ShowTransitions` (ya hecho); el
  diálogo tiene que quedar encima de Ajustes.
- «Color de fondo…» pide `IColorPicker`, que es de la tanda 17: dejalo
  enganchado a la interfaz.
- En la Mac **no se muestra lo que no hay**: «Seguir la canción»
  (`IPlatformInfo.IsAvailable(Feature.FollowSong)`; `IsListeningAvailable` solo
  no alcanza) ni lo de buscar actualizaciones (`IUpdates` es `Silent` hasta la
  fase 4; no hay una `Feature` para eso: hacela, con Windows igual que hoy). El
  contador anónimo y el nombre de la iglesia sí van.
- Textos que nombran a Windows («la salida de audio de Windows», lo de los
  videos de YouTube): en la Mac, adaptados, sin cambiar los de Windows.
- Las pastillas usan `TabButton` con `GroupName` (`SettingsSections` y
  `UiMode`): mirá lo de `GroupName` en la guía.
- Para el banco hace falta un `MainViewModel`. `Presentation.Tests` ya arma uno
  con todo falso (`ViewModelWorld.cs`): reusalo (por ejemplo, enlazando el
  archivo) en vez de escribir otro.

## 3. El teclado

Prueba de **Enter** y **Esc** en cada diálogo, con teclas de verdad
(`DialogKeyTests.cs`). En particular:

- el buscador: Enter y ↑ ↓ con el cursor en la caja, Enter y doble clic en la
  lista;
- el editor de canción: Enter escribe un renglón en la letra y **no guarda**
  (en WPF, mirá qué botones son `IsDefault`);
- buscar letra: Enter en la caja de búsqueda busca;
- los botones «Coro», «Estrofa»…: que Ctrl+Z (⌘Z) los deshaga, como en WPF. La
  `TextBox` de Avalonia no se comporta igual al reemplazar la selección:
  probalo.

## No entra

- Corrector de ortografía (fase 5): las cajas de Mac no marcan faltas.
- Diseñador de canciones y diapositiva vacía, temas y selector de color: tanda
  17. Pixabay: tanda 18. Conectar el menú: tanda 19.
- Actualización (`UpdateWindow`): fase 4.

## Terminado

- Compila sin advertencias. Pasan todas las pruebas: núcleo, parte común y Mac,
  más las nuevas. (Al empezar: 1.762, 329 y 134.)
- El banco de render de WPF da idéntico y el de Avalonia sigue en 195 de 195.
- Cada pantalla tiene su imagen de WPF y de Avalonia lado a lado, y la miraste.
  Lo que queda distinto va en el informe, con el porqué.
- Windows funciona igual: las pruebas muestran que lo mudado no cambió, y las
  imágenes de WPF del banco de pantallas son iguales antes y después.
- Ninguna pantalla nueva decide nada que no esté en su ViewModel o servicio.
- La compilación en una Mac queda sin comprobar hasta que se suba: decilo.
- `CHANGELOG.md`, «Sin publicar»: una línea por lo que se nota.

**Informe final**, como piden las reglas, más: qué de la guía no sirvió o
faltaba (para corregirla antes de la tanda 17).
