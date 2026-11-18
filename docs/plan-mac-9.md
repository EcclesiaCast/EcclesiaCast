# Tanda 9 (Opus): la letra en Avalonia

- **Reglas:** [plan-mac-reglas.md](plan-mac-reglas.md).
- **Contexto:** lo común de la fase está en [plan-mac-fase-2.md](plan-mac-fase-2.md).
  Del plan general, leé sólo las secciones 5.1, 7.2 y 11 («Fuentes») de
  [plan-mac.md](plan-mac.md).
- **Rama:** la de esta sesión, cortada de `mac`.
- **Tamaño:** grande.

## Para qué

La diapositiva es el corazón del programa. En Avalonia tiene que cortar los
renglones y elegir el tamaño de letra **igual** que WPF.

Para asegurarlo, la cuenta tiene que ser **una sola**. Hoy está en
`App/Controls/SlideTextFit.cs` y usa `FormattedText`, que es de WPF. Pasa a
la parte común, y cada interfaz sólo pone la forma de medir.

## Qué hay que hacer

Un commit por punto.

### 1. La cuenta de la letra pasa a `Presentation`

- `SlideTextFit` se muda a `Presentation/Rendering/`, sin WPF:
  - `Thickness` se cambia por un `record` propio para los márgenes.
  - Las mediciones (`Format` y la del pie) se piden a una interfaz nueva,
    `ITextMetrics`, en `Presentation/Platform`.
- `ITextMetrics` mide un bloque de texto.
  - **Recibe:** texto, familia, peso, cursiva, tamaño, ancho máximo o «sin
    cortar», alto de renglón y `pixelsPerDip`.
  - **Devuelve:** ancho, alto y cantidad de renglones, o lo que la cuenta use
    hoy. **Mirá qué lee del `FormattedText` y pedí eso, nada más.**
- `WpfTextMetrics`, en `App/Platform`, hace exactamente lo de hoy con
  `FormattedText`.
- `ApplyCase`, `SplitToFit`, `SplitEvenly` e `IsCaptionOnTop` se mudan junto
  con la cuenta. `WpfTextMeasurer` queda igual por fuera.
- **Comprobación obligatoria:**
  1. Compilá y corré el banco de WPF (`tests/EcclesiaCast.RenderBench.Wpf`).
  2. Compará sus 195 `caso.json` con `tests/casos-de-render/referencia-wpf/`.
  3. Tienen que salir **idénticos**, byte por byte. Si uno cambia, el commit
     no va.
- Las pruebas de la cuenta van en `Presentation.Tests`. Usan una medición
  falsa (ancho = letras × tamaño × 0,5) para que corran también en Mac.

### 2. Proyecto `src/EcclesiaCast.Desktop`

- net10.0, Avalonia 12.1.6, tema Fluent.
- Por ahora es una biblioteca. En la tanda 11 pasa a ser el programa.
- Sus pruebas van en `tests/EcclesiaCast.Desktop.Tests`, con
  `Avalonia.Headless.XUnit`. Tienen que dibujar con Skia de verdad, no con el
  dibujo falso, para que la letra se mida como en pantalla.
- Los dos proyectos se agregan a la solución y a `build.yml`, tanto en
  Windows como en el trabajo `test-mac`.

### 3. Las fuentes que vienen con el programa

- Van en `Desktop/Assets/Fonts/`, cada una con su `OFL.txt`:
  - **Selawik** (regular, semibold y negrita): copiala de `proto/` en la rama
    de la tanda 1.
  - **Carlito:** de `github.com/google/fonts`, carpeta `ofl/carlito`.
  - **Anton:** del mismo lugar, carpeta `ofl/anton`.

  Si no podés bajar de esa dirección, seguí sólo con Selawik y anotalo.
- La tabla de reemplazos va en `Presentation/Rendering/FontSubstitutes.cs`:
  - Segoe UI → Selawik.
  - Calibri → Carlito.
  - Consolas y Cascadia → Menlo en Mac, Consolas en Windows.
- **El reemplazo se usa sólo si la fuente pedida no está instalada.** En una
  PC con Windows y Segoe UI no cambia nada.
- `IFontCatalog` de Avalonia, en `Desktop/Platform/AvaloniaFontCatalog.cs`:
  lista las fuentes instaladas más las que vienen con el programa, y marca
  estas últimas.
- Prueba: pedir «Segoe UI» en una máquina que no la tiene da Selawik.

### 4. `AvaloniaTextMetrics`

- Usa `TextLayout` de Avalonia y cumple `ITextMetrics`.
- Prueba: un párrafo conocido da la misma cantidad de renglones que en el
  `caso.json` de WPF con el mismo texto.

### 5. `SlideView` en Avalonia

Va en `Desktop/Controls/SlideView.axaml` y su `.cs`, con las mismas
propiedades que la de WPF (`DependencyProperty.Register` pasa a
`StyledProperty`). Tiene que incluir:

- Texto principal, segunda versión y pie.
- Cuadros de texto extra.
- Mayúsculas, interlineado y ajuste al ancho.
- **Contorno y sombra:**
  - Se hacen con `DropShadowEffect`, igual que en WPF.
  - El contorno hoy es una sombra «halo». Copiá cómo lo arma `BuildOutline`.
  - Si en Avalonia no se ve parecido, anotalo. El plan B (dibujar el contorno
    como trazo) lo decide la tanda 13.
- **Franjas** (`SlideView.Bands.cs`):
  - Si se puede separar la cuenta de dónde va cada franja, va a
    `Presentation`.
  - Lo que lee los renglones del control queda en cada interfaz.
- Logo de texto, cuenta regresiva y anuncio en el tercio inferior.
- Los fundidos de entrada y salida, con la misma duración que en WPF. Usan un
  ayudante único, `Fade`, en `Desktop/Controls/Fades.cs` (sección 5.1,
  punto 4).

**Queda afuera:** el resaltado de selección (`ShowSelection`,
`PhraseBetween`), que es del operador y corresponde a la fase 3. Dejá una
nota.

### 6. `StageBoxView` en Avalonia

Con lo mismo que tiene la de WPF.

### 7. Pruebas que dibujan

- En `Desktop.Tests`, dibujá a 1920×1080 en memoria estos cinco casos de
  `tests/casos-de-render/`:
  - canción de 4 renglones;
  - Biblia con dos versiones;
  - franja;
  - contorno con sombra;
  - cuenta regresiva.
- Guardá las imágenes en `artifacts/render/avalonia-tanda-9/`.
- Mirá los cinco PNG con Read y decí en el informe si se ven como una
  diapositiva de verdad.
- El banco completo es la tanda 12.

### 8. `CHANGELOG.md`

Una línea al final de «Sin publicar», del estilo «Por dentro, sin cambios
para quien usa el programa».

## Terminado cuando

- El build sale sin advertencias y las pruebas pasan en Windows.
- El banco de WPF da las 195 medidas idénticas a la referencia.
- Los cinco PNG de Avalonia se ven como diapositivas.
- El informe muestra, para esos cinco casos, los renglones de Avalonia al
  lado de los de WPF (están en el `caso.json` de la referencia).

## Qué no hacer

- **No tocar cómo se ve nada en WPF.** Si para mudar la cuenta hubiera que
  cambiar aunque sea un redondeo, no se cambia: se deja una conversión que dé
  lo mismo.
- **No hacer todavía** el video, `ProjectedView` ni las ventanas. Son de las
  tandas 10 y 11.
