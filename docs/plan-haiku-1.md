# Plan de trabajo para Haiku — arreglos chicos de la prueba del 11/10

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2):
- No abrir la aplicación ni ninguna ventana visible. Alexis está usando la PC.
  Para lo visual, dibujá a PNG desde un programa de consola STA en tu carpeta
  temporal (nunca dentro del repo).
- No escribir nunca en la biblioteca real
  (`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`).
- No ejecutar python: no está instalado y llamarlo abre la Microsoft Store.
- Rama `beta6`, sin push. **Un commit por punto**, mensaje en inglés en
  imperativo, y al final:
  `Co-Authored-By: Claude Haiku 5.5 <noreply@anthropic.com>`
- Castellano rioplatense para todo lo que se ve («elegí», «podés»).
- Los archivos tienen fin de línea CRLF y acentos: **usá la herramienta Edit**.
  Nunca sed, perl ni Set-Content (rompen los acentos). Después de cada punto:
  `grep -rn "Ã\|â€" src` tiene que dar vacío.
- Antes de cada commit: `dotnet build` sin errores ni advertencias nuevas y
  `dotnet test tests/EcclesiaCast.Core.Tests` en verde.
- Anotá cada punto en `CHANGELOG.md`, sección `[Sin publicar]`, con el estilo
  de las entradas que ya hay (negrita para el título, una o dos frases para
  alguien que no programa).
- Si algo no coincide con lo que dice este plan (líneas movidas, nombres
  distintos), buscá el lugar correcto; si no lo encontrás, saltealo y
  explicalo en el informe. No improvises arreglos grandes.

## H1. Tooltip de la grilla que habla de «reordenar» en la Biblia

La tarjeta de la grilla central tiene un ToolTip fijo para todo:
«Clic: proyectar · Arrastrar: cambiarla de lugar · Clic derecho: más opciones»
(`MainWindow.xaml`, cerca de la línea 1151). En la Biblia no se puede
arrastrar.

- En `ViewModels/SlideItemViewModel.cs` agregá `public string CardToolTip`:
  - `JumpTarget != null`: si `JumpToEnd`, «Clic: abre el capítulo anterior en
    su último versículo»; si no, «Clic: abre el capítulo siguiente en su
    primer versículo».
  - `Verses != null` (versículo): «Clic: proyectar · Clic derecho: agregar a la
    playlist o corregir el texto».
  - `SectionId != 0` (canción): el texto actual.
  - Cualquier otro caso: «Clic: proyectar».
- En el XAML: `ToolTip="{Binding CardToolTip}"`.
- Verificá los nombres reales de las propiedades (`JumpTarget`, `JumpToEnd`,
  `Verses`, `SectionId`) en `SlideItemViewModel`.

## H2. El fondo de imagen del tema queda pegado encima de los medios

La imagen de fondo del tema (`BackgroundImage` + `DimLayer` en
`Controls/SlideView.xaml`) se aplica en `ApplyTheme()` de
`SlideView.xaml.cs` sin mirar `_overMedia`. `IsOverMedia` sólo vuelve
transparente el **color** del tema, no la imagen. Como `SlideView` está encima
de los medios en `ProjectedView`, la imagen del tema de la Biblia tapa el
medio que el operador eligió después.

Regla: **mientras hay un medio en pantalla, gana el medio.**
- Sacá de `ApplyTheme` las líneas que deciden la visibilidad de
  `BackgroundImage` y la opacidad de `DimLayer` y llevalas a un método
  `ApplyThemeBackdrop()`, que se llama desde `ApplyTheme()`, desde
  `OnStateChanged()` y desde donde cambia `IsOverMedia`.
  - `BackgroundImage.Visibility = !_overMedia && BackgroundImage.Source != null ? Visible : Collapsed`
    (que `ApplyBackgroundImage` siga cargando la imagen; la visibilidad la
    decide este método).
  - `DimLayer.Opacity = _overMedia && State != OutputState.Content ? 0 : Math.Clamp(theme.BackgroundDim, 0, 1)`.
- Verificá con un render a PNG desde consola STA: `ProjectedView` de
  1920×1080 con una diapositiva cuyo tema tenga `BackgroundImagePath` = una
  imagen roja, `BackgroundMedia` = imagen verde y estado `Clear`: el centro
  tiene que salir verde. Sin `BackgroundMedia`, rojo. Si `ProjectedView`
  necesita recursos de `App`, probá con `SlideView` solo con `IsOverMedia`
  true/false.

## H3. Los cuadros de texto extra no se van con «Clear» (F2)

En `SlideView.xaml.cs`, `OnStateChanged` oculta `TextLayer` y
`CaptionLayer` pero no `ExtraBoxLayer`, así que con F2 la letra se va y los
cuadros extra quedan en pantalla.
- En `OnStateChanged`: `ExtraBoxLayer.Visibility` igual que `TextLayer`.
- En `OnSlideChanged`, si se anima la entrada de `TextLayer`
  (`TransitionIn(TextLayer)`), hacé lo mismo con `ExtraBoxLayer`.
- Render a PNG: `SlideView` con `SlideContent` cuyo `Override` tenga un
  cuadro extra, estado `Content` (se ve) y `Clear` (no se ve).

## H4. Tipografía de las notas (y de cualquier recuadro del escenario)

Hoy `StageBox` (`Core/Stage/StageLayout.cs`) tiene tamaño, color,
alineación y negrita, pero no tipografía.
1. Agregá `public string? FontFamily { get; set; }` a `StageBox`.
2. En `StageLayoutStore.Sanitize`: recortá espacios y convertí vacío en null.
   El JSON viejo, sin el campo, se lee como null.
3. En `Controls/StageBoxView.cs` (`Apply`): si hay tipografía,
   `_text.FontFamily = new FontFamily(Box.FontFamily)`; si no, que herede.
4. En `Views/StageDesignerWindow.xaml`, antes de «Tamaño de letra»: rótulo
   «Tipografía» y un `ComboBox x:Name="FontBox" IsEditable="True"` con la
   lista de `Fonts.SystemFontFamilies` ordenada (mismo patrón que
   `SongDesignerWindow.xaml.cs`). En `Select()` cargá
   `box.FontFamily ?? "Segoe UI"`. En el cambio (SelectionChanged y
   LostFocus) guardá en `box.FontFamily` y llamá a `Touch()`, igual que el
   cambio de tamaño.
5. Pruebas en `tests/EcclesiaCast.Core.Tests/Stage/` (buscá el archivo de
   pruebas de `StageLayoutStore`): el ida y vuelta conserva `FontFamily`, y
   un JSON sin el campo da null.

## H5. Tipografía del texto rápido

Hoy el texto rápido usa `new SlideContent(QuickText, Theme: DefaultSongTheme)`
en `MainViewModel.cs` (buscá `QuickText`), así que hereda la letra del tema de
canciones.
- En `MainViewModel`: `[ObservableProperty] private string _quickTextFont = "";`
  (vacío = la del tema). Cargalo de `_settings.Get("quicktext.font")` donde se
  cargan los otros ajustes (por ejemplo junto a `StageNotesText`). En
  `OnQuickTextFontChanged` guardalo con `_settings.Set` y refrescá la vista
  previa del texto rápido si corresponde.
- Un helper `SlideContent QuickSlide(string text)` que devuelve
  `new SlideContent(text, Theme: DefaultSongTheme, Override: string.IsNullOrWhiteSpace(QuickTextFont) ? null : new SlideOverride(FontFamily: QuickTextFont))`
  (verificá la firma real de `SlideOverride`) y usalo en los lugares donde
  hoy se arma el `SlideContent` del texto rápido.
- `public IReadOnlyList<string> SystemFonts { get; }` en el VM con
  `Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList()`
  (si ya existe algo así, reusalo).
- En la pestaña Texto rápido de `MainWindow.xaml`: arriba, un rótulo
  «Tipografía» y un `ComboBox` editable enlazado a `QuickTextFont` con
  `ItemsSource="{Binding SystemFonts}"`. Que no quede apretado.

## Informe final

Para cada punto: commit (hash), qué hiciste, cómo lo verificaste, y lo que
quede «para probar con el programa abierto».
