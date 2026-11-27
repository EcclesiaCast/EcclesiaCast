# Tanda 17 (Sonnet) — Diseño

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (todas). Cómo se traduce
cada pantalla: [guia-wpf-a-avalonia.md](guia-wpf-a-avalonia.md) (**leela
entera antes de empezar**: se corrigió con lo que dejó la tanda 16). Contexto:
[plan-mac-fase-3.md](plan-mac-fase-3.md).

**Rama:** la de esta sesión, cortada de `mac` (con la tanda 16 revisada).
**Tamaño:** grande. **Dos sesiones en fila** (17a y 17b), cada una con su
revisión. Hacé sólo la parte que dice tu instrucción; no arranques la otra ni la
tanda 18.

Lo que sigue se comprobó contra el código el 27/11/2026. Si algo no coincide,
gana el código y se anota en el informe.

## Para qué

Que en la Mac funcionen las ventanas de diseño, que hoy responden «Esto
todavía no está en la versión de Mac» (`NotOnMacYet` en
`Desktop/Composition.cs`), con el mismo aspecto que en Windows.

## Antes de tocar nada

- **El banco de WPF nunca corre el dispatcher** (guía, sección 5). Varias de
  estas ventanas tienen relojes (`DispatcherTimer` en el diseñador de placas),
  arrastres y vistas previas que se dibujan «después». En el banco, todo eso se
  llama a mano; nunca `PushFrame`, `DoEvents`, `Dispatcher.Yield` ni esperar.
  Antes de correr el banco, buscá esas palabras en `tests/EcclesiaCast.ScreenBench.Wpf`.
- **Sumá primero las pantallas al banco de WPF «como están hoy»**, en un commit
  aparte y antes de tocar WPF, para tener con qué comparar (como las tandas 15 y
  16). Los datos de muestra van en `tests/pantallas-de-muestra/`, que compilan
  los dos bancos.
- **Pedir archivos** es `IDialogs` (con `InitialDirectory` si hace falta).
  **Abrir un archivo con su programa** no existe todavía: `IShell` sólo tiene
  `OpenUrl`, `OpenFolder` y `ShowInFolder`. Sumale `OpenFile(string path)`, con
  Windows igual que hoy (`Process.Start` con `UseShellExecute`) y la Mac con
  `open`. Nada de `Microsoft.Win32` ni `Process.Start` en lo común ni en
  Avalonia.

## 17a — Colores, temas, medios y logos (primera sesión)

| Pantalla de WPF | Interfaz | Qué decide hoy el `.xaml.cs` de WPF (mudarlo primero) |
|---|---|---|
| `ColorPickerWindow` (+ `Services/ColorPickerHelper.cs`) | `IColorPicker` | Los colores usados últimamente (`RecentColors`, que guarda en `ColorPickerHelper.Settings`), «Transparente» (`ColorPickerWindow.Transparent`) y pasar entre texto `#RRGGBB` y color. Va a un ViewModel común con `ISettingsStore`; el `Settings` estático de WPF deja de hacer falta en lo común |
| `ThemeManagerWindow` | `IThemeManagerDialog` | Ya tiene `ThemeManagerViewModel`. El `.xaml.cs` mueve la caja de texto en la vista previa (`Canvas`, escala, `BoxX`/`BoxY` redondeados): la cuenta de escala y redondeo va al ViewModel; el arrastre queda en la pantalla |
| `MediaInspectorWindow` | `IMediaInspector.Edit` | Ya tiene `MediaInspectorViewModel`. Abre el archivo con `Process.Start` si existe: pasa a `IShell.OpenFile` (nuevo, ver arriba) |
| `MediaTabRuleWindow` | `IMediaInspector.EditTabRule` | Lo que se elige para la pestaña y si abre como presentación (`ref bool presentation`): mirá qué decide la ventana y mudalo si decide algo |
| `LogoManagerWindow` | `ILogoManagerDialog` | Ya tiene `LogoManagerViewModel`. Pide la imagen o el video del logo con `Microsoft.Win32.OpenFileDialog` y sus filtros: pasa a `IDialogs` con los mismos filtros |

- El selector de colores va **primero**: lo usan los temas, los medios, los
  diseñadores y «Color de fondo…» de Ajustes. Al terminarlo, comprobá con una
  prueba que Ajustes ya lo abre en la Mac (la tanda 16 lo dejó enganchado a
  `IColorPicker`).
- El selector tiene que abrirse **encima de la ventana que lo pidió** (en WPF,
  la activa; en Avalonia, `Modal.Owner`).
- El editor de temas es la ventana más larga de esta tanda (878 renglones de
  XAML): compará la imagen lado a lado por partes, no sólo de lejos.

## 17b — Diseñadores (segunda sesión)

| Pantalla de WPF | Interfaz | Qué decide hoy el `.xaml.cs` de WPF (mudarlo primero) |
|---|---|---|
| `SongDesignerWindow` | `ISongDesigner` | Ya tiene `SongDesignerViewModel`, pero el `.xaml.cs` tiene 802 renglones y unas 47 decisiones (`if`): qué diapositiva queda elegida, qué se copia a las demás, cuándo se prenden los botones, y la **diapositiva nueva vacía** (`InsertSlideAfter`, de la 1.5.0, que en la Mac tiene que estar). Todo lo que no sea arrastrar y dibujar va al ViewModel |
| `StageDesignerWindow` | `IStageDesigner` | Ya tiene `StageDesignerViewModel`. Unos 27 `if` en el `.xaml.cs`: las mismas reglas |
| `NamePlateDesignerWindow` | `INamePlateDesigner` | La vista previa entra, espera y sale con un `DispatcherTimer`: el tiempo (`ExitMs + 900`) y qué se muestra en cada momento van a lo común con `IUiTimer` |

- Los diseñadores dibujan la diapositiva con `SlideView`: en Avalonia ya existe
  (tanda 9). No se hace otro.
- Arrastrar cajas con el mouse: probalo en memoria con `MouseDown`,
  `MouseMove` y `MouseUp` de `Avalonia.Headless`, y comprobá que la posición
  que guarda el ViewModel es la misma que en WPF para el mismo arrastre.

## Para las dos sesiones

Cada ventana con su servicio en `Desktop/Services`, su línea en
`Composition.cs` en vez de `NotOnMacYet`, su imagen en los dos bancos y su
prueba del teclado. Un commit por ventana, y uno por cada lógica que se muda.

**El teclado:** Enter y Esc en cada diálogo, con teclas de verdad
(`DialogKeyTests`), con el foco en cada clase de control (caja de texto, lista,
casilla, opción, desplegable): la guía (2.1) dice por qué. En el editor de
temas y los diseñadores de canción y de escenario, además, las flechas que
mueven la caja elegida (con su paso) y, en el de escenario, Supr: lo que haga
WPF, con teclas de verdad.

## No entra

- Pixabay, control remoto y transmisión: tanda 18. Conectar el menú: tanda 19.
- Elegir una ventana para capturar (`IWindowPickerDialog`): en la Mac no se
  captura todavía (fase 6).
- Corrector de ortografía (fase 5).
- La falla de Windows de «Importar canciones» que se cierra mientras importa:
  espera la decisión de Alexis. No la toques.

## Terminado (de cada sesión)

- Compila sin advertencias. Pasan todas las pruebas: núcleo, parte común y Mac,
  más las nuevas. (Al empezar la 17a: 1.762, 387 y 193.)
- El banco de render de WPF da idéntico a la referencia y el de Avalonia sigue
  en 195 de 195.
- Cada pantalla tiene su imagen de WPF y de Avalonia lado a lado, y la miraste
  entera (bordes incluidos). Lo que queda distinto va en el informe, con el
  porqué.
- Windows funciona igual: las pruebas muestran que lo mudado no cambió, y las
  imágenes de WPF del banco de pantallas son iguales antes y después.
- Ninguna pantalla nueva decide nada que no esté en su ViewModel o servicio.
- La compilación en una Mac queda sin comprobar hasta que se suba: decilo.
- `CHANGELOG.md`, «Sin publicar»: una línea por lo que se nota.

**Informe final**, como piden las reglas, más: qué de la guía no sirvió o
faltaba (para corregirla antes de la parte siguiente).

## Instrucción para pegar en la sesión nueva

Para la primera sesión:

```text
Hacé la tanda 17a de la versión de Mac. Hablame siempre en español, simple.
Tu rama sale de mac. Si tu rama salió de main y no tiene commits propios, adelantala hasta mac.
Leé enteros antes de empezar: docs/plan-mac-reglas.md (cumplí todas las reglas), docs/plan-mac-17.md, docs/guia-wpf-a-avalonia.md y docs/plan-mac-fase-3.md.
Hacé sólo la parte «17a — Colores, temas, medios y logos». No arranques la 17b.
No abras el programa ni ninguna ventana. Antes de correr el banco de WPF, revisá que no corra el dispatcher. No toques mi biblioteca.
No hagas push ni fusiones: lo reviso yo.
Al final, el informe que pide el encargo y un resumen corto.
```

Para la segunda, después de revisar y fusionar la primera, la misma
instrucción cambiando «17a — Colores, temas, medios y logos» por «17b —
Diseñadores» y «No arranques la 17b» por «No arranques la tanda 18».
