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
- **🅿** trae **todas** las canciones de ProPresenter de una vez: busca sola
  dónde está instalado (también en la carpeta de OneDrive), te muestra sus
  bibliotecas con cuántas canciones tiene cada una y las importa. Las que ya
  estén en la biblioteca se saltean. Si tus canciones están en otro lado (un
  disco externo, una copia de otra máquina), usá **Elegir carpeta…**.
- **📥** importa archivos sueltos `.txt` o `.pro`.
- El buscador filtra por título, artista o texto de la letra.

Al escribir la letra, las palabras dudosas se **subrayan en rojo**. El clic
derecho ofrece las correcciones y también **agregar la palabra al diccionario**
de la iglesia, que ya viene con vocabulario bíblico. (Necesita el corrector de
español de Windows, que viene instalado con el idioma.)

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
  - **Desenfoque**: difumina el fondo para que la letra se lea encima.
  - **Encuadre**: **zoom** para acercar o alejar (100 % es como está), y
    **correr** la imagen a los lados o arriba y abajo. Con **tamaño fijo en
    pantalla** le das una medida exacta (1280×720, cuadrado, vertical…), para
    cuando la pantalla de la iglesia no tiene la forma de lo que tira el
    proyector.
  - **Color**: **brillo** para oscurecer (negativo) o aclarar (positivo) el
    fondo, y un **tinte** de color con su intensidad. Oscurecer es, junto con
    el desenfoque, lo que hace que la letra se lea sobre un fondo cargado.
  - **Recorte del video**: desde qué segundo arranca y hasta cuál llega. Sirve
    para saltear la cortina o el logo del principio; el bucle respeta el
    recorte y vuelve al segundo que marcaste, no al cero.
  - **Los bordes se rellenan con**: un color, o **otra imagen o video** de la
    biblioteca detrás. El truco de siempre es poner una copia desenfocada del
    mismo video como relleno.
- **Sin fondo (F4)**, arriba, lo quita **sin sacar la letra**.
- Con un video en pantalla aparece la **barra de reproducción**: volver al
  principio, ±10 segundos, reproducir/pausar y una barra para moverte. Al lado
  está el **desenfoque**, que se ajusta en vivo y queda guardado en ese medio.
- **▶▶ continua** hace que la pestaña funcione como playlist: al terminar un
  video arranca el siguiente, y al final vuelve al primero. Ideal para dejar un
  bucle de avisos antes de la reunión.
- **🎲** pone un fondo al azar de la pestaña que estés viendo, en el momento.
- **🎲 al azar por canción** hace que **cada canción que proyectás se lleve un
  fondo distinto** de esa pestaña. Marcalo en *Fondos* y esa pasa a ser la
  pestaña de la que sortea (por eso la casilla sólo se ve tildada ahí).

> El sorteo **no repite** ningún fondo hasta haber usado todos, y nunca te da
> el mismo dos canciones seguidas. Si en medio de una canción aplicás un fondo
> a mano, se respeta hasta la canción siguiente. Sólo entran los medios
> marcados como *Fondo*: los de *Primer plano* taparían la letra.
- **Clic derecho → Descargar para uso local** guarda un video de YouTube en la
  computadora, para no depender de internet durante el servicio. Queda en la
  pestaña **Descargados**. Necesita [yt-dlp](https://github.com/yt-dlp/yt-dlp)
  instalado aparte (`winget install yt-dlp`); con ffmpeg además se baja hasta
  1080p. Descargá solo videos propios o con licencia que lo permita.

> Al apagar la salida, el video se **pausa** y retoma donde estaba cuando la
> volvés a prender.

> Para que el texto se vea *sobre* el fondo, el tema tiene que tener el fondo
> transparente (los temas nuevos ya vienen así).

## 5. Temas: tipografía, colores y márgenes

Botón **🎨 Temas** en la barra superior:

- Editás tipografía, tamaño (con **auto-ajuste**: si un versículo no entra, se
  achica solo), negrita/cursiva, color, alineación, márgenes e interlineado.
- **Cómo entra la diapositiva**: sin transición (corte), fundido, desde la
  derecha, desde abajo o con un zoom suave, con su duración en milisegundos.
  Como es parte del tema, podés tener las canciones con fundido y la Biblia
  con corte seco.
- **Uso de mayúsculas**: como está escrito, TODAS MAYÚSCULAS, Cada Palabra,
  como oración o todas minúsculas — cambia lo proyectado sin tocar el texto
  guardado.
- **Sombra** con intensidad y difusión, y **contorno** de la letra con grosor y
  color: es lo que salva la letra blanca sobre un fondo blanco.
- Los tamaños se escriben como número y se ajustan de a un punto con **−** y
  **+** (con Shift, de a diez).
- La **caja de texto** se arrastra sobre el lienzo, se redimensiona desde
  cualquiera de sus 8 manijas y se ajusta fino con las flechas.
- La **leyenda** (título y artista en canciones, referencia en la Biblia) tiene
  su propia tipografía, color, tamaño y posición; podés ocultarla.
- **Usar en canciones / Usar en Biblia** fija el tema por defecto de cada uno.

En el diseñador, arriba del panel de formato hay un selector: **Letra de la
canción** o **Cuadro 1, 2, 3…**. Con **➕ Cuadro de texto** agregás otro cuadro
a esa diapositiva — el nombre de la serie, la cita del pasaje, una traducción —
y lo movés, lo estirás y lo escribís (doble clic) igual que la caja de la letra.
Cada cuadro lleva su tipografía, tamaño, color y alineación; la papelera lo
quita.

Cada **canción puede tener su propio tema**, y cada **diapositiva** su propio
diseño (clic derecho sobre la canción → *Editar canción (diseño)*).

## 6. Playlist del servicio

En el panel **PLAYLIST**:

- **➕** crea la playlist del domingo; **⧉** duplica la del domingo pasado.
- **✨** crea una **lista inteligente**: en vez de armarla vos, se llena sola
  con las canciones que **agregaste hace poco**, las que **hace rato que no
  cantás**, las de **un artista** o las que **dicen una palabra**. Cambia sola
  cuando cambia la biblioteca, así que no se le agregan ni quitan cosas a
  mano; con **✏️** cambiás su regla. Para saber qué hace rato no cantás, el
  programa anota la fecha cada vez que proyectás una canción (las que ya
  tenías arrancan sin fecha, así que al principio salen todas).
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
| **F4** | Quita el fondo, dejando la letra |
| **Esc** | Apaga la salida |
| **Ctrl+Enter** | Proyecta el texto rápido |

- **Logos**: la flecha **▾** al lado de *Logo (F3)* elige cuál se muestra —
  podés tener uno para la reunión general, otro para jóvenes, otro para
  mujeres. Cada logo puede ser una **imagen**, un **video en bucle** o un
  **texto libre** con su tipografía y colores; se arman en *Administrar
  logos…*. El botón **🖼▦** de al lado pone el logo **de fondo**, detrás de la
  letra, con el desenfoque que le hayas puesto.

- **Texto rápido**: escribí un anuncio y proyectalo al momento.
- **Aviso al pie**: un mensaje que aparece sobre todo lo demás (ideal para
  "el auto ABC 123 está mal estacionado"). Se quita con **Quitar**.
- **Resaltar en vivo**: escribí una palabra y se pinta como con marcador sobre
  el texto proyectado.

## 8. Pantalla de escenario

Botón **🎭 Escenario** en la barra superior. Es un tercer monitor, de cara a la
plataforma, para que los músicos y quien predica vean:

- la **letra que está proyectada**, en grande y blanco sobre negro;
- la **diapositiva que sigue**, para llegar preparados al cambio;
- la **hora** y un **cronómetro** de cuánto lleva la reunión;
- el **aviso al pie**, si hay uno puesto.

Con **Clear** o **Black** la congregación deja de ver la letra, pero el
escenario **la sigue mostrando** y avisa cuál de los dos estados está activo —
que es justamente para lo que sirve.

Abajo de todo aparecen las **notas**: las escribís en la pestaña **Notas** de
la columna derecha y tocás *Mostrar*. Se ven sólo acá — el guion de quien
predica, «cantamos el coro dos veces» — y nunca llegan a la congregación. Si
son muchas, se achican para entrar enteras.

La flecha **▾** de al lado elige en qué pantalla va, qué se muestra (hora,
cronómetro, siguiente), el tamaño de la letra y **⟲ pone el cronómetro en
cero**. No te deja elegir la misma pantalla que la salida.

## 9. Controlar desde el celular

Botón **📱** en la barra superior. Abre una ventana con un **código QR**, la
dirección y un **PIN de cuatro dígitos**.

- El celular tiene que estar en la **misma red wifi** que la computadora.
- Escaneás el QR (o escribís la dirección, tipo `http://192.168.1.40:8080`),
  ponés el PIN una vez y listo — el celular lo recuerda.
- Desde ahí manejás: **◀ ▶** para las diapositivas, **Clear / Black / Logo**,
  **Sin fondo**, prender y apagar la **salida**, tocar cualquier **diapositiva**
  y cualquier ítem de la **playlist**. Arriba se ve la letra en vivo y la que
  sigue.
- La primera vez, **Windows pregunta si permitís el acceso a la red**: aceptá
  para *redes privadas*.
- Queda encendido para el próximo servicio. Se apaga desde la misma ventana.

### Para la transmisión (OBS o vMix)

En la misma ventana del 📱 está la dirección **para la transmisión**. Copiala y
pegala en un **Browser Source** de OBS o en un **Web Input** de vMix: vas a ver
la letra que está proyectada, sola, con el **fondo transparente**, lista para
poner encima de la cámara. No hace falta placa capturadora ni otra computadora.

- Se actualiza sola, tres veces por segundo.
- Respeta el tema: tipografía, color, contorno y alineación.
- Con **Clear**, **Black** o **Logo** la letra desaparece de la transmisión,
  igual que en el proyector.
- El **aviso al pie** y la **cuenta regresiva** también salen.
- Si tu programa no maneja transparencia, agregale `&fondo=negro` al final de
  la dirección.

> El PIN existe porque cualquiera conectado a la wifi podría, si no, manejar la
> proyección. Cambiá de PIN apagando y volviendo a prender el control remoto.

## 10. Cuenta regresiva

Botón **⏱** en la barra de arriba, para el rato antes de empezar.

- Elegís **en cuántos minutos** empieza (con atajos de 5, 10, 15 y 30) o **a
  qué hora del reloj** (10:30, 19:00). Si la hora ya pasó, se entiende que es
  la de mañana.
- El **texto de arriba** («Empezamos en») y el de **cuando llega a cero**
  («¡Bienvenidos!») los escribís vos, y quedan guardados para la próxima.
- Se proyecta **encima del fondo** que tengas puesto, y también aparece en la
  **pantalla de escenario**, para que los músicos sepan cuánto falta.
- Al llegar a cero se queda con el mensaje final hasta que la detengas: el
  botón ⏱ queda encendido mientras corre, y adentro está **Detener la que
  corre**.

## 11. Copia de seguridad

Botón **💾** en la barra de arriba:

- **Guardar una copia de la biblioteca…** escribe un archivo `.ecbackup` con
  **todo**: canciones, Biblias, temas, medios y playlists. Se puede hacer con
  el programa abierto, incluso proyectando. Guardalo fuera de la computadora
  (un pendrive, la nube): si se rompe el disco, la copia se va con él.
- **Restaurar desde una copia…** te muestra qué trae la copia (cuántas
  canciones, Biblias, medios y playlists) antes de tocar nada. Si aceptás,
  EcclesiaCast se cierra y se vuelve a abrir solo, y arranca con esa
  biblioteca. La que tenías queda guardada al lado, como
  `ecclesiacast.db.before-restore`, por si te arrepentís.

> Restaurar **reemplaza todo** lo que tengas ahora, no lo mezcla.

## Dónde quedan tus datos

Todo (canciones, Biblias, temas, medios y playlists) vive en un solo archivo:

```
%APPDATA%\EcclesiaCast\ecclesiacast.db
```

**Copiar ese archivo es tu backup.** Los logs, por si algo falla, están en
`%APPDATA%\EcclesiaCast\logs\`.
