# Guía: traducir una pantalla de WPF a Avalonia

Para las tandas 15 a 21 de la versión de Mac. Se escribió en la tanda 14
(26/11/2026), con Avalonia 12.1.6. Antes de leerla hay que leer las reglas
([plan-mac-reglas.md](plan-mac-reglas.md)), que siguen valiendo todas.

**La idea en una línea:** la pantalla de Avalonia es una copia de la de WPF,
con los mismos textos, tamaños y colores. La lógica no se copia: ya está en
su ViewModel o en un servicio de `EcclesiaCast.Presentation`. Si algo de la
ventana de WPF decide algo y no está en la parte común, primero se muda ahí
(con su prueba) y después se traduce la pantalla.

## 1. Dónde va cada cosa

| Qué | Dónde |
|---|---|
| La pantalla | `src/EcclesiaCast.Desktop/Views/<Nombre>Window.axaml` y su `.axaml.cs` (mismo nombre que en WPF) |
| Un control propio | `src/EcclesiaCast.Desktop/Controls/` |
| Lo que abre la pantalla (la interfaz que pide el ViewModel, por ejemplo `ITextPrompt`) | Una clase en `src/EcclesiaCast.Desktop/Services/` y su línea en `Composition.cs`, donde hoy dice `NotOnMacYet.For<ITextPrompt>()` |
| Los estilos de todo el programa | `src/EcclesiaCast.Desktop/Styles/EcclesiaCastTheme.axaml` (es el `App.xaml` de WPF) |
| Los convertidores | `src/EcclesiaCast.Desktop/Converters/Converters.cs` |
| La prueba que dibuja la pantalla | `tests/EcclesiaCast.Desktop.Tests/ScreenBenchTests.cs` (y su gemela en `tests/EcclesiaCast.ScreenBench.Wpf/Program.cs`) |

`Desktop` no puede usar nada de WPF ni de Windows. Si hace falta algo del
sistema, va detrás de una interfaz de `Presentation/Platform` (sección 5.2 del
plan).

## 2. Tabla de traducción

| WPF | Avalonia |
|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` |
| `xmlns:local="clr-namespace:EcclesiaCast.App.X"` | `xmlns:local="using:EcclesiaCast.Desktop.X"` |
| Archivo `.xaml` | Archivo `.axaml`. En el `.axaml.cs`, el constructor llama a `InitializeComponent()` (no a `AvaloniaXamlLoader.Load`, que deja en `null` los controles con nombre) |
| `x:Name="ValueBox"` | `Name="ValueBox"` (o `x:Name`; las dos andan) |
| `Style="{StaticResource DarkButton}"` | `Theme="{StaticResource DarkButton}"`. En este programa, todos los estilos con nombre de `App.xaml` son `ControlTheme` y se piden con `Theme` |
| Estilo sin nombre en `App.xaml` (menús, listas, ayudas, barras) | Ya está: se aplica solo a todas las ventanas |
| `Visibility="Collapsed"` / `Visible` | `IsVisible="False"` / `True` |
| `Visibility="{Binding X, Converter={StaticResource BoolToVis}}"` | `IsVisible="{Binding X}"` |
| `InverseBoolToVis`, `InverseBool` | `IsVisible="{Binding !X}"`, `IsEnabled="{Binding !X}"` |
| «no es nulo» | `{Binding X, Converter={x:Static ObjectConverters.IsNotNull}}` |
| «texto no vacío» | `{Binding X, Converter={x:Static StringConverters.IsNotNullOrEmpty}}` |
| `HexToBrush`, `IndexIs`, `NonZeroToBool`, `FirstNonNull`, `TabMutedToVis` | `{x:Static conv:Converters.HexToBrush}`, `.IndexIs`, `.NonZero`, `.FirstNonNull`, `.InSet` (este último da un `bool` para `IsVisible`), con `xmlns:conv="using:EcclesiaCast.Desktop.Converters"` |
| `DataTrigger` | Una clase que sigue al dato y un estilo para esa clase (ver 3.2) |
| `Trigger Property="IsMouseOver"` | `Style Selector="^:pointerover"` dentro de un `ControlTheme`, o `Selector="Button:pointerover"` |
| `IsPressed`, `IsChecked`, `IsEnabled=False`, `IsSelected`, `IsKeyboardFocusWithin` | `:pressed`, `:checked`, `:disabled`, `:selected`, `:focus-within` |
| `TargetName="bd"` en un disparador | `Selector="^:pointerover /template/ Border#bd"` |
| `ToolTip="…"` | `ToolTip.Tip="…"` |
| `InputGestureText="Ctrl+K"` (en un menú) | `InputGesture="{OnPlatform Ctrl+K, macOS=Meta+K}"`: en la Mac sale ⌘K. La tecla de verdad se ata aparte (`KeyBindings` de la ventana; tanda 19 y 22) |
| `IsCheckable="True"` | `ToggleType="CheckBox"` |
| `SubmenuOpened` | `SubmenuOpened` también existe, pero salta también por los submenús: mirar que `e.Source` sea el propio menú |
| `MenuItem.Click` | `Click` existe. Si el ViewModel tiene el comando, mejor `Command="{Binding …}"` |
| `Window.ShowDialog()` y leer `DialogResult` | `Modal.Run(ventana)` (espera como WPF) y una propiedad propia con la respuesta (ver 3.3) |
| `IsCancel="True"` cierra la ventana | En Avalonia **no la cierra**: `IsCancel` sólo hace que Esc apriete el botón. El botón necesita su `Click` que llame a `Close()` |
| `WindowStartupLocation="CenterOwner"` | Lo pone `Modal.Run` |
| `Background="{StaticResource BgBrush}" FontFamily="Segoe UI"` en la ventana | No hace falta: el tema ya le da a toda ventana el fondo oscuro, letra blanca, la fuente del programa y tamaño 12 |
| `TextElement.Foreground` | `TextElement.Foreground` existe; para un `TextBlock` suelto, `Foreground` |
| `RelativeSource={RelativeSource AncestorType=X}` | `{Binding $parent[X].Propiedad}` |
| `ElementName=Root` | `{Binding #Root.Propiedad}` |
| `ActualWidth` | `Bounds.Width` |
| `DockPanel`, `Grid`, `StackPanel`, `WrapPanel`, `Border`, `ScrollViewer`, `UniformGrid` | Iguales. `Grid` acepta la forma corta `ColumnDefinitions="*,16,*"` |
| `StackPanel` con márgenes entre hijos | Igual, o `Spacing="8"` |
| `CaretBrush`, `SelectionBrush` | Iguales |
| `Run`, `Hyperlink` dentro de un `TextBlock` | `Run` existe. Un enlace: `HyperlinkButton` o un `TextBlock` con `Cursor="Hand"` y un comando |
| `SharedSizeGroup`, `Grid.IsSharedSizeScope` | Iguales |
| `DrawingImage`, `DrawingBrush`, `GeometryDrawing` | Iguales. En `DrawingBrush`, `Viewport="0,0,40,40" ViewportUnits="Absolute"` pasa a `DestinationRect="0,0,40,40"` |
| `MessageBox` | Nunca. Se pregunta por `INotices` (lo hace el ViewModel) |
| `Dispatcher.Invoke` | `IUiDispatcher` en el ViewModel; en la pantalla, `Dispatcher.UIThread.Post` |
| `x:Static` a una constante de la parte común | Igual: `{x:Static local:Clase.Campo}` |

## 3. Ejemplos del proyecto

### 3.1 Un botón con su estilo y su atajo

WPF (`App/MainWindow.xaml`):

```xml
<Button Content="Black" Style="{StaticResource DarkButton}"
        controls:ShortcutHint.Text="F1" Command="{Binding ToggleBlackCommand}"/>
```

Avalonia:

```xml
<Button Content="Black" Theme="{StaticResource DarkButton}"
        controls:ShortcutHint.Text="F1" Command="{Binding ToggleBlackCommand}"/>
```

con `xmlns:controls="using:EcclesiaCast.Desktop.Controls"`. El atajo se ve
sólo en el diseño de aprendizaje: lo cambia `EcclesiaCastTheme.ShowHints`. Un
texto de ayuda que sólo va en ese diseño usa `Theme="{StaticResource LearningHint}"`.

### 3.2 Un disparador de datos (`DataTrigger`)

WPF (`VersionChip` en `App.xaml`) prende el botón con `DataTrigger` sobre
`IsPrimary` e `IsSecondary`. En Avalonia el tema tiene las clases `primary` y
`secondary`, y la pantalla dice cuál va:

```xml
<Button Content="{Binding Abbreviation}" Theme="{StaticResource VersionChip}"
        Classes.primary="{Binding IsPrimary}" Classes.secondary="{Binding IsSecondary}"/>
```

Si el disparador cambia un texto, se hace igual. El menú
(`Desktop/Controls/MainMenu.axaml`) lo hace con «Cuenta regresiva…»:

```xml
<MenuItem Classes="countdown" Classes.running="{Binding IsCountdownRunning}"
          Command="{Binding OpenCountdownCommand}">
    <MenuItem.Styles>
        <Style Selector="MenuItem.countdown">
            <Setter Property="Header" Value="Cuenta regresiva…"/>
        </Style>
        <Style Selector="MenuItem.countdown.running">
            <Setter Property="Header" Value="✔ Cuenta regresiva en marcha… (detener o cambiar)"/>
        </Style>
    </MenuItem.Styles>
</MenuItem>
```

Si son sólo dos estados que se ven o no se ven, alcanza con dos controles,
uno con `IsVisible="{Binding X}"` y otro con `IsVisible="{Binding !X}"`.

### 3.3 Un diálogo entero: pedir un texto (`TextPromptWindow`, tanda 15)

WPF tiene la ventana (`App/Views/TextPromptWindow.xaml`) y un servicio
(`App/Services/TextPromptService.cs`) que la abre y devuelve lo escrito. En
Avalonia van las mismas dos piezas:

```xml
<!-- Desktop/Views/TextPromptWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="EcclesiaCast.Desktop.Views.TextPromptWindow"
        Width="420" SizeToContent="Height" CanResize="False">
    <Grid Margin="20" RowDefinitions="Auto,Auto,Auto,Auto,Auto">
        <TextBlock Name="MessageText" FontSize="13" Foreground="{StaticResource MutedBrush}"
                   TextWrapping="Wrap" Margin="0,0,0,8"/>
        <TextBox Grid.Row="1" Name="ValueBox" Padding="8,6" FontSize="14"/>
        <TextBlock Grid.Row="2" Name="SecondMessageText" FontSize="13" Foreground="{StaticResource MutedBrush}"
                   TextWrapping="Wrap" Margin="0,14,0,8" IsVisible="False"/>
        <TextBox Grid.Row="3" Name="SecondValueBox" Padding="8,6" FontSize="14" IsVisible="False"/>
        <StackPanel Grid.Row="4" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
            <Button Content="Cancelar" Theme="{StaticResource DarkButton}" IsCancel="True"
                    Padding="18,9" Click="Cancel_Click"/>
            <Button Content="Aceptar" Theme="{StaticResource AccentButton}" IsDefault="True"
                    Padding="18,9" Margin="10,0,0,0" Click="Accept_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

```csharp
// Desktop/Views/TextPromptWindow.axaml.cs
public sealed partial class TextPromptWindow : Window
{
    public TextPromptWindow(string title, string message, string initialValue)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ValueBox.Text = initialValue;
        Opened += (_, _) => { ValueBox.Focus(); ValueBox.SelectAll(); };
    }

    /// <summary>True when the operator accepted (WPF's DialogResult).</summary>
    public bool Accepted { get; private set; }

    public string Value { get; private set; } = string.Empty;

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Value = (ValueBox.Text ?? string.Empty).Trim();
        Accepted = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
```

El servicio hace `var window = new TextPromptWindow(…); Modal.Run(window);` y
devuelve `window.Accepted ? window.Value : null`, como el de WPF. Después, en
`Composition.cs`, `NotOnMacYet.For<ITextPrompt>()` se cambia por el servicio
nuevo. Así ya hacen sus ventanas `AvaloniaNotices` (`Views/NoticeWindow.cs`) y
`RemoteControlDialog`.

Fijate en lo que cambió y en lo que no:

- los textos, tamaños, márgenes y colores son los mismos;
- se fueron `Background`, `FontFamily` y los colores de cada `TextBox`: el
  tema ya los pone;
- `Visibility` pasó a `IsVisible`, y `Style` a `Theme`;
- `Cancelar` tiene su `Click`, porque `IsCancel` no cierra la ventana;
- `DialogResult` pasó a una propiedad propia;
- el foco se pone en `Opened`, porque en el constructor la ventana todavía no
  existe.

**Los campos de texto:** en WPF cada ventana pinta sus `TextBox` a mano
(`SurfaceAltBrush`, `LineBrush`, letra blanca). En Avalonia el `TextBox` del
tema Fluent oscuro ya se ve oscuro y con el borde azul del programa al
escribir. Si al comparar no alcanza, no lo arregles ventana por ventana:
agregá un `ControlTheme` para `TextBox` en `EcclesiaCastTheme.axaml`, que vale
para todas.

### 3.4 Lo que en WPF resuelve el código de la ventana

Si un `Click` de WPF llama a un comando del ViewModel, en Avalonia se enlaza el
comando. Si hace algo de la ventana (abrir otra ventana, cambiar el diseño),
la pantalla avisa y lo hace quien la contiene. El menú es el ejemplo:
`MainMenu` tiene el evento `Requested` con un `MenuRequest` (`Settings`,
`Exit`, `Pixabay`, las filas de Ver…). La ventana principal (tanda 19 en
adelante) se suscribe y hace lo mismo que `MainWindow.xaml.cs`. Para tildar las
filas de Ver cuando se abre ese menú, la ventana usa `SetChecked`. Las
tutorías las carga en `MainMenu.Tutorials` (tanda 21).

### 3.5 Un menú del botón derecho

Se escribe igual que en WPF (`ContextMenu` con `MenuItem` y `Separator`) y
toma solo el aspecto del programa. Lo que cambia son los enlaces que buscaban
`PlacementTarget`. Esta forma todavía no se usó en el proyecto: la primera
tanda que la use, que la confirme con una prueba y corrija este ejemplo si
hace falta.

```xml
<!-- WPF -->
Command="{Binding PlacementTarget.Tag.RenameBibleVersionCommand, RelativeSource={RelativeSource AncestorType=ContextMenu}}"
<!-- Avalonia: el menú hereda el DataContext del control donde está -->
Command="{Binding $parent[ListBox].((vm:MainViewModel)DataContext).RenameBibleVersionCommand}"
CommandParameter="{Binding}"
```

## 4. Enlaces compilados

`Desktop` compila los enlaces (`AvaloniaUseCompiledBindingsByDefault`): cada
`.axaml` dice de qué tipo es su `DataContext` con `x:DataType`, y un nombre
mal escrito no compila. Eso es bueno: si no compila, el nombre está mal.

```xml
<UserControl … xmlns:vm="using:EcclesiaCast.Presentation.ViewModels"
             x:DataType="vm:MainViewModel">
```

Dentro de un `DataTemplate`, otro `x:DataType` con el tipo de cada fila.

## 5. Probar y comparar cada pantalla

Cada pantalla nueva tiene una prueba que la dibuja en memoria, y su imagen de
WPF al lado.

1. **En WPF**, en `tests/EcclesiaCast.ScreenBench.Wpf/Program.cs`, sumá la
   pantalla a `Screens` con un nombre (`"texto-pedir"`), cómo se arma y su
   ancho. Se arma con la ventana real de `App`, sin mostrarla, y se dibuja su
   contenido: una ventana no se puede poner adentro de otra cosa, así que
   primero se lo sacás (`var c = (FrameworkElement)w.Content; w.Content =
   null;`). Para una ventana con ViewModel, dale uno con datos de ejemplo.
2. **En Avalonia**, en `ScreenBenchTests.cs`, una prueba `[AvaloniaFact]` que
   arma la misma ventana con los mismos datos y llama a `Draw(contenido,
   "texto-pedir")`. El nombre tiene que ser el mismo en los dos lados.
3. Corré los dos:

   ```powershell
   dotnet build tests/EcclesiaCast.ScreenBench.Wpf
   ```

   ```powershell
   .\tests\EcclesiaCast.ScreenBench.Wpf\bin\Debug\net10.0-windows10.0.19041.0\EcclesiaCast.ScreenBench.Wpf.exe
   ```

   ```powershell
   dotnet test tests/EcclesiaCast.Desktop.Tests --filter "FullyQualifiedName~ScreenBench"
   ```

   No se abre ninguna ventana. Quedan `artifacts/pantallas/wpf/`,
   `artifacts/pantallas/avalonia/windows/` y, con las dos, la imagen lado a
   lado en `artifacts/pantallas/comparacion/<nombre>.png`. El banco de WPF
   deja `resultado.txt` en su carpeta.
4. **Mirá cada imagen lado a lado** (con la herramienta de leer imágenes). Lo
   que importa: textos, orden, tamaños, colores, márgenes, qué está prendido o
   apagado. Anotá en el informe lo que quedó distinto y por qué.

**Diferencias que se esperan y no hay que perseguir:**

- La letra de Avalonia se ve un poco más nítida y algún renglón queda uno o
  dos píxeles más alto. Es el motor de dibujo, no un error.
- En la Mac la letra de la interfaz es Selawik (las medidas de Segoe UI), no
  Segoe UI.
- **Una fila con `Command` sin ViewModel se ve apagada en Avalonia y prendida
  en WPF.** Avalonia apaga el control cuyo comando no pudo enlazar. En las
  imágenes del menú pasa eso porque la prueba no arma un `MainViewModel`; con
  el programa de verdad se ven prendidas. Si la pantalla tiene ViewModel,
  dáselo a las dos y la diferencia desaparece.
- En WPF, un menú abierto o una ayuda flotante no se puede dibujar en memoria
  (es una ventana aparte). El banco dibuja sus filas como un menú del botón
  derecho, que tiene el mismo aspecto. En Avalonia se abre el menú de verdad.

**Además, una prueba de contenido** cuando la pantalla tiene muchos textos:
leer el `.xaml` de WPF y comprobar que cada texto y cada ayuda está en la de
Avalonia. `The_menu_has_every_row_of_the_WPF_one` lo hace con el menú: si
alguien agrega una fila al menú de Windows y no al de Mac, falla.

## 6. Lo que no se hace

- **Lógica en la pantalla.** Si el `.xaml.cs` de WPF tiene un `if` que decide
  algo (qué se guarda, qué se muestra según un dato), va al ViewModel con su
  prueba en `Presentation.Tests`, y las dos pantallas lo usan.
- **Tocar `App`** (la versión de Windows), salvo para mudar lógica a la parte
  común sin cambiar lo que se ve. El banco de render de WPF tiene que dar
  idéntico.
- **Colores sueltos.** Se usan los pinceles del tema (`SurfaceAltBrush`,
  `MutedBrush`…). Si WPF tiene un color suelto (`#243558`), se copia tal cual.
- **Un estilo nuevo sólo para una ventana**, cuando ya hay uno en el tema que
  hace lo mismo.
- **`MessageBox`, `Process.Start`, rutas de Windows, `System.Windows`.** Todo
  pasa por las interfaces de `Presentation/Platform`.
- **Abrir la ventana para mirarla.** Sólo se mira la imagen en memoria.

## 7. Lo que ya está y lo que falta

**Ya está (tanda 14):** el tema con todo `App.xaml`, `ShortcutHint`, el
diseño de aprendizaje o avanzado, los convertidores, el menú de la ventana
principal y los dos bancos de pantallas.

**Falta, y lo necesita la primera tanda que lo use:**

- `IDialogs` (abrir y guardar archivos, elegir carpeta) todavía es
  `NotOnMacYet`. Avalonia lo hace con `StorageProvider`, que responde más
  tarde (es asincrónico) y los ViewModels esperan la respuesta en la línea
  siguiente. Hay que esperarla como `Modal.Run`: un `DispatcherFrame` que
  termina cuando llega el resultado. Lo usan las tandas 15 y 16.
- El menú global de la Mac (`NativeMenu`), Cmd en vez de Ctrl en las teclas y
  «Salir» en el menú del programa: tanda 22.
- Un `ControlTheme` para `TextBox`, si al comparar hace falta (ver 3.3).
