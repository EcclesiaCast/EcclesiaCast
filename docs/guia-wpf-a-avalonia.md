# Guía: traducir una pantalla de WPF a Avalonia

Para las tandas 15 a 21 de la versión de Mac. Se escribió en la tanda 14
(26/11/2026), con Avalonia 12.1.6, y se corrigió con lo que dejó la tanda 15.
Antes de leerla hay que leer las reglas
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
| `Run`, `Hyperlink` dentro de un `TextBlock` | `Run` existe. `Hyperlink` no: `<HyperlinkButton Classes="inline" …>`, al lado del texto en un `WrapPanel` (ver `FeedbackWindow.axaml` y `AboutWindow.axaml`) |
| `ProgressBar` finita de una ventana que hace esperar | `<ProgressBar Classes="work" Height="6" …>`, sin repetir sus colores |
| `RadioButton` con una caja de texto adentro | `controls:HolderRadioButton`. El `RadioButton` de Avalonia se queda con **Enter** y con la barra espaciadora aunque vengan de adentro, y Enter no llega al botón principal |
| `HorizontalContentAlignment` en un `DarkButton` o `AccentButton` | **No se copia**: ver abajo |
| `TextChanged` | Existe, pero llega **más tarde** que en WPF: ver abajo |
| `SharedSizeGroup`, `Grid.IsSharedSizeScope` | Iguales |
| `DrawingImage`, `DrawingBrush`, `GeometryDrawing` | Iguales. En `DrawingBrush`, `Viewport="0,0,40,40" ViewportUnits="Absolute"` pasa a `DestinationRect="0,0,40,40"` |
| `MessageBox` | Nunca. Se pregunta por `INotices` (lo hace el ViewModel) |
| `Dispatcher.Invoke` | `IUiDispatcher` en el ViewModel; en la pantalla, `Dispatcher.UIThread.Post` |
| `x:Static` a una constante de la parte común | Igual: `{x:Static local:Clase.Campo}` |

### 2.1 Dos trampas de la tabla

**El `DarkButton` de WPF centra el texto aunque se le pida a la izquierda.** Su
plantilla tiene el centrado escrito fijo, así que un
`HorizontalContentAlignment="Left"` en la ventana de WPF no hace nada (pasa en
`ChoiceWindow`). La plantilla de Avalonia sí obedece esa propiedad: si se
copia, el texto sale a la izquierda y no se parece. Se copia **lo que se ve** en
la imagen de WPF, no lo que dice el `.xaml`. Para que un botón ocupe todo el
ancho, `HorizontalAlignment="Stretch"`.

**`TextChanged` llega más tarde que en WPF.** En WPF se avisa en la misma línea
que cambia el texto; en Avalonia, después. El truco de WPF de prender una
bandera (`_writing = true`), escribir en la caja y apagarla **no alcanza**:
cuando llega el aviso la bandera ya está apagada. Hay que reconocer lo propio
por el contenido (¿el texto es el que yo escribiría para el valor actual?), como
`Desktop/Controls/NumericField`. En las pruebas, después de escribir en una caja
va `Dispatcher.UIThread.RunJobs()` antes de comprobar.

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

El servicio (`Desktop/Services/AvaloniaTextPrompt.cs`) hace
`var window = new TextPromptWindow(…); Modal.Run(window);` y devuelve
`window.Accepted ? window.Value : null`, como el de WPF. Después, en
`Composition.cs`, `NotOnMacYet.For<ITextPrompt>()` se cambia por el servicio
nuevo. Así hacen todos los de la tanda 15.

**La advertencia AVLN3001.** Una ventana como ésta, que sólo tiene un
constructor con parámetros, da `AVLN3001`: el cargador de XAML en tiempo de
ejecución no la puede crear. El programa no usa ese cargador (las ventanas se
arman desde el código, con sus datos), así que está **silenciada en
`EcclesiaCast.Desktop.csproj`** (`NoWarn`). No hay que hacer nada: ni agregar un
constructor vacío, ni silenciarla de nuevo, ni sumar otras advertencias ahí.

Fijate en lo que cambió y en lo que no:

- los textos, tamaños, márgenes y colores son los mismos;
- se fueron `Background`, `FontFamily` y los colores de cada `TextBox`: el
  tema ya los pone;
- `Visibility` pasó a `IsVisible`, y `Style` a `Theme`;
- `Cancelar` tiene su `Click`, porque `IsCancel` no cierra la ventana;
- `DialogResult` pasó a una propiedad propia;
- el foco se pone en `Opened`, porque en el constructor la ventana todavía no
  existe.

**Cajas de texto, casillas, opciones, enlaces y barra de progreso.** De fábrica
se ven distintos en Avalonia; la tanda 15 ya lo resolvió en
`EcclesiaCastTheme.axaml`. `TextBox`, `CheckBox` y `RadioButton` salen solos
como en WPF; para el enlace y la barra van las clases `inline` y `work` (tabla
de la sección 2). **No se copian** de la ventana de WPF `Background`,
`Foreground`, `BorderBrush` ni `CaretBrush` de las cajas; sí `Padding`,
`FontSize` y el ancho (si se repiten, con una clase en `Window.Styles`, como en
`CountdownWindow.axaml`). Si otro control no se parece, se arregla en el tema,
no ventana por ventana.

**Ventanas que trabajan solas** (copia liviana, descarga). El trabajo, su avance
y su cancelación van en el ViewModel; la ventana sólo lo arranca y se cierra
cuando se lo piden. En Avalonia **se arranca con `Dispatcher.UIThread.Post`**
dentro de `Opened`:

```csharp
copy.CloseRequested += Close;
Opened += (_, _) => Dispatcher.UIThread.Post(async () => await copy.RunAsync());
```

Sin el `Post`, un trabajo que termina enseguida pide cerrar la ventana mientras
todavía se está abriendo. Y como entre abrir y arrancar la pueden cerrar, el
ViewModel no empieza nada si ya recibió `Closed()`.

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

1. **En WPF**, sumá la pantalla a la lista de `Screens` de
   `tests/EcclesiaCast.ScreenBench.Wpf` con un nombre (`"texto-pedir"`), cómo se
   arma y su ancho. Los diálogos de la tanda 15 están en `DialogScreens.cs`; cada
   tanda puede hacer un archivo igual y sumarlo en `Screens`. Se arma la ventana
   real de `App`, **sin mostrarla**, y se dibuja su contenido, porque una ventana
   no se puede poner adentro de otra cosa. Lo hace `Take(ventana)`:
   - saca el contenido (`var c = (FrameworkElement)w.Content; w.Content = null;`);
   - **le pasa el `DataContext`** de la ventana (`c.DataContext = …`): al
     sacarlo lo pierde y los enlaces salen vacíos;
   - si la ventana tiene altura fija, se la pone al contenido, descontando la
     barra de título.

   **Ojo con `GroupName`:** los botones de opción con nombre de grupo dependen de
   la ventana que los contiene, y al sacar el contenido la foto puede salir mal
   marcada. Si los botones van sueltos en el mismo panel, no hace falta
   `GroupName`: así forman su grupo (`CountdownWindow`).

   Como la ventana no se muestra, `Loaded` no se dispara: nada empieza a
   trabajar.
2. **En Avalonia**, una prueba `[AvaloniaFact]` que arma la misma ventana con los
   mismos datos. Para un diálogo entero se usa `DrawWindow(ventana,
   "texto-pedir", alto)` (en `ScreenBenchTests.Dialogs.cs`), que la muestra en
   memoria, la fotografía y la cierra; `Draw(contenido, nombre)` es para un
   control suelto. El nombre tiene que ser el mismo en los dos lados.

   **Mostrar la ventana dispara `Opened`**, también en memoria. Si la pantalla
   trabaja sola (copia, descarga, búsqueda, lo que sea que salga a internet o
   toque archivos), en el banco va con un **trabajo falso** que no termina
   (`new TaskCompletionSource<bool>().Task`). Con el de verdad, la prueba
   copiaría o bajaría cosas.
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

**Y una prueba del teclado.** Cada diálogo comprueba **Enter** y **Esc** como en
WPF, con la ventana mostrada en memoria y teclas de verdad
(`KeyPressQwerty`), no apretando el botón desde el código: así se encontró lo de
`HolderRadioButton`. El ejemplo está en `DialogKeyTests.cs`. Probá Enter con el
cursor en cada caja de texto.

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

**Ya está (tanda 15):** los diálogos chicos con sus servicios; `IDialogs`
(`AvaloniaDialogs`, abrir y guardar archivos y elegir carpeta; no elige carpeta
de inicio porque el pedido común no la tiene); `NumericField`; el ícono de «va
encima de todo» (`OnTopMark`); `HolderRadioButton`; y en el tema, cajas de
texto, casillas, opciones, enlaces y barra de progreso. `MenuDialogs` (Acerca
de y transiciones) y `FeedbackDialogs` (comentarios) están hechos y **todavía no
los llama nadie**: los conecta la tanda 19.

**Falta, y lo necesita la primera tanda que lo use:**

- Corrector de ortografía en las cajas de texto (`Feature.SpellCheck`): fase 5.
- El menú global de la Mac (`NativeMenu`), Cmd en vez de Ctrl en las teclas y
  «Salir» en el menú del programa: tanda 22.
