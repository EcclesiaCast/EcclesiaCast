# Plan de trabajo para Sonnet — avisos con el aspecto del programa

12/11/2026. Hoy el programa usa 47 veces `MessageBox.Show`: los avisos salen
grises, con el aspecto de Windows, y con botones «Sí / No / Cancelar» que no
dicen qué pasa. Alexis pidió reemplazarlos por avisos con el diseño del
programa.

## 0. Reglas

- **No abrir el programa ni ninguna ventana.** Alexis trabaja en esta PC.
  Sólo `dotnet build EcclesiaCast.slnx` y `dotnet test EcclesiaCast.slnx`.
  Nunca ejecutar `EcclesiaCast.App.exe` ni `dotnet run`.
- No tocar la biblioteca real (`%APPDATA%\EcclesiaCast`).
- Archivos con texto en español: **Edit/Write, nunca sed, perl ni
  Set-Content**. Al terminar: `git grep -n "Ã\|â€" -- src` vacío.
- Código, comentarios y commits en inglés; lo que lee el operador, en
  español rioplatense (voseo). Siempre «español», nunca «castellano».
- Commits sin línea de coautor. Sin push ni PR.
- Antes de cada commit: build sin errores ni advertencias nuevas y pruebas
  en verde.
- Otra sesión trabaja en paralelo en Pexels: **no toques**
  `MainViewModel.Media.cs`, `MainViewModel.BundledBackgrounds.cs`,
  `Services/Pexels*`, `Views/PexelsWindow*` ni la barra de medios de
  `MainWindow.xaml`. Si alguno de los 47 avisos está en esos archivos,
  dejalo y anotalo en el informe.

## 1. La ventana de aviso (un commit)

`Views/NoticeWindow.xaml(.cs)` y una fachada estática
`Services/Notice.cs` que es lo único que usa el resto del programa.

**Aspecto** (copiá estilos de `Views/TransitionsWindow.xaml` y
`Views/SettingsWindow.xaml`, que ya son del programa):

- Fondo `BgBrush`, sin barra de título de Windows propia más allá de la que
  oscurece `DarkTitleBar` (mirá cómo la usan las otras ventanas), ancho fijo
  460, alto según el contenido (`SizeToContent="Height"`), sin cambiar de
  tamaño, `ShowInTaskbar="False"`, centrada en su dueña.
- A la izquierda, un círculo de 40 px con un signo según el tipo:
  información «i» en `AccentBrush`, pregunta «?» en `AccentBrush`, cuidado
  «!» en `#E8C34A`, error «×» en `LiveBrush`. Letra blanca adentro.
- A la derecha, un título en negrita de 16 px (opcional) y el mensaje en 13 px
  `MutedBrush`, con saltos de línea respetados y ajuste de renglón.
- Abajo a la derecha, los botones: el principal con `AccentButton` (o con
  fondo `LiveBrush` si es una acción que borra), los demás `DarkButton`.
  `Padding="18,8"`. El principal es `IsDefault`; el de cancelar, `IsCancel`.
- **El dueño** es la ventana activa
  (`Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)`),
  si no la principal, si no ninguna (avisos de arranque, antes de que haya
  ventana: ahí `WindowStartupLocation.CenterScreen` y `Topmost`).
- Sonido de sistema como el de Windows (`SystemSounds.Asterisk/Exclamation/Hand`)
  según el tipo, para que no se pierda.

**La fachada** (todo devuelve en el hilo de la ventana; si se llama desde
otro hilo, que pase por el `Dispatcher`):

```csharp
public enum NoticeKind { Info, Warning, Error, Question }

public static class Notice
{
    // Aviso de un solo botón.
    public static void Show(string message, string? title = null,
                            NoticeKind kind = NoticeKind.Info, string ok = "Entendido");

    // Pregunta de dos botones: true si eligió «yes».
    // Los textos son verbos de lo que pasa: «Eliminar» / «Cancelar»,
    // «Restaurar» / «Ahora no», nunca «Sí» / «No».
    public static bool Confirm(string message, string yes, string no = "Cancelar",
                               string? title = null, bool destructive = false);

    // Tres caminos: true = yes, false = no, null = cancelar o cerró la ventana.
    public static bool? Choose(string message, string yes, string no,
                               string cancel = "Cancelar", string? title = null);
}
```

Las pruebas de la lógica que no es ventana (por ejemplo, qué devuelve
`Choose` según el botón) no hacen falta: es todo interfaz.

## 2. Reemplazar los 47 (un commit por grupo de archivos)

Buscalos con `git grep -n "MessageBox.Show" -- src`. Por cada uno:

1. Elegí el método según lo que pregunta: `OK` → `Notice.Show`;
   `YesNo` → `Notice.Confirm`; `YesNoCancel` → `Notice.Choose`.
2. El tipo según el ícono que tenía: `Information` → `Info`,
   `Warning` → `Warning`, `Error` → `Error`, `Question` → `Question`
   (en `Confirm` el círculo es «?» siempre, salvo `destructive`, que va «!»).
3. **Los textos de los botones** salen de lo que hace cada pregunta. Ejemplos:
   - «¿Eliminar el tema…?» → `Confirm(msg, "Eliminar", destructive: true)`
   - «¿Querés guardar los cambios?» (editor de temas) →
     `Choose(msg, "Guardar", "No guardar", "Seguir editando")`
   - «¿Restaurar la copia?» → `Confirm(msg, "Restaurar", "Ahora no")`
   - Avisos de error → `Show(msg, kind: NoticeKind.Error)`.
   Leé cada mensaje: si el texto dice «Sí — … / No — …» explicando los
   botones (pasa en `ThemeManagerViewModel`), sacá esas líneas, porque ahora
   los botones dicen lo que hacen.
4. **El título** era casi siempre «EcclesiaCast»: ahora va `null` (sin
   título) o una frase corta que diga de qué se trata («No se pudo bajar la
   Biblia»), y el mensaje queda como detalle.
5. **No cambies cuándo aparece cada aviso ni qué pasa después.** Sólo cómo
   se ve y qué dicen los botones. Si un `MessageBoxResult.X` se compara
   después, traducilo al `bool` / `bool?` sin cambiar la lógica.
6. `App.xaml.cs`: el aviso de error de arranque (antes de que exista la
   ventana principal) también pasa a `Notice`, que sin dueño se centra en
   la pantalla.

`ThemeManagerViewModel.AskWithDialog` devuelve `bool?`: queda como
`Notice.Choose(...)` con los mismos tres significados.

Al terminar, `git grep -n "MessageBox" -- src` sólo puede encontrar la
fachada (si la usa por dentro para algo) o nada.

## 3. CHANGELOG

Un renglón en «Sin publicar» → «Cambiado»: «Los avisos y preguntas del
programa tienen su diseño (oscuros, con botones que dicen lo que hacen) en
vez de los carteles grises de Windows.»

## Informe final

En español, corto: el hash de cada commit; cuántos avisos cambiaste y cuáles
dejaste (y por qué); una lista de los textos de botones que elegiste para
cada pregunta, así Alexis los revisa; y qué mirar con el programa abierto
(por ejemplo: borrar un tema, cerrar el editor de temas con cambios,
restaurar una copia, un error al bajar una Biblia).
