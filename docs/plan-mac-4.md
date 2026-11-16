# Tanda 4 (Opus) — Proyecto `Presentation` e interfaces de plataforma

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). Contexto:
[plan-mac.md](plan-mac.md), sección 5 (estructura e interfaces). No leas el
resto.

**Rama:** la de esta sesión, cortada de `mac`. **Antes tienen que estar:** la
puerta 1 (prototipo aprobado) y la tanda 3 (.NET 10). **Tamaño:** grande.

## Para qué

Hoy `src/EcclesiaCast.App` mezcla pantallas (WPF), ViewModels y servicios.
Para tener una segunda interfaz (Avalonia) sin duplicar la lógica, todo lo que
no dibuja tiene que vivir en un proyecto que **no conozca WPF**. Esta tanda
arma el lugar y las reglas; la tanda 5 (Sonnet) muda los ViewModels.

**Esto es reordenar: el programa de Windows tiene que hacer exactamente lo
mismo que antes.** Ningún cambio de comportamiento, ni «de paso».

## Qué hay que hacer

1. **Proyecto `src/EcclesiaCast.Presentation`** (`net10.0`, sin `UseWPF`, sin
   nada de Windows). Referencia `Core` y `Data`. Paquetes:
   `CommunityToolkit.Mvvm`, `LibVLCSharp` (sin la parte de WPF), `Serilog`,
   `QRCoder`, `Microsoft.Extensions.DependencyInjection.Abstractions`.
   `App` pasa a referenciarlo. Sumalo a `EcclesiaCast.slnx`.

2. **Proyecto `src/EcclesiaCast.Platform.Windows`**
   (`net10.0-windows10.0.19041.0`, **sin** `UseWPF` ni `UseWindowsForms`).
   Referencia `Presentation`. Para lo que le habla a Windows y no dibuja.

3. **Interfaces de plataforma** en `Presentation/Platform/`, según la tabla
   5.2 del plan. Definí en esta tanda las que hacen falta para sacar WPF de
   los ViewModels:
   - `IUiDispatcher` (`Post`, `InvokeAsync`, `CheckAccess`) e `IUiTimer`
     (intervalo, `Tick`, `Start`, `Stop`) con su fábrica. Reemplazan los 6
     `DispatcherTimer` y los `Application.Current.Dispatcher`.
   - `IDialogs`: abrir archivo, guardar archivo, elegir carpeta (hoy
     `Microsoft.Win32.OpenFileDialog` y parecidos, en 13 archivos).
   - `INotices`: lo que hoy hace la fachada estática `Services/Notice`.
     **Mantené la fachada estática** (se usa en decenas de lugares): pasa a
     `Presentation` y delega en el `INotices` que se registre al arrancar.
   - `IPlatformPaths`: carpeta de datos, de videos bajados, de herramientas,
     de modelos, y la de lo que viene con el programa. Reemplaza a
     `App.DataDir` y a las rutas que hoy se arman aparte en `YtDlp.ToolsFolder`
     y `Listening/SpeechEar`. Tiene que seguir respetando
     `ECCLESIACAST_DATA_DIR`.
   - `IImageFactory`: crea, a partir de un archivo o de bytes, la imagen que
     entiende cada interfaz. Los ViewModels la guardan como `object?` (hoy
     `ImageSource` en `ProjectionViewModel.LiveFrame`,
     `SlideItemViewModel.PageImage` y `MainViewModel.WebImages`). Elegí la
     forma más simple que funcione enlazada desde un `Image` de WPF y de
     Avalonia.
   - `IFontCatalog`: nombres de las fuentes instaladas (hoy
     `Fonts.SystemFontFamilies` en 7 lugares).
   - `IClipboardText`, `IShell` (abrir una carpeta, una dirección web: hoy
     `Process.Start` suelto en 12 archivos).
   - `IPlatformInfo`: sistema, y `IsAvailable(Feature)` para las funciones que
     no existen en todos lados (enum `Feature`: captura de ventanas, panel
     web, YouTube en vivo, PowerPoint, lo que suena, seguir la canción,
     corrector). En Windows, todas disponibles.
   - `IVideoEngine`: acceso al `LibVLC` compartido (hoy `App.VideoEngine`).

   Las demás de la tabla (`ITextMeasurer`, `IOutputWindowNative`,
   `IDocumentRenderer`, `IDeckConverter`, `ISystemMedia`, `ISoundInput`,
   `ISpeechEar`, `IPowerGuard`, `IThumbnailer`, `IUpdateInstaller`,
   `ISpellChecker`): definí la interfaz **sólo si podés mudar ya su
   implementación de Windows sin WPF**; si no, quedan para su tanda.

4. **Implementaciones de WPF** de todo lo anterior, en
   `src/EcclesiaCast.App/Platform/`, registradas en `App.xaml.cs`.

5. **Mudar a `Presentation`** las 26 interfaces `Services/I*.cs` y los
   servicios que **compilen sin WPF** tal como están o con cambios mínimos.
   Candidatos (comprobalo compilando): `AppInfo`, `BoxGeometry`,
   `BroadcastStyleSettings`, `GifImporter`, `LightVideoCache`, `MediaLookup`,
   `PerformanceMode`, `SongSaver`, `ToolInstaller`, `TransitionSettings`,
   `UsageReporter`, `YtDlp`, `UpdateChecker`, `VlcAudioPlayer`, `PlayerSound`,
   `VlcLog`, y la carpeta `Remote/` (el servidor usa el hilo de WPF en una
   línea: pasala a `IUiDispatcher`; `OutputPage` nombra WebView2 sólo en un
   comentario).

6. **Mudar a `Platform.Windows`** lo que le habla a Windows y no dibuja:
   `PowerPointConverter`, `MediaKeys`, `PowerThrottling`,
   `ProcessInstallerLauncher`, `AudioEndpointWatcher`, `Listening/SoundInput`,
   y la parte de `DocumentImporter` que dibuja el PDF con `Windows.Data.Pdf`.
   Lo que usa imágenes o ventanas de WPF (`WindowCapture`, `ShellThumbnail`,
   `DarkTitleBar`, `WebViewProfile`, `YouTubeCookies`,
   `MusicaComBrowserSearch`, `SpellChecking`) **se queda en `App`**.

7. **Nombres de archivos por sistema** en `ToolInstaller` y `YtDlp`
   (`yt-dlp.exe`, `deno.exe`, `ffmpeg.exe`, direcciones de descarga): que
   salgan de un solo lugar que hoy devuelve los de Windows. No agregues
   todavía los de Mac.

8. **Espacios de nombres:** lo mudado pasa de `EcclesiaCast.App.Services` a
   `EcclesiaCast.Presentation…`. Usá `git mv` para que el historial se
   conserve.

9. **`CONTRIBUTING.md`:** una sección corta con la estructura nueva y la
   regla: «`Presentation` no conoce pantallas; si no compila sin WPF, no va
   ahí».

10. **Dejale a Sonnet la lista para la tanda 5** en
    `docs/plan-mac-5-lista.md`: por cada archivo de `ViewModels/`, qué le
    queda de WPF (con Grep: `System.Windows`, `Application.Current`,
    `DispatcherTimer`, `Microsoft.Win32`, `new Views.`, `ImageSource`,
    `BitmapImage`, `App.DataDir`, `App.VideoEngine`, `Process.Start`) y con
    qué interfaz se reemplaza cada cosa. Una tabla, sin explicaciones.

## Terminado cuando

- `Presentation` y `Platform.Windows` compilan **sin referencia a WPF**.
- `dotnet build` sin advertencias, `dotnet test` en verde.
- `dotnet publish` de `App` para `win-x64` termina bien.
- Los ViewModels **todavía no se mudaron** (es la tanda 5), pero las
  interfaces que necesitan existen y están registradas.
- El informe lista cada archivo mudado, cada uno que no se pudo y por qué.

## Cuidado con

- **El orden de arranque** en `App.xaml.cs`: registro, pantalla de carga,
  restauración de la copia, migraciones, LibVLC. No lo cambies.
- **Los delegados de LibVLC** que tienen que vivir tanto como el reproductor
  (comentario al principio de `Controls/VlcVideoSurface.xaml.cs`).
- **Hilos:** lo que hoy corre en el hilo de la pantalla tiene que seguir ahí.
  `IUiDispatcher.Post` no es lo mismo que `Invoke`: respetá cuál se usaba.
- **`InternalsVisibleTo`:** si algo interno de `App` lo usaban las pruebas,
  mantené el acceso.
