# Tanda 5 (Sonnet) — Mudar los ViewModels a `Presentation`

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). Tu lista
de trabajo es `docs/plan-mac-5-lista.md`, que dejó la tanda 4. No leas el plan
completo.

**Rama:** la de esta sesión, cortada de `mac` con la tanda 4 ya fusionada.
**Tamaño:** grande: son 2 o 3 sesiones **en fila, nunca en paralelo**. Si se
te acaba el espacio, dejá en el informe en qué archivo quedaste.

## Para qué

Los ViewModels (`src/EcclesiaCast.App/ViewModels/`, 34 archivos; el
`MainViewModel` son 29 partes que suman 10.200 líneas) tienen muy poco de WPF,
pero lo tienen. Hay que sacárselo para que los use también la interfaz nueva.

**Es reordenar: el programa tiene que hacer exactamente lo mismo.** Si un
reemplazo cambia cuándo o en qué hilo pasa algo, no lo hagas así: anotalo.

## Cómo (importa el orden)

`MainViewModel` es una sola clase partida en archivos: **no se puede mudar de
a una parte** a otro proyecto. Por eso son dos etapas.

### Etapa A — Limpiar cada archivo en su lugar (un commit por archivo)

Para cada archivo de la lista, sin moverlo:

| Hoy | Pasa a |
|---|---|
| `System.Windows.Threading.DispatcherTimer` | `IUiTimer` (pedido a su fábrica) |
| `Application.Current.Dispatcher.BeginInvoke(...)`, `Dispatcher.CurrentDispatcher` | `IUiDispatcher.Post(...)` (o `InvokeAsync` si se esperaba el resultado) |
| `Microsoft.Win32.OpenFileDialog`, `SaveFileDialog`, `OpenFolderDialog` | `IDialogs` |
| `new Views.AlgoWindow(...) { Owner = Application.Current.MainWindow }.ShowDialog()` | Una interfaz de diálogo como las que ya existen (`ISongEditor`, `ICountdownDialog`…): creá `IAlgoDialog` en `Presentation` y su servicio en `App/Services`, copiando el formato de uno existente |
| `ImageSource`, `BitmapImage`, `BitmapSource` | `object?` creado con `IImageFactory` |
| `System.Windows.Media.Fonts.SystemFontFamilies` | `IFontCatalog` |
| `App.DataDir`, rutas armadas con `SpecialFolder` | `IPlatformPaths` |
| `App.VideoEngine` | `IVideoEngine` |
| `Process.Start` para abrir una carpeta o una página | `IShell` |
| `Clipboard` | `IClipboardText` |
| `WindowInteropHelper`, manejadores de ventana (`MainViewModel.Window.cs`) | Que el servicio que lo necesita (`IWindowCapture`) reciba la ventana dueña por su cuenta; el ViewModel no la conoce |
| `Visibility` como tipo de una propiedad | `bool` (y el XAML usa el conversor `BoolToVis` que ya existe) |
| `MessageBox` | `Notice` (ya debería estar todo pasado) |

Las ventanas que hoy se abren directo desde un ViewModel (encontradas con
Grep): `BibleFormatHelpWindow`, `LightCopyWindow`, `BroadcastWindow`,
`BroadcastStyleWindow`, `TrashWindow`, y las de
`MainViewModel.Remote.cs:119` y `MainViewModel.YouTube.cs:167`.

Las interfaces nuevas se piden por el constructor, como las demás. El
constructor de `MainViewModel` ya es largo: no lo «mejores», sólo sumá.

Después de cada archivo: `dotnet build` sin advertencias y `dotnet test`.

### Etapa B — La mudanza (un solo commit)

Cuando ningún archivo de `ViewModels/` tenga `using System.Windows` ni
`Microsoft.Win32` (comprobalo con Grep):

1. `git mv` de toda la carpeta a `src/EcclesiaCast.Presentation/ViewModels/`.
2. Espacio de nombres `EcclesiaCast.App.ViewModels` →
   `EcclesiaCast.Presentation.ViewModels`, y los `xmlns` de los XAML que los
   nombran.
3. Lo que no compile es algo de `App` que el ViewModel seguía usando: se muda
   a `Presentation` si no tiene WPF, o se le hace una interfaz.

### Etapa C — Pruebas de humo de los ViewModels

Proyecto nuevo `tests/EcclesiaCast.Presentation.Tests` (`net10.0`, xUnit),
sumado a la solución y a `build.yml` (también en el trabajo de Mac). Con
servicios de mentira hechos a mano (sin librerías de imitación) y una base en
una carpeta temporal:

1. Se construye un `MainViewModel` completo.
2. Abrir una canción arma sus diapositivas.
3. Proyectar una diapositiva cambia el estado de `ProjectionViewModel`.
4. Elegir un versículo arma su diapositiva.
5. Un reloj de mentira (`IUiTimer`) hace avanzar la cuenta regresiva.

No más que eso: la idea es que el armado completo se pruebe en Windows y en
Mac en cada cambio.

## Terminado cuando

- `src/EcclesiaCast.App/ViewModels/` no existe.
- `Presentation` compila sin WPF.
- Build sin advertencias, todas las pruebas en verde, `dotnet publish` de
  `App` termina bien.
- El informe lista, por archivo, qué se reemplazó, y **todo lugar donde
  dudaste** de que el reemplazo sea idéntico (hilo, momento, foco de
  ventana).

## Cuidado con

- **`Post` contra `Invoke`:** `BeginInvoke` no espera; `Invoke` sí. No los
  cruces.
- **La prioridad de los relojes y de los `BeginInvoke`**
  (`DispatcherPriority.Loaded`, `Background`, `Render`): si el código la
  pedía, la interfaz tiene que poder pasarla. Si a `IUiDispatcher` le falta,
  agregásela (y su versión de WPF), no la descartes.
- **Los diálogos con dueño:** hoy todos tienen `Owner = MainWindow`. El
  servicio nuevo tiene que ponerlo igual, o el diálogo queda detrás.
- **No toques la lógica.** Si ves una falla, anotala; no la arregles acá.
