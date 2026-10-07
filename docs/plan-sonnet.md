# Plan de trabajo para Sonnet

Este documento es el encargo completo. Leelo entero antes de tocar nada.
Trabajás solo: Alexis (el dueño del proyecto) no está mirando y **no maneja
inglés**. No le podés preguntar nada en el medio. Cuando haga falta decidir,
elegí lo más simple que cumpla el pedido y anotalo en el informe final.

El pedido original de Alexis, punto por punto, está en
[pendientes.md](pendientes.md), sección 2. Este plan dice en qué orden
hacerlos, dónde está cada cosa en el código y cómo probarlos.

## 1. El proyecto en dos minutos

- **EcclesiaCast**: software libre de proyección para iglesias. WPF sobre
  .NET 8, Windows. Solución `EcclesiaCast.slnx`:
  - `src/EcclesiaCast.Core`: modelo y lógica sin interfaz (Biblia,
    canciones, playlists, presentación, temas). Acá van las pruebas.
  - `src/EcclesiaCast.Data`: SQLite con EF Core (`Persistence/*Repository.cs`).
  - `src/EcclesiaCast.App`: la aplicación WPF. Casi todo pasa en
    `ViewModels/MainViewModel.cs` (enorme, CommunityToolkit.Mvvm con
    `[ObservableProperty]` y `[RelayCommand]`), `MainWindow.xaml`,
    `MainWindow.xaml.cs` y `MainWindow.Layout.cs`.
  - `tests/EcclesiaCast.Core.Tests`: xUnit, 211 pruebas, todas pasan.
- La diapositiva se dibuja con `Controls/SlideView` sobre un lienzo virtual
  de 1920×1080. El tamaño de letra lo calcula `Controls/SlideTextFit.cs`
  (compartido con el armado de diapositivas: no lo dupliques).
- La ventana de escenario usa `Controls/StageView` y `Services/StageOptions.cs`.

## 2. Reglas de trabajo

**Rama y commits**
- Trabajá en la rama `beta6` (ya está creada y al día). No toques `main`.
- **No hagas push** ni publiques nada. No armes releases.
- Un commit por tema terminado (por ejemplo "Buscador", "Medios").
  Mensaje en inglés, en imperativo, explicando el porqué, y al final:
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

**Idioma**
- Todo lo que ve el usuario (botones, mensajes, tooltips), el `CHANGELOG.md`
  y `docs/` van en **castellano rioplatense** («elegí», «tocá», «podés»).
- Los comentarios del código van en inglés, como el resto del código:
  explican el porqué, no el qué. Copiá la densidad y el tono de los que hay.

**Al cerrar cada punto**
- Borralo de `docs/pendientes.md`.
- Anotalo en `CHANGELOG.md`, sección `[Sin publicar]`, en `Agregado`,
  `Cambiado` o `Corregido`, con el mismo estilo de las entradas que hay
  (negrita para el título, una o dos frases claras para un no técnico).

**Pruebas**
- Siempre: `dotnet build` sin errores ni advertencias nuevas, y
  `dotnet test tests/EcclesiaCast.Core.Tests` todo en verde.
- Lo que sea lógica (filtros, búsquedas, normalización de texto, modelos)
  ponelo en `EcclesiaCast.Core` y escribile pruebas. Los nombres de las
  pruebas siguen el estilo `Una_frase_que_dice_lo_que_pasa`.
- **No abras la aplicación ni ninguna ventana.** Alexis está usando esta
  misma PC para su trabajo y las ventanas que aparecen solas (la copia de
  desarrollo, la salida a pantalla completa, clics automáticos) lo
  confunden. Nada de computer-use ni de arrancar `EcclesiaCast.App.exe`.
  Probá con `dotnet build`, `dotnet test` y, para lo visual, dibujando
  controles a PNG sin mostrarlos (un programa de consola STA que crea el
  control, le hace `Measure`/`Arrange` y lo renderiza con
  `RenderTargetBitmap`; así se verificó `SlideView`). Lo que sólo se pueda
  probar con la app abierta, anotalo en el informe como «para probar».
- Lo que sigue queda para cuando Alexis diga que la PC está libre:
  - **Nunca uses los datos reales** de `%APPDATA%\EcclesiaCast`. Copiá
    `ecclesiacast.db` (y `-wal`/`-shm` si están) a una carpeta de trabajo
    temporal y arrancá la versión de desarrollo con la variable
    `ECCLESIACAST_DATA_DIR` apuntando ahí:
    ```powershell
    $env:ECCLESIACAST_DATA_DIR = "<carpeta con la copia>"
    & "src\EcclesiaCast.App\bin\Debug\net8.0-windows10.0.19041.0\EcclesiaCast.App.exe"
    ```
  - Puede haber un EcclesiaCast **instalado** abierto
    (`C:\Program Files\EcclesiaCast\...`). Es el de Alexis: no lo cierres ni
    lo toques. Cerrá sólo los procesos de desarrollo que abriste vos.
  - La PC tiene **una sola pantalla**: si proyectás algo, la salida de
    desarrollo tapa todo. `Esc` la apaga. Para leer la ventana del operador
    con la salida prendida, usá UI Automation desde PowerShell.
  - Para leer la base sin abrir la app: `node` (v24) trae `node:sqlite`.
    No hay Python instalado.

**Trampas que ya encontramos (no las repitas)**
- En los `ListBox`, un `MouseBinding` de doble clic **nunca se dispara**: la
  fila se queda con el clic. Atendé el doble clic en
  `PreviewMouseLeftButtonDown` con `e.ClickCount == 2` (así ya lo hacen la
  playlist, la lista de canciones y los resultados de la Biblia).
- Los archivos de `EcclesiaCast.Core` tienen fin de línea CRLF: los
  reemplazos con `sed`/`perl` que esperan `\n` fallan en silencio. Para
  editar, usá la herramienta de edición.
- Con `perl` y `|` como delimitador, `\|` se vuelve alternancia y el
  reemplazo cae en cualquier lado. Mejor no.
- `SavePlaylist()` recarga la lista entera (objetos nuevos): no guardes
  referencias a ítems viejos después de llamarla.

## 3. Qué hacer, en este orden

Cada punto dice qué pidió Alexis, dónde mirar y cómo darlo por terminado.

### 3.1 Buscador (Ctrl+K)

**a) Decir de qué versión es cada versículo encontrado.**
Hoy `SearchEverywhere` (en `MainViewModel.cs`) busca sólo en la versión
elegida (`SelectedBibleVersionOption`). Alexis quiere que busque en
**todas** las versiones instaladas y que cada resultado muestre la versión,
por ejemplo «Salmos 55:22 · RVR1960». Al elegir un resultado tiene que abrirse
**en esa versión** (`SelectSoloVersion`), no en la que estaba elegida.
Mirá también `Views/GlobalSearchWindow.xaml(.cs)` y el `SearchHit`.
Poné un tope por versión para que una no tape a las demás.
Prueba: «echa sobre» tiene que traer Salmos 55:22 con su versión.

**b) Que los saltos de línea no corten la búsqueda en las letras.**
`SongRepository.Search` usa `LIKE '%texto%'` sobre `SongSections.Text`, que
tiene saltos de línea. «las cadenas de la muerte» no encuentra *Hay
Libertad* porque la letra dice «las cadenas⏎de la muerte». Hacé que la
comparación trate saltos de línea y espacios repetidos como un solo
espacio, tanto en el texto como en lo que se escribe (EF Core traduce
`string.Replace` a `replace()` de SQLite). Lo mismo para el texto de la
Biblia si tiene saltos. Si la normalización es una función, va en Core
con pruebas. Afecta al buscador de Ctrl+K y al de la lista de canciones.

### 3.2 Medios: la vista previa de los Anuncios

Alexis: «después de cerrar y abrir el programa la vista previa de las
imágenes en anuncios no me aparece». Es un bug: reproducilo con la copia de
la base (agregá una imagen a la pestaña Anuncios, cerrá, abrí) y encontrá
la causa antes de arreglar. La barra de medios muestra `ThumbnailPath`
(`MainWindow.xaml`, plantilla de la barra de medios); mirá cómo se arma esa
miniatura para imágenes en `Services/MediaThumbnails.cs` y al importar.
Ojo: en la base real hay un video con `Category` vacía; fijate si tiene que
ver (una categoría vacía no aparece en ninguna pestaña).

### 3.3 Ventana

**a) Sacar dos botones de la barra de arriba:** el ⏱ (cuenta regresiva) y
el 💾 (copia de seguridad). Los dos ya están en los menús
(Proyección → Cuenta regresiva…; Archivo → Guardar una copia… / Restaurar…).
El ⏱ hoy se pinta de color mientras la cuenta corre: que el ítem del menú lo
muestre (por ejemplo con una tilde o el texto «Detener la cuenta regresiva»).
Lo que hacía `Backup_Click` (guardar o restaurar) tiene que seguir
alcanzable desde el menú.

**b) Ayuda → Acerca de.** Hoy es un `MessageBox` en
`MainWindow.Layout.cs` (`About_Click`). Hacé una ventanita propia, oscura
como las demás (mirá `Views/TextPromptWindow` para el estilo), con:

> **EcclesiaCast** (versión)
> Software libre de proyección para iglesias.
> Desarrollado por **Alexis Pérez** · Montevideo, Uruguay.
> Hecho para bendecir a otras iglesias con lo que recibimos.
> «De gracia recibisteis, dad de gracia.» — Mateo 10:8
> Licencia GPL-3.0 · github.com/EcclesiaCast

El enlace a GitHub, que se pueda abrir. Más adelante va a haber una forma
de colaborar: no la agregues ahora.

### 3.4 Canciones: un solo «➕»

Arriba de la lista de canciones hay un botón «📥 ▾» (menú de importar) y un
«➕» (nueva canción). Alexis quiere **un solo ➕** que, al tocarlo, ofrezca:
**Crear desde cero**, **Buscar en internet…**, **Traer de ProPresenter…** e
**Importar archivo (.txt, .pro)…**. Los comandos ya existen
(`NewSongCommand`, `SearchSongsOnlineCommand`,
`ImportFromProPresenterCommand`, `ImportSongsCommand`). Que el menú del
«📥 ▾» desaparezca.

### 3.5 Medios: el resto

- **Sacar el botón «Sin fondo (F4)» de la barra de medios** (cerca de
  `ClearBackgroundCommand`, al final de `MainWindow.xaml`). El de la barra
  de arriba, el menú y la tecla F4 se quedan.
- **Pestañas a la izquierda como lista apilada**, en vez de arriba del
  recuadro (`ItemsSource="{Binding MediaTabs}"`). Una columna angosta a la
  izquierda de la grilla de medios, una pestaña debajo de la otra, con la
  elegida resaltada. El «+» para agregar pestaña tiene que seguir estando.
- **Arrastrar y soltar archivos** desde el Explorador de Windows a la barra
  de medios: se importan a la pestaña que está elegida, igual que con
  «Agregar» (`ImportMediaCommand`; separá la parte que recibe rutas para
  reusarla). Aceptá lo mismo que acepta «Agregar» (imágenes, videos, PDF,
  PowerPoint) y avisá en la barra de estado si se soltó algo que no sirve.
  Mientras se arrastra encima, que se note (un borde de color).

### 3.6 Pantalla de escenario: editor y varias configuraciones

Es lo más grande. Hoy el escenario es fijo: `StageView` con las opciones de
`StageOptions` (hora, cronómetro, siguiente, notas, tamaño). Alexis quiere:

- **Un editor** como el de canciones (`Views/SongDesignerWindow`, que ya
  mueve y estira recuadros; mirá `Services/BoxGeometry.cs`): recuadros que
  uno ubica y agranda sobre un lienzo de 1920×1080, y en cada uno elige qué
  muestra: **letra en vivo**, **próxima diapositiva**, **hora**,
  **cronómetro** (el del servicio, `ProjectionViewModel.StageTimerStart`),
  **temporizador** (lo que le queda a la cuenta regresiva), **notas para el
  predicador**, **nombre de lo que está en vivo** y **texto fijo**. Cada
  recuadro con tamaño de letra, color y alineación.
- **Varias configuraciones guardadas con nombre** y cambio rápido entre
  ellas desde el «▾» que está al lado del botón Escenario. Que vengan tres
  armadas de fábrica: **Cantantes** (letra en vivo grande y la siguiente),
  **Predicador** (hora, cronómetro, notas) y **Igual a la pantalla
  principal** (un recuadro que muestra exactamente lo que se proyecta:
  un `SlideView` con la diapositiva en vivo).
- Guardalas en la tabla de Settings como JSON (claves nuevas tipo
  `stage.layouts` y `stage.layout.active`). La primera vez, convertí las
  opciones viejas de `StageOptions` en una configuración, para que nadie
  pierda lo que tenía.
- El modelo (configuración, recuadros, tipos, serializar y leer el JSON)
  va en Core con pruebas.
- El PREVIEW del operador tiene una vista «Escenario» que usa `StageView`:
  que siga funcionando con la configuración elegida.

### 3.7 Modo para PC de bajo rendimiento

Alexis: «que en la instalación pregunte si quiere o no la configuración
para PC de bajo rendimiento». Ese modo no existe todavía. Definilo así:

- Ajuste `performance.low` (Settings). Con él prendido:
  - las transiciones cortan (sin animación) en la salida;
  - el panel LIVE del operador muestra el póster del video en vez del video
    en movimiento (mirá `Controls/ProjectedView` y `VlcVideoSurface`);
  - las miniaturas y vistas previas del operador se dibujan sin sombra ni
    contorno (en la salida se mantienen: es lo que ve la congregación).
- Un ítem en el menú **Ver** para prenderlo y apagarlo («Modo PC de bajo
  rendimiento»), con tilde.
- En el instalador (`installer/EcclesiaCast.iss`, Inno Setup) una casilla
  en una página propia o como tarea: «Configurar para una PC de bajo
  rendimiento (recomendado en computadoras viejas o notebooks sin placa de
  video)», destildada por defecto. Si se tilda, que deje el valor en el
  registro (`HKCU\Software\EcclesiaCast`, `LowPerformance=1`); la app, al
  arrancar, si encuentra ese valor y todavía no hay ajuste guardado, lo
  copia a Settings. No hace falta compilar el instalador, pero el `.iss`
  tiene que quedar bien escrito.

### 3.8 (Opcional, sólo si lo anterior quedó terminado) Controles de música

Alexis preguntó por Spotify. **No** metas un reproductor ni un navegador
adentro. Lo liviano: en las pestañas de abajo a la derecha (Texto rápido,
Aviso al pie, Resaltar, Notas) sumá una pestaña **Música** con botones
⏮ ⏯ ⏭ y volumen que mandan las teclas multimedia de Windows (controlan
Spotify o el reproductor que esté abierto). Un texto chico aclarando que
sirve para el Spotify instalado en la PC.

## 4. Cuando termines

1. `dotnet build` y `dotnet test` en verde. Los commits hechos en `beta6`.
2. `docs/pendientes.md` sin los puntos que cerraste y `CHANGELOG.md` al día.
3. Un informe final **en castellano, para Alexis**, corto y sin jerga:
   qué quedó hecho, qué probaste y cómo, qué no pudiste probar, y qué
   decidiste vos cuando hubo que elegir. Si algo quedó a medias, decilo
   claro y dejalo anotado en `pendientes.md`.
