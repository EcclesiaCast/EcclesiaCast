# Tanda 5 — Lo que le queda de WPF a cada ViewModel

Sacado con Grep el 18/11/2026, después de la tanda 4. Las interfaces están en
`src/EcclesiaCast.Presentation/Platform/` y ya están registradas en
`App.xaml.cs`. «(por definir)» es una interfaz que todavía no existe.

| Archivo | Línea | Lo que le queda | Se reemplaza con |
|---|---|---|---|
| `BibleGridTiles.cs` | 1, 16–36 | `System.Windows.Media`: `Color`, `ColorConverter`, `SolidColorBrush`, `Brush` | El color como texto (`#RRGGBB`) y un conversor en la vista |
| `MainViewModel.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.cs` | 166 | `Fonts.SystemFontFamilies` | `IFontCatalog.InstalledFamilies()` |
| `MainViewModel.cs` | 455 | `Application.Current?.Dispatcher.BeginInvoke` | `IUiDispatcher.Post` |
| `MainViewModel.Audio.cs` | 25 | `DispatcherTimer` (250 ms) | `IUiTimerFactory.Create` |
| `MainViewModel.Audio.cs` | 132 | `Microsoft.Win32.OpenFileDialog` (varios) | `IDialogs.OpenFiles` |
| `MainViewModel.Bible.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Bible.cs` | 88 | `Keyboard.Modifiers` (Ctrl) | La vista pasa si Ctrl está apretado (parámetro del comando) |
| `MainViewModel.Bible.cs` | 514 | `CollectionViewSource.GetDefaultView` | Filtrar la lista en el ViewModel |
| `MainViewModel.Bible.cs` | 619 | `Controls.SlideTextFit` | `ITextMeasurer` (por definir) |
| `MainViewModel.Bible.cs` | 1038 | `new Views.BibleFormatHelpWindow`, `Application.Current.MainWindow` | Interfaz de diálogo nueva en `Presentation/Services` (como `IBackupDialog`) |
| `MainViewModel.Bible.cs` | 1047 | `Microsoft.Win32.OpenFileDialog` | `IDialogs.OpenFile` |
| `MainViewModel.Bible.cs` | 1185 | `Microsoft.Win32.SaveFileDialog` | `IDialogs.SaveFile` |
| `MainViewModel.BibleDownload.cs` | 3 | `using System.Windows` | Borrar |
| `MainViewModel.BiblePackages.cs` | 2 | `using System.Windows` | Borrar |
| `MainViewModel.BiblePackages.cs` | 16 | `new BiblePackageDialogService()` | `IBiblePackageDialog` por el constructor |
| `MainViewModel.BiblePackages.cs` | 19 | `AppContext.BaseDirectory` | `IPlatformPaths.BundledFolder` |
| `MainViewModel.BiblePackages.cs` | 85 | `Microsoft.Win32.SaveFileDialog` | `IDialogs.SaveFile` |
| `MainViewModel.BiblePackages.cs` | 138 | `Microsoft.Win32.OpenFileDialog` | `IDialogs.OpenFile` |
| `MainViewModel.BiblePackages.cs` | 255 | `MessageBox.Show` (Sí/No/Cancelar del sistema) | `INotices` con los mismos botones; cambia el aspecto: lo decide Alexis |
| `MainViewModel.BundledBackgrounds.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.BundledBackgrounds.cs` | 18 | `AppContext.BaseDirectory` | `IPlatformPaths.BundledFolder` |
| `MainViewModel.BundledBackgrounds.cs` | 25, 99 | `App.DataDir` | `IPlatformPaths.DataFolder` |
| `MainViewModel.BundledBackgrounds.cs` | 109 | `Microsoft.Win32.OpenFileDialog` | `IDialogs.OpenFile` |
| `MainViewModel.BundledBackgrounds.cs` | — | `MediaThumbnails` (App) | `IThumbnailer` (por definir) |
| `MainViewModel.CardZoom.cs` | 58 | `DispatcherTimer` | `IUiTimerFactory.Create` |
| `MainViewModel.Listening.cs` | 3, 32, 245–247 | `DispatcherTimer` (120 ms) | `IUiTimerFactory.Create` |
| `MainViewModel.Listening.cs` | 44 | `App.DataDir` | `IPlatformPaths.DataFolder` |
| `MainViewModel.Listening.cs` | 206 | `new SongListener(Application.Current.Dispatcher)` | `SongListener(IUiDispatcher)` |
| `MainViewModel.Listening.cs` | — | `SongListener`, `SpeechEar` (App), `SoundInput`, `SoundInputDevice` (Platform.Windows) | `ISoundInput`, `ISpeechEar` (por definir) |
| `MainViewModel.Logos.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Logos.cs` | 135 | `App.VideoEngine`, `MediaThumbnails` | `IVideoEngine.Instance`, `IThumbnailer` (por definir) |
| `MainViewModel.Media.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Media.cs` | 672 | `Microsoft.Win32.OpenFileDialog` (varios) | `IDialogs.OpenFiles` |
| `MainViewModel.Media.cs` | 937 | `new Views.LightCopyWindow`, `Application.Current.MainWindow` | Interfaz de diálogo nueva en `Presentation/Services` |
| `MainViewModel.Media.cs` | 1132 | `App.VideoEngine` | `IVideoEngine.Instance` |
| `MainViewModel.Media.cs` | — | `MediaThumbnails` (App) | `IThumbnailer` (por definir) |
| `MainViewModel.OutputVolume.cs` | 38–40, 88–99 | `DispatcherTimer` (3) | `IUiTimerFactory.Create` |
| `MainViewModel.Performance.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Performance.cs` | 68 | `Microsoft.Win32.Registry` (lo que eligió el instalador) | Servicio nuevo en `Platform.Windows` detrás de una interfaz (por definir) |
| `MainViewModel.Pixabay.cs` | 20 | `App.DataDir` | `IPlatformPaths.DataFolder` |
| `MainViewModel.Playlist.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Projection.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Remote.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Remote.cs` | 58, 64 | `new Views.BroadcastWindow`, `new Views.BroadcastStyleWindow`, `Application.Current.MainWindow` | Interfaces de diálogo nuevas en `Presentation/Services` |
| `MainViewModel.Remote.cs` | 117–119 | `new Views.RemoteControlWindow`, `Application.Current.MainWindow` | Interfaz de diálogo nueva en `Presentation/Services` |
| `MainViewModel.Remote.cs` | — | `MediaThumbnails` (App) | `IThumbnailer` (por definir) |
| `MainViewModel.Search.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Songs.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Songs.cs` | 529 | `new Views.TrashWindow`, `Application.Current.MainWindow` | Interfaz de diálogo nueva en `Presentation/Services` |
| `MainViewModel.Songs.cs` | 566 | `Microsoft.Win32.OpenFileDialog` (varios) | `IDialogs.OpenFiles` |
| `MainViewModel.Stage.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Stage.cs` | 181 | `OperatorScreen` (App) | `IDisplayProvider` o interfaz nueva (por definir) |
| `MainViewModel.Themes.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Timing.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Timing.cs` | 60 | `DispatcherTimer` | `IUiTimerFactory.Create` |
| `MainViewModel.Transport.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.Transport.cs` | 32 | `DispatcherTimer` | `IUiTimerFactory.Create` |
| `MainViewModel.Updates.cs` | 74 | `Application.Current?.Dispatcher.BeginInvoke` | `IUiDispatcher.Post` |
| `MainViewModel.Web.cs` | 2 | `using System.Windows` | Borrar |
| `MainViewModel.Web.cs` | 126 | `App.DataDir` | `IPlatformPaths.DataFolder` |
| `MainViewModel.Web.cs` | — | `MediaThumbnails`, `WebLiveFeed`, `WebViewProfile` (App) | `IThumbnailer`, `IWebPageHost` (por definir lo que falte) |
| `MainViewModel.WebImages.cs` | 3, 193 | `BitmapDecoder` (tamaño de la imagen) | `IImageFactory.ReadSize` |
| `MainViewModel.WebImages.cs` | 41 | `App.DataDir` | `IPlatformPaths.DataFolder` |
| `MainViewModel.Window.cs` | 1–2 | `using System.Windows`, `System.Windows.Interop` | Borrar |
| `MainViewModel.Window.cs` | 38 | `new WindowPickerDialogService()`; `IWindowPickerDialog` vive en App | Mudar `IWindowPickerDialog` a `Presentation/Services`; por el constructor |
| `MainViewModel.Window.cs` | 191 | `Application.Current.MainWindow`, `WindowInteropHelper` | Que `IWindowCapture.StartFromPickerAsync` busque la ventana dueña él solo |
| `MainViewModel.Window.cs` | — | `WindowCapture`, `WindowCaptureFeed`, `WindowPick` (App) | `IWindowCapture` (ya en `Presentation/Platform`) |
| `MainViewModel.YouTube.cs` | 4 | `using System.Windows` | Borrar |
| `MainViewModel.YouTube.cs` | 165–167 | `new Views.YouTubeDownloadWindow`, `Application.Current.MainWindow` | Interfaz de diálogo nueva en `Presentation/Services` |
| `MainViewModel.YouTube.cs` | 182 | `App.VideoEngine`, `MediaThumbnails` | `IVideoEngine.Instance`, `IThumbnailer` (por definir) |
| `MainViewModel.YouTube.cs` | 251 | `Microsoft.Win32.OpenFileDialog` | `IDialogs.OpenFile` |
| `MainViewModel.YouTube.cs` | 302 | `Process.Start` (`explorer.exe /select`) | `IShell.ShowInFolder` |
| `MainViewModel.YouTube.cs` | — | `WebViewProfile` (App) | `IYouTubeBrowser` o interfaz nueva (por definir) |
| `ProjectionViewModel.cs` | 42, 46 | `ImageSource` (`LiveFrame`, `LiveLogoFrame`) | `object?` |
| `SlideItemViewModel.cs` | 2, 55–86 | `ImageSource`, `BitmapImage` (`PageImage`) | `object?` y `IImageFactory.FromFile(path, poster ? null : 220)` |
| `ThemeManagerViewModel.cs` | 4–5 | `using System.Windows`, `System.Windows.Threading` | Borrar |
| `ThemeManagerViewModel.cs` | 428 | `Dispatcher.CurrentDispatcher.BeginInvoke` | `IUiDispatcher.Post` |
| `ThemeManagerViewModel.cs` | 531 | `Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, …)` | `IUiDispatcher.Post` (no tiene prioridad: probar que siga esperando a que se dibuje) |
| `ThemeManagerViewModel.cs` | 839–846 | `Application.Current`, `new Views.ChoiceWindow` | `INotices` o interfaz de diálogo nueva |
| `ThemeManagerViewModel.cs` | 1014 | `Microsoft.Win32.OpenFileDialog` | `IDialogs.OpenFile` |
| `ThemeManagerViewModel.cs` | — | `ColorPickerHelper` (App) | Interfaz de diálogo nueva (por definir) |
