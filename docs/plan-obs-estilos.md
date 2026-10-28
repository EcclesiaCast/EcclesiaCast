# Plan: menú Transmisión y estilos propios para OBS

Pedido por Alexis el 28/10/2026. Las capas separadas (`/salida/biblia`,
`/salida/canciones`, `/salida/anuncios`) ya las probó en OBS y llegan bien;
ahora quiere que **cada capa tenga su propio diseño**, pensado para la
transmisión y no copiado del proyector, con plantillas como las de Reuna
(ver abajo), y que lo de OBS deje de estar dentro de la ventana del celular.

Todo el texto que ve el usuario va en castellano rioplatense. Comentarios de
código en inglés, como el resto del proyecto. No abrir el programa: se
comprueba con compilación, pruebas y dibujando a imagen.

## 1. Menú «Transmisión»

En `MainWindow.xaml`, un menú nuevo `_Transmisión` entre «Proyección» y
«Ayuda», con:

- **Salida para OBS o vMix…** → ventana nueva `BroadcastWindow` (ver 2).
- **Estilos de la transmisión…** → ventana nueva `BroadcastStyleWindow` (ver 4).

Las dos usan el mismo servidor que el celular (`_remote` en
`MainViewModel.Remote.cs`): si está apagado, «Salida para OBS» lo prende con
`StartRemote()` (igual que `OpenRemote`) y lo deja prendido. Comandos nuevos
`OpenBroadcastCommand` y `OpenBroadcastStylesCommand` en `MainViewModel.Remote.cs`.

## 2. Ventana «Salida para OBS o vMix»

Se mueve **tal cual** la caja «Para la transmisión (OBS o vMix)» de
`RemoteControlWindow.xaml` (la dirección general, los tres botones Copiar:
Biblia / Canciones / Anuncios y su texto) a `Views/BroadcastWindow.xaml`, con
el mismo estilo oscuro de las demás ventanas. Se le suma:

- Una línea: «Cada dirección usa su estilo de Transmisión → Estilos de la
  transmisión…» y un botón **🎨 Editar los estilos…** que abre la ventana 4.
- La guía corta para OBS: «En OBS: Fuentes → ➕ → Navegador, pegá la
  dirección, ancho 1920 y alto 1080, y poné esa fuente arriba de la cámara».

En `RemoteControlWindow` la caja se reemplaza por un renglón: «La salida para
OBS o vMix ahora está en el menú **Transmisión**.» (sin botones).

## 3. El estilo de cada capa (`BroadcastStyle`)

`src/EcclesiaCast.Core/Remote/BroadcastStyle.cs`, un `record` inmutable con
`Parse(string?)`/`ToSetting()` en JSON (como `OutputTransitions`), y
`BroadcastStyles` con uno por canal (Biblia, Canciones, Anuncios), guardados
en `ISettingsStore` con la clave `broadcast.style.<biblia|canciones|anuncios>`.
Un `BroadcastStyleSettings` estático en `App/Services` (como
`TransitionSettings`) los tiene cargados.

Campos (con sus valores por defecto):

| Campo | Qué es | Por defecto |
|---|---|---|
| `FollowProjector` | «Igual que en el proyector»: se comporta como hasta hoy (usa el tema de la diapositiva) | `false` |
| `Position` | `Top` / `Center` / `Bottom` (Superior, Centro, Inferior) | Biblia `Bottom`, Canciones `Bottom`, Anuncios `Center` |
| `Plate` | `None`, `Tab` (Pestaña), `Glass` (Vidrio), `Solid` (Sólida), `Gradient` (Degradé), `Halo`, `Fillet` (Filete), `Line` (Línea) | `Tab` |
| `AccentColor` | color de la pestaña, el filete y la línea | `#2DD4BF` |
| `PlateColor` | color de la placa | `#14161C` |
| `PlateOpacity` | 0–100 | 85 |
| `TextColor` | | `#FFFFFF` |
| `FontFamily` | | `Segoe UI` |
| `FontSize` | en puntos sobre 1080 de alto, como `RemoteOutput.FontSize` | 54 |
| `Bold` | | `true` |
| `Uppercase` | | Canciones `true`, el resto `false` |
| `ShowLabel` | la etiqueta de arriba de la placa: en canciones la parte («CORO», «ESTROFA 1»), en Biblia la cita («JUAN 3:16 · RVR1909») | `true` |
| `WidthPercent` | ancho máximo de la placa, 30–100 % | 70 |
| `MarginPercent` | margen de seguridad desde el borde, 0–15 % | 6 |
| `Background` | `None` (transparente) / `Color` / `File` (imagen o video fijo detrás) | `None` |
| `BackgroundColor` | para `Color` | `#000000` |
| `BackgroundFile` | ruta de la imagen o video, para `File` | vacío |

`Clamp()` mantiene los números dentro de sus límites; `Parse` de algo roto
devuelve el de fábrica de ese canal (nunca tira).

### Qué cambia en lo que se manda

- `RemoteOutput` suma `Style` (el `BroadcastStyle` del canal, o `null` cuando
  `FollowProjector`) y `Label` (la etiqueta: en canciones el `Label` de la
  diapositiva en vivo en mayúsculas, en Biblia el `Caption`).
- La página `/salida` (todo) usa el estilo del canal de lo que está al aire
  (`OnAir`: Bible → Biblia, Song → Canciones, el resto → Anuncios).
- Con estilo propio, el `Caption` de la Biblia **no** se dibuja aparte cuando
  `ShowLabel` lo pone en la etiqueta.
- `Background = File`: endpoint nuevo `/api/fondo?pin=…&canal=…` en
  `RemoteControlServer` que sirve **sólo** el archivo guardado en el estilo de
  ese canal (la ruta nunca viene del pedido), con los mismos tipos que
  `StreamableExtensions` y con soporte de rangos como `/api/media`.
  Va en `IRemoteHost` como `GetBackgroundFile(OutputChannel)`.
- El fondo se ve **siempre** que la página está prendida (es fijo), salvo con
  Black, Clear y Logo, que vacían la capa entera como hoy. Un video de fondo
  va en bucle y sin sonido.

### Cómo se dibuja (en `OutputPage.cs`)

Con `s.style` presente se usa un camino nuevo; sin él, el de hoy sin cambios.
El texto va dentro de `#plate` (un `div`) que se pone arriba, al centro o abajo
con el margen de seguridad, centrado horizontalmente, con `max-width` del
ancho elegido. La etiqueta (`#label`) va pegada arriba de la placa. Las placas,
iguales a las imágenes de Reuna:

- **Pestaña**: placa redondeada del `PlateColor` con su opacidad, borde fino
  más claro, una línea gruesa del acento abajo, y la etiqueta como pestañita
  del acento con letra oscura, centrada arriba.
- **Vidrio**: placa semitransparente con `backdrop-filter: blur()`, borde
  claro fino, sin acento.
- **Sólida**: sin placa; letra con una sombra fuerte y pareja para leerse
  sobre cualquier cámara.
- **Degradé**: sin caja; un degradé del `PlateColor` de transparente a casi
  opaco que ocupa el tercio de la pantalla del lado de la posición (abajo,
  arriba, o el centro desvanecido hacia los dos lados).
- **Halo**: sin caja; un resplandor oscuro difuso detrás del texto
  (`text-shadow` grande y suave).
- **Filete**: sin caja; sombra suave y una línea fina del acento debajo, que
  se desvanece hacia los costados.
- **Línea**: placa como Sólida pero con una línea vertical del acento a la
  izquierda del texto (texto alineado a la izquierda).
- **Ninguna**: sólo el texto con el contorno fino de siempre.

Al cambiar de texto, la placa hace un fundido corto (200 ms) en vez de saltar.
El tamaño de letra se achica solo si el texto no entra (bajar de a 5 % hasta
que la placa quepa en la mitad de alto de la pantalla), igual que hace el
proyector.

## 4. Ventana «Estilos de la transmisión»

`Views/BroadcastStyleWindow.xaml`, oscura como `TransitionsWindow`. Arriba,
tres pestañas: **Biblia · Canciones · Anuncios**. Cada una muestra:

- Izquierda (controles):
  - Casilla «Igual que en el proyector» (apaga todo lo demás).
  - **1 · Posición**: tres botones Superior / Centro / Inferior.
  - **2 · Placa**: una grilla de 8 botones (Ninguna, Pestaña, Vidrio, Sólida,
    Degradé, Halo, Filete, Línea), con el elegido resaltado.
  - **3 · Letra**: tipografía (la misma lista de fuentes que el editor de
    temas), tamaño (deslizador), Negrita, MAYÚSCULAS, color del texto.
  - **Colores**: acento y placa (con `ColorPickerWindow`) y opacidad de la placa.
  - Ancho de la placa y margen de seguridad (deslizadores).
  - Casilla «Mostrar la etiqueta (CORO / la cita)».
  - **Fondo fijo**: Transparente / Color / Imagen o video… (abre un selector
    de archivos).
- Derecha: **vista previa** con un `WebView2` que carga `OutputPage.Html` y
  le llama `paint(...)` con un texto de ejemplo del canal (canciones: «Santo,
  santo, santo, Señor omnipotente» con etiqueta CORO; Biblia: Juan 3:16 de la
  RV1909; anuncios: «Reunión de jóvenes · Sábado 19 h») sobre una «cámara»
  falsa: un degradé violeta y azul oscuro con luces, hecho en CSS, para ver
  cómo se lee. La página no debe sondear el servidor cuando está en la vista
  previa (si existe `window.chrome.webview`, no llama a `poll()`). Si WebView2
  no está, la vista previa dice «No se pudo mostrar la vista previa» y lo
  demás anda igual.
- Abajo: **Volver a los de fábrica** (sólo la pestaña abierta), **Cancelar**,
  **Guardar**. Guardar aplica a OBS en el próximo sondeo (≤ 0,3 s).

## 5. Pruebas

En `tests/EcclesiaCast.Core.Tests`: `BroadcastStyle` (ida y vuelta por JSON,
`Parse` de basura y de vacío devuelve el de fábrica del canal, `Clamp`), y
que cada canal tiene sus valores por defecto. Si hay pruebas del servidor
(`RemoteControlServer`), sumar que `/api/fondo` sólo sirve el archivo del
estilo y responde 404 sin fondo.

## 6. Al terminar

- Dibujar a imagen la vista previa de las 8 placas en las 3 posiciones
  (puede ser abriendo `OutputPage.Html` en un navegador sin el programa) y la
  ventana nueva, para que Alexis las vea.
- `CHANGELOG.md` en `## [Sin publicar]`, y en `docs/pendientes.md` §1 los
  pasos para probarlo en OBS.
