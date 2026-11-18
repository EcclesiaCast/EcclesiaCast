# Plan de trabajo para Sonnet — hoja de ruta del 10/11/2026

Sale del análisis [analisis-2026-11.md](analisis-2026-11.md). Alexis aprobó
cada punto. El seguimiento por audio («karaoke») **no** es de este plan: lo
hace otra sesión en paralelo, en `src/EcclesiaCast.Core/Listening/`,
`src/EcclesiaCast.App/Services/Listening/`, `MainViewModel.Listening.cs` y
`Views/ListeningWindow.*`. No toques esos archivos.

## 0. Reglas

- **No abrir el programa ni ninguna ventana.** Alexis trabaja en esta PC.
  Se prueba con `dotnet build` y `dotnet test`. Nunca ejecutar
  `EcclesiaCast.App.exe`.
- No tocar la biblioteca real (`%APPDATA%\EcclesiaCast`).
- Archivos con texto en español: **Edit/Write, nunca sed, perl ni
  Set-Content** (rompen los acentos). Al terminar cada punto:
  `grep -rn "Ã\|â€" src` tiene que dar vacío.
- Código, comentarios y commits en inglés; todo lo que lee el operador, en
  español rioplatense (voseo), sin jerga.
- Un commit por punto, mensaje en inglés, **sin** línea de coautor. Sin push.
- Antes de cada commit: `dotnet build EcclesiaCast.slnx` sin errores ni
  advertencias nuevas, y `dotnet test EcclesiaCast.slnx` en verde.
- Lo que sea lógica pura va en `EcclesiaCast.Core` con pruebas xUnit.
- Cada punto suma su renglón en `CHANGELOG.md`, sección «Sin publicar».
- Buscá con grep; no leas enteros `MainWindow.xaml` (2.500 líneas) ni los
  parciales grandes de `MainViewModel`.
- Si un punto no se puede hacer como está escrito, no inventes: dejalo y
  explicalo en el informe final.

## 1. Un error inesperado no frena al operador

`App.xaml.cs`, `DispatcherUnhandledException`: hoy muestra un `MessageBox`
que hay que cerrar para seguir pasando la letra, y si el error se repite los
carteles se apilan.

- Sin cartel. El error va al registro (ya va) y a la barra de estado de la
  ventana principal: «Hubo un error; el programa sigue. Quedó anotado en el
  registro.» (`MainViewModel.StatusText`, si la ventana principal ya existe;
  antes de que exista, sí cartel, porque es un error de arranque).
- Como mucho un aviso cada 30 segundos: la decisión «¿aviso ahora?» va en una
  clase chica en Core (`Diagnostics/ErrorNoticeGate` o parecido, con el reloj
  por parámetro) con pruebas.
- `args.Handled = true` como hoy.

## 2. Las caídas en segundo plano quedan en el registro

En `App.xaml.cs`, junto al manejador de arriba:

- `AppDomain.CurrentDomain.UnhandledException`: `Log.Fatal` con la excepción
  y `Log.CloseAndFlush()` (el proceso se cae igual; que quede escrito).
- `TaskScheduler.UnobservedTaskException`: `Log.Error` y `SetObserved()`.

## 3. Doble Esc para apagar la salida

Hoy `Esc` (KeyBinding en `MainWindow.xaml`, comando `HideOutputCommand`)
apaga la salida al primer toque, y es la tecla que todos aprietan para
«cancelar».

- Con la salida prendida, el primer Esc sólo avisa en la barra de estado:
  «Esc otra vez para apagar la salida.» El segundo, dentro de 2 segundos, la
  apaga. Pasados los 2 segundos vuelve a pedir dos.
- La regla (¿este Esc apaga?) en Core con el reloj por parámetro y pruebas.
- El botón «Apagar salida» y el menú siguen apagando con un clic.
- Si hay textos que dicen «Esc» como atajo de apagar (botón, tooltips,
  `docs/guia-de-uso.md`, recorrido de bienvenida), que digan «Esc dos veces».
- El celular (`/api/cmd`) no cambia.

## 4. Ayuda en las listas vacías

Cuando no hay nada, la lista dice qué hacer, con botones ahí mismo. Seguí el
estilo del estado vacío que ya existe en Medios (buscá «Lista vacía: dice qué
hacer» en `MainWindow.xaml`).

- **Canciones** sin ninguna canción en la biblioteca (no cuando el buscador
  no encuentra): «Todavía no hay canciones.» + botones «Buscar en internet»,
  «Traer de otro programa» y «Escribir una nueva» (los comandos ya existen en
  el menú Archivo y Edición).
- Canciones con el buscador sin resultados: «Ninguna canción con eso.»
- **Playlist** vacía: «Esta playlist está vacía. Clic derecho en una canción
  → Agregar a la playlist, o arrastrala acá.» (comprobá que arrastrar exista;
  si no, sacá esa parte).

## 5. Pestañas del diseñador de canciones

`Views/SongDesignerWindow.xaml` tiene un `TabControl` con «Texto» y «Caja»
con el aspecto blanco de Windows. Que se vean como las pastillas de
`Views/ThemeManagerWindow.xaml` (estilo `SectionPill`, basado en
`TabButton`): dos `RadioButton` que muestran un panel u otro. Los dos
paneles tienen que seguir **dentro de la ventana aunque estén ocultos**
(el código de atrás usa sus controles por nombre). No cambies nada más de
esa ventana.

## 6. Papelera para canciones y temas

Borrar una canción o un tema hoy es definitivo.

- `Song` y `SlideTheme` suman `DeletedAt` (`DateTime?`, UTC). Migración EF
  (`dotnet ef migrations add AddTrash` en `src/EcclesiaCast.Data`; mirá cómo
  están hechas las últimas). Probá la migración sobre una base nueva en una
  prueba.
- `Delete` de los dos repositorios marca `DeletedAt` en vez de borrar.
  `Search`, `GetTitles`, `GetAll` y `Get` no devuelven lo borrado. Métodos
  nuevos en las interfaces: `GetDeleted()`, `Restore(int id)`,
  `Purge(int id)` y `PurgeOlderThan(DateTime limit)`.
- Al arrancar (`App.xaml.cs`, después de migrar) se purga lo que lleva más
  de 30 días.
- Lo que apunta a algo borrado no se rompe: una playlist con una canción en
  la papelera la saltea como si no existiera; una canción cuyo tema está en
  la papelera usa el tema por defecto (ya pasa cuando el tema no existe).
  Un tema que es el predeterminado no se puede mandar a la papelera sin que
  `ThemeSeeder.EnsureDefaults` elija otro (ya se llama después de borrar).
- Ventana nueva `Views/TrashWindow` (menú Edición → «Papelera…»): lista de
  canciones y temas borrados, con cuándo se borró y cuántos días le quedan;
  botones «Restaurar», «Borrar del todo» y «Vaciar la papelera» (los dos
  últimos preguntan). Aspecto como las demás ventanas (`BgBrush`, `Panel`,
  `DarkButton`).
- Los carteles de borrar canción y tema dicen que va a la papelera y que se
  guarda 30 días.
- El respaldo y los paquetes no cambian.

## 7. Paleta de colores más fácil

`Views/ColorPickerWindow.*` (la usan temas, diseñador, logos, escenario).
Hoy: 18 colores y tres barras R, G, B.

- Paleta grande arriba: una grilla de unos 10 tonos × 6 claridades más una
  fila de grises de blanco a negro. Un clic elige.
- Fila «Usados hace poco» (hasta 12, sin repetir, el último primero). Se
  guardan en los ajustes (`ISettingsStore`, clave `colors.recent`), así
  quedan de un día a otro. La lógica de la lista (agregar, tope, sin
  repetidos, leer y escribir el texto) en Core con pruebas.
  `ColorPickerHelper.Pick` es estático: dale acceso a los ajustes como ya lo
  hagan otras clases estáticas del proyecto, o con una propiedad estática que
  se asigna en `App.xaml.cs`.
- Las tres barras R/G/B salen de la vista principal: quedan detrás de un
  botón «Ajuste fino» que las despliega, y ahí en vez de R/G/B son
  «Tono», «Intensidad» y «Claridad» (HSV), que es como piensa la gente. La
  conversión HSV↔RGB en Core con pruebas.
- El recuadro con el color elegido y el código se quedan. Enter acepta
  (`IsDefault` en Aceptar). «Transparente» sigue como está.
- La firma de `ColorPickerHelper.Pick` no cambia.

## 8. Ventana de Ajustes única

Las preferencias están repartidas en cuatro menús. Ventana nueva
`Views/SettingsWindow` (menú Archivo → «Ajustes…», antes de «Salir», con
`Ctrl+,`), con secciones a la izquierda y el contenido a la derecha:

- **Pantallas**: salida y pantalla de escenario (lo que hoy está arriba a la
  derecha y en Proyección) + «Actualizar la lista».
- **Sonido**: «Reiniciar el sonido» con su explicación.
- **Transiciones**: lo que hoy tiene `TransitionsWindow` (podés incrustar su
  contenido o dejar un botón que la abre; lo que sea menos riesgoso).
- **Celular**: botón que abre la ventana del control remoto.
- **Rendimiento**: «Modo PC de bajo rendimiento».
- **Actualizaciones**: «Buscar actualizaciones al abrir» y «Buscar ahora».
- **Aspecto**: diseño de aprendizaje / avanzado.

No saques nada de los menús todavía (eso se decide después de probar): la
ventana usa los mismos comandos y ajustes que ya existen, no duplica estado.
La otra sesión va a sumar una sección «Escuchar la canción»: dejá la lista de
secciones fácil de extender (una clase o un panel por sección).

## Informe final

Al terminar, un resumen corto: qué quedó hecho (con el hash de cada commit),
qué no y por qué, y **qué tiene que mirar Alexis con el programa abierto**
para cada punto.
