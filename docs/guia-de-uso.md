# Guía de uso de EcclesiaCast

Guía práctica para operar un servicio. Si es tu primera vez, seguí las
secciones en orden.

## 1. Elegir la pantalla de salida

Arriba a la derecha, en **Salida**, elegí el monitor del proyector (EcclesiaCast
prefiere solo el que no es el principal). El botón **🔴 Poner en vivo** enciende
y apaga la proyección; **Esc** también la apaga.

La pantalla elegida se recuerda para la próxima vez.

## 2. Cargar canciones

En la pestaña **Canciones**:

- **➕** crea una canción. Pegá la letra: **cada párrafo** (bloque separado por
  una línea en blanco) es una diapositiva. Opcionalmente, una línea `[Coro]` o
  `[Verso 1]` no se proyecta, solo le pone nombre a los párrafos que siguen.
- **📥** importa canciones desde archivos `.txt` o desde archivos **`.pro` de
  ProPresenter 7**. Para migrar, copiá la carpeta
  `Documentos\ProPresenter\Libraries` y seleccioná todos los `.pro`.
- El buscador filtra por título, artista o texto de la letra.

**Proyectar:** clic en una diapositiva, o doble clic en la canción para empezar
desde la primera. Las flechas **←→** navegan en vivo.

## 3. Biblia

En la pestaña **Biblia**:

- **📥** importa una Biblia en **JSON** o **Zefania XML**.
- Marcá la casilla de una versión para activarla. **Podés marcar dos**: la
  segunda se proyecta debajo de la primera, y cada texto lleva su abreviatura
  entre corchetes (`[RVR]`, `[NTV]`). Un **clic en el nombre** cambia a esa
  versión sola.
- Elegí **libro → capítulo**, o escribí una referencia: `Juan 3:16`,
  `jn 3:16-18`, `sal 23`, `1 co 13`. Con una referencia, **Enter** proyecta el
  versículo indicado.
- Escribir una palabra suelta busca en el texto de la versión activa.
- Cada capítulo empieza y termina con tarjetas **◀ Anterior / ▶ Siguiente**:
  con las flechas recorrés la Biblia entera, cruzando de libro.

## 4. Fondos (imágenes y videos)

En la barra **MEDIOS**, abajo:

- Las **pestañas** (Fondos, Anuncios, y las que agregues con **＋**) organizan
  los medios.
- **📥 Agregar** importa imágenes y videos a la pestaña activa.
- **Clic** en una miniatura la aplica al instante. El fondo **persiste entre
  diapositivas**: cambiar de verso no reinicia el video.
- **Clic derecho → Propiedades** abre el Inspector:
  - **Comportamiento**: *Fondo* (detrás del texto) o *Primer plano* (pantalla
    completa, tapa el texto — para anuncios o videos institucionales).
  - **Escala**: *Rellenar* (recorta), *Ajustar* (con barras) o *Estirar*.
  - **Al terminar el video**: repetir en bucle o detenerse.
  - **Audio**: silenciar o volumen.
- **Sin fondo** lo quita.

> Para que el texto se vea *sobre* el fondo, el tema tiene que tener el fondo
> transparente (los temas nuevos ya vienen así).

## 5. Temas: tipografía, colores y márgenes

Botón **🎨 Temas** en la barra superior:

- Editás tipografía, tamaño (con **auto-ajuste**: si un versículo no entra, se
  achica solo), negrita/cursiva/mayúsculas, color, alineación, márgenes,
  interlineado y sombra.
- La **caja de texto** se arrastra sobre el lienzo, se redimensiona desde
  cualquiera de sus 8 manijas y se ajusta fino con las flechas.
- La **leyenda** (título y artista en canciones, referencia en la Biblia) tiene
  su propia tipografía, color, tamaño y posición; podés ocultarla.
- **Usar en canciones / Usar en Biblia** fija el tema por defecto de cada uno.

Cada **canción puede tener su propio tema**, y cada **diapositiva** su propio
diseño (clic derecho sobre la canción → *Editar canción (diseño)*).

## 6. Playlist del servicio

En el panel **PLAYLIST**:

- **➕** crea la playlist del domingo; **⧉** duplica la del domingo pasado.
- Agregá contenido: clic derecho en una canción → *Agregar a la playlist*; en
  la Biblia, cargá un pasaje y tocá **▶➕**; clic derecho en un medio →
  *Agregar a la playlist*.
- **Clic** carga el ítem, **doble clic** lo proyecta. Clic derecho para
  **subir/bajar/quitar**.
- Con la playlist en uso, las flechas **←→** siguen de largo: al terminar una
  canción, la flecha derecha pasa al siguiente ítem del culto.

## 7. Durante el servicio

| Atajo | Qué hace |
|---|---|
| **←→** | Diapositiva anterior / siguiente (y salta al ítem contiguo de la playlist) |
| **F1** | *Clear* — oculta el texto, deja el fondo |
| **F2** | *Black* — pantalla negra |
| **F3** | *Logo* |
| **Esc** | Apaga la salida |
| **Ctrl+Enter** | Proyecta el texto rápido |

- **Texto rápido**: escribí un anuncio y proyectalo al momento.
- **Aviso al pie**: un mensaje que aparece sobre todo lo demás (ideal para
  "el auto ABC 123 está mal estacionado"). Se quita con **Quitar**.
- **Resaltar en vivo**: escribí una palabra y se pinta como con marcador sobre
  el texto proyectado.

## Dónde quedan tus datos

Todo (canciones, Biblias, temas, medios y playlists) vive en un solo archivo:

```
%APPDATA%\EcclesiaCast\ecclesiacast.db
```

**Copiar ese archivo es tu backup.** Los logs, por si algo falla, están en
`%APPDATA%\EcclesiaCast\logs\`.
