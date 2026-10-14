# Guía de uso de EcclesiaCast

Guía práctica para operar un servicio. Si es tu primera vez, seguí las
secciones en orden.

## 1. Elegir la pantalla de salida

Arriba a la derecha, en **Salida**, elegí el monitor del proyector (EcclesiaCast
prefiere solo el que no es el principal). El botón **🔴 Poner en vivo** enciende
y apaga la proyección; **Esc** también la apaga.

La pantalla elegida se recuerda para la próxima vez.

Al abrir el programa aparece una pantalla de carga con el logo y la versión,
que se va sola cuando carga la ventana principal. La **primera vez** que se
abre, además, se muestra **Acerca de EcclesiaCast**: tocá **Aceptar** y sigue.
No vuelve a aparecer, pero lo podés abrir cuando quieras en **Ayuda → Acerca de
EcclesiaCast**.

## 2. Cargar canciones

En la pestaña **Canciones**:

- **➕** es el único botón para agregar canciones y ofrece cuatro caminos:
  **Crear desde cero**, **Buscar en internet…**, **Traer de ProPresenter…** e
  **Importar archivo (.txt, .pro)…**. Crear desde cero abre una canción en
  blanco: pegá la letra, y **cada párrafo** (bloque separado por
  una línea en blanco) es una diapositiva. Opcionalmente, una línea `[Coro]` o
  `[Verso 1]` no se proyecta, solo le pone nombre a los párrafos que siguen.
- **➕ → Traer de ProPresenter…** trae **todas** las canciones de ProPresenter de una vez: busca sola
  dónde está instalado (también en la carpeta de OneDrive), te muestra sus
  bibliotecas con cuántas canciones tiene cada una y las importa. Las que ya
  estén en la biblioteca se saltean. Si tus canciones están en otro lado (un
  disco externo, una copia de otra máquina), usá **Elegir carpeta…**.
- **➕ → Buscar en internet…** (también en **Archivo**) trae una canción
  nueva sin copiar y pegar: escribí el **nombre**, el **artista** o **una frase
  de la letra** y apretá Enter. Busca en musica.com y, si no está ahí, en
  lrclib.net. Al elegir un resultado ves la letra ya separada en diapositivas
  (con [Coro], [Verso 1]… cuando la página los marca); corregila ahí mismo si
  hace falta y apretá **Importar a la biblioteca**. Podés importar varias sin
  cerrar la ventana. Necesita internet, claro.
- **➕ → Importar archivo…** trae archivos sueltos `.txt` o `.pro`.
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
- Las versiones van en una fila de botones con su abreviatura. Un **clic**
  usa sólo esa versión; **Ctrl+clic suma o saca una segunda**, que se proyecta
  debajo de la primera (cada texto lleva su abreviatura entre corchetes,
  `[RVR]`, `[NTV]`). Las activas se ven encendidas: la primera llena, la
  segunda con borde. El botón **▸** abre la lista con los nombres completos.
  Con **clic derecho** sobre una versión: **editar** su nombre y abreviatura,
  **exportar** a un archivo que el programa vuelve a importar (con 📥), o
  **eliminar**.
- **➕ Playlist** abre una cajita con la cita que agregaría (la del buscador, el
  versículo en pantalla o el capítulo). Ahí podés escribir otra
  (`juan 8 del 5 al 10`), leer el pasaje y tocar **Agregar**.
- Elegí **libro → capítulo**, o escribí una referencia: `Juan 3:16`,
  `jn 3:16-18`, `sal 23`, `1 co 13`. Con una referencia, **Enter** proyecta el
  versículo indicado.
- Escribir una palabra suelta busca en el texto de la versión activa.
- Cada capítulo empieza y termina con tarjetas **◀ Anterior / ▶ Siguiente**:
  con las flechas recorrés la Biblia entera, cruzando de libro.

## 4. Fondos (imágenes y videos)

En la barra **MEDIOS**, abajo:

- Las **pestañas** (Fondos, Anuncios, y las que agregues con **＋**) van en
  una lista a la izquierda y organizan los medios. Con **clic derecho** sobre
  una: **Cambiar nombre…** (sirven los emojis), **Configurar…** (todo lo de la
  pestaña va como *fondo*, como *primer plano* o *como diga cada medio*; para
  primer plano, «Al terminar, mostrar el logo») y **Eliminar…** (los medios
  dejan de verse en el programa; los archivos no se borran).
- **📥 Agregar** importa imágenes y videos a la pestaña activa, y también
  **PDF** y **presentaciones de PowerPoint**: cada página entra como un medio
  de pantalla completa, numerado y en orden. El PDF lo dibuja Windows sin
  instalar nada; el `.pptx` lo convierte el PowerPoint de la máquina (si no
  tenés PowerPoint, exportá la presentación a PDF y entra igual).
- También podés **arrastrar archivos desde el Explorador** a la barra: se
  agregan a la pestaña elegida (el borde se marca mientras arrastrás).
- **Clic** en una miniatura la aplica al instante. El fondo **persiste entre
  diapositivas**: cambiar de verso no reinicia el video.
- **Clic derecho → Propiedades** abre el Inspector:
  - **Comportamiento**: *Fondo* (detrás del texto) o *Primer plano* (pantalla
    completa, tapa el texto — para anuncios o videos institucionales).
  - **Escala**: *Rellenar* (recorta), *Ajustar* (con barras) o *Estirar*.
  - **Qué sigue después**: en un video, repetir en bucle, nada, el logo u
    **otro medio de la biblioteca**. En una imagen en primer plano, cuántos
    segundos se ve y qué pasa pasados esos segundos (nada: sale de la
    pantalla; el logo; u otro medio).
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

> **Videos pesados (4K o de 60 cuadros por segundo):** si tenés ffmpeg
> instalado (`winget install ffmpeg`), EcclesiaCast les hace solo una **copia
> liviana** en 1080p y a 30 cuadros, que se ve igual en el proyector y le
> cuesta varias veces menos a la computadora (un 4K a 60 bajó de 107 % a 20 %
> de un núcleo). Tu biblioteca sigue apuntando siempre al archivo original:
> la copia se usa en su lugar sólo al proyectar, y nunca si le pusiste zoom
> para acercar. Las copias se hacen de a una, con la menor prioridad, y
> **nunca empiezan con la salida prendida**; la barra de abajo avisa cuando
> una está lista. Los videos que ya son livianos (como los loops de 1080p a
> 30) se reproducen directo del original.
>
> **Al agregar un video pesado** (4K, o de 50 cuadros o más), el programa
> pregunta «¿Hago una copia más liviana?». Con **Sí** la copia se hace en ese
> momento, con una barra de avance (y **Cancelar** si no querés esperar); con
> **No** se proyecta el original y no se vuelve a preguntar. Para hacerla más
> tarde: clic derecho sobre el video → **Hacer una copia liviana…**. En el panel
> LIVE, estos videos se muestran con su imagen quieta en vez de moverse.

> Para que el texto se vea *sobre* el fondo, el tema tiene que tener el fondo
> transparente (los temas nuevos ya vienen así).

### Proyectar una página web

El botón **🌐 Web** de la barra de medios abre una página web **en el lugar de
las diapositivas** (el cuadro central). La usás ahí con el mouse y el teclado
como en cualquier navegador, y la mandás a la pantalla de dos formas. Hace
falta el componente WebView2 de Microsoft (el mismo de YouTube).

- **La dirección.** Escribí una dirección (`google.com`) y apretá Enter, o
  escribí lo que quieras buscar (`juan 3 16 comentario`) y lo busca en Google.
  Con ◀ ▶ ⟳ volvés, avanzás y recargás. Las ventanas nuevas que abre una página
  se abren en este mismo panel. Si iniciás sesión (Google, Canva), queda
  guardada y vale también para YouTube.
- **Lo que ves es lo que se proyecta.** La página se arma siempre como en una
  pantalla de 1920×1080 y se achica para entrar en el panel (con barras a los
  costados si sobra lugar). **A−** y **A+** achican y agrandan la letra de la
  página, para que se lea en la pantalla grande.
- **📷 Proyectar foto.** Saca una foto de la página tal como está y la
  proyecta, **en primer plano** (tapa la letra, como un anuncio) y entera. Si
  seguís navegando, la pantalla **no cambia** hasta la próxima foto. Las fotos
  se guardan en la carpeta `capturas` y las de más de 7 días se borran solas
  al abrir el programa, salvo las que guardaste en medios.
- **🔴 En vivo.** La pantalla muestra la página **en movimiento**: lo que te
  desplazás, un video o una animación. Con **Mostrar el puntero** (prendido de
  entrada; se recuerda) se dibuja en la pantalla una flecha donde tenés el
  mouse sobre la página, para que la congregación vea qué señalás. Se apaga con
  el mismo botón, o solo cuando proyectás otro fondo o una diapositiva. Si
  tocás **✕ Cerrar** y volvés a las diapositivas, la página **sigue en vivo**.
  Con el **modo de PC de bajo rendimiento**, tus paneles LIVE y PREVIEW
  muestran una foto que se renueva cada 2 segundos (la pantalla siempre va en
  movimiento).
- **Guardar en medios.** Si sacaste una foto de la página que tenés abierta, la
  guarda como imagen en la pestaña elegida. Si no, guarda **la página**: una
  tarjeta 🌐 con su título y una miniatura. Un **clic** en una tarjeta 🌐 abre
  la página en el panel, sin proyectarla (se proyecta con los botones). Se
  puede agregar a la playlist: al elegirla se abre el panel, y el **doble
  clic** la pone en vivo. Su menú de clic derecho tiene sólo lo que sirve:
  cambiar nombre, mover de pestaña, agregar a la playlist y quitar.
- **El teclado.** Mientras escribís en la página o en la caja de dirección, los
  atajos del programa (Esc, las flechas que cambian de diapositiva, Ctrl+K)
  no se disparan; vuelven cuando hacés clic afuera de la página. **F1 a F4
  siguen andando siempre**, para poner la pantalla en negro o limpiarla en un
  apuro.
- La página con **protección de copia** (Netflix, Spotify) se ve negra en la
  pantalla; no es un problema del programa.

### Proyectar otra ventana

El botón **🖥 Ventana** de la barra de medios pone en la pantalla **otra
ventana de esta computadora**, en vivo y tal como se ve: un PowerPoint en
presentación, un reproductor de video, un navegador, una planilla. Sirve para lo
que no se puede pasar por el programa. Usa la captura de ventanas del propio
Windows (hace falta Windows 10 versión 1903 o más nueva) y no necesita
internet.

- **Elegir la ventana.** El botón abre la lista de ventanas abiertas, con el
  ícono, el título y el programa de cada una (la de EcclesiaCast no está: se
  vería a sí misma sin fin). Elegí una y tocá **Proyectar ▶** (o doble clic).
  **↻ Actualizar** vuelve a buscarlas. **Elegir con la ventana de Windows…**
  abre el selector de Windows, que sirve también para proyectar **una pantalla
  entera**.
- **Mostrar el puntero.** Tildada, la flecha del mouse también se ve en la
  pantalla. Se recuerda; viene apagada.
- **💾 Guardar en medios.** En vez de proyectarla ahora, deja en la pestaña
  elegida una tarjeta **🖥 ventana** con el nombre de la ventana (el programa y
  su título; no se guarda ninguna imagen). Un **clic** en la tarjeta busca esa
  ventana entre las abiertas y la proyecta; si el programa no está abierto, lo
  avisa. Si el título cambió un poco (un documento al que se le agregó
  «[modificado]»), la encuentra igual; si el programa tiene varias ventanas y
  ninguna se parece, prefiere avisar antes que mostrar la equivocada. Se puede
  **agregar a la playlist** como cualquier medio: el doble clic la proyecta.
  Su menú de clic derecho tiene sólo lo que sirve (cambiar nombre, mover de
  pestaña, agregar a la playlist, quitar). No sirve para lo elegido con el
  selector de Windows.
- **Se ve entera.** La ventana se achica para entrar completa y va **en primer
  plano** (tapa la letra, como un anuncio). Se apaga con **F4** (sin fondo) o
  cuando proyectás otro fondo o una diapositiva.
- **Tené en cuenta.**
  - Windows dibuja un **borde amarillo** alrededor de la ventana elegida: se ve
    sólo en la pantalla del operador, no en la de la congregación.
  - Si **minimizás** la ventana, en la pantalla queda el **último cuadro**;
    restaurala para que siga. Si la **cerrás**, se deja de proyectar sola.
  - Si el programa de esa ventana está abierto **como administrador**, Windows
    no deja capturarlo, salvo que EcclesiaCast también lo esté.
  - La ventana con **protección de copia** (Netflix, algunos reproductores) se
    ve negra.
  - Con el **modo de PC de bajo rendimiento**, la captura baja a 12 cuadros por
    segundo y tus paneles LIVE y PREVIEW muestran una foto que se renueva cada
    2 segundos.

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
- Agregá contenido: clic derecho en una canción → *Agregar a la playlist*; en
  la Biblia, cargá un pasaje y tocá **▶➕**; clic derecho en un medio →
  *Agregar a la playlist*.
- **Clic** carga el ítem, **doble clic** lo proyecta. Clic derecho para
  **subir/bajar/quitar**.
- Con la playlist en uso, las flechas **←→** siguen de largo: al terminar una
  canción, la flecha derecha pasa al siguiente ítem del culto.

## 7. Buscar en todo de una vez

Botón **🔍** de la barra de arriba, o **Ctrl+K**. Una sola caja que busca a la
vez en las **canciones**, en **todas las Biblias** que tengas instaladas (cada versículo
dice de qué versión es, y se abre en esa) y en los **medios**. Una frase
escrita de corrido se encuentra aunque en la letra esté partida en dos renglones.
Las flechas **↑↓** recorren los resultados sin sacar el cursor de la caja, y
**Enter** carga el que elijas: la canción queda lista para proyectar, el
versículo se carga en la pestaña Biblia y el medio se aplica de fondo.

## 8. Durante el servicio

**LIVE, PREVIEW y ESCENARIO** son las tres pestañas del recuadro de arriba a la
derecha: *LIVE* es lo que se está proyectando, *PREVIEW* la diapositiva que
sigue y *ESCENARIO* lo que ven los músicos, esté prendida o no esa pantalla.
Con un video en pantalla, sus botones se ven tanto en LIVE como en PREVIEW. Si
preferís dos recuadros, uno arriba del otro, tildá **Ver → Separar el PREVIEW
del LIVE** (ahí, PREVIEW sigue teniendo su **Siguiente / Escenario**); el
programa recuerda lo que elijas.

| Atajo | Qué hace |
|---|---|
| **←→** | Diapositiva anterior / siguiente (y salta al ítem contiguo de la playlist) |
| **F1** | *Black* — pantalla negra |
| **F2** | *Clear* — oculta el texto, deja el fondo |
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
- **Resaltar en vivo**: escribí una o varias palabras o frases separadas por
  **;** (`gracia; fe`) y se pintan como con marcador, en todas las veces que
  aparecen. También podés marcar con el mouse sobre el panel LIVE: un clic en
  una palabra o un arrastre sobre una frase la resalta **sólo esa vez**;
  con **Shift** se suma otra marca sin perder las anteriores, y marcar de
  nuevo algo resaltado lo saca. Al cambiar de diapositiva se borra todo.
- **Tiempos**: las **secciones de la reunión** con su duración (por ejemplo
  Alabanza 25 minutos, Anuncios 5, Prédica 35), que cambiás escribiendo. Elegís
  una con el círculo y la manejás con **▶ Iniciar / ⏸ Pausar**, **⟲** (vuelve
  al tiempo completo), **−1 min**, **+1 min** y **Siguiente ⏭**. Si se pasa del
  tiempo sigue contando en negativo y el número se pone **rojo**. Abajo está el
  **cronómetro** del servicio: iniciar, pausar y poner en cero. En la pantalla
  de escenario, el recuadro **Temporizador** muestra lo que le queda a la
  sección en marcha; la **cuenta regresiva** que se proyecta antes de empezar
  es otra cosa, con su propio recuadro.

## 9. Pantalla de escenario

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

### Varios escenarios y el editor

Lo de arriba es lo que muestra el escenario **Cantantes**. Vienen tres armados,
y podés hacer los que quieras:

- **Cantantes**: la letra en vivo grande y la que sigue.
- **Predicador**: la hora, el cronómetro, la cuenta regresiva y las notas.
- **Igual a la pantalla principal**: un solo recuadro con lo mismo que se
  proyecta, con su fondo.

La flecha **▾** de al lado del botón elige el escenario (se cambia al instante,
también con la pantalla prendida), abre el **editor**, elige en qué pantalla
va y **⟲ pone el cronómetro en cero**. No te deja elegir la misma pantalla que
la salida. El editor también está en **Proyección → Editar los escenarios…**.

En el editor, cada **recuadro** se mueve arrastrándolo y se agranda con las
manijas de los bordes; en el panel de la derecha elegís **qué muestra** (letra
en vivo, próxima diapositiva, hora, cronómetro, temporizador de la sección,
cuenta regresiva, notas, nombre de lo que está en vivo, texto fijo, aviso al pie o igual a la pantalla principal),
el tamaño de letra, el color y la alineación. Con **＋** hacés un escenario
nuevo, con ⧉ lo duplicás y con **Usar este escenario** elegís cuál se ve. Lo
que tenías configurado antes (hora, cronómetro, siguiente, notas) quedó
guardado como un escenario llamado **Mi escenario**.

## 10. Controlar desde el celular

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

## 11. Cuenta regresiva

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

## 12. Copia de seguridad

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

## 13. PC de bajo rendimiento

Para computadoras viejas o notebooks sin placa de video. Se prende en
**Ver → Modo PC de bajo rendimiento** (con tilde), y el instalador pregunta si
querés dejarlo prendido desde el principio. Con el modo prendido:

- las **transiciones cortan** en la pantalla, sin animación;
- el panel **LIVE** muestra la imagen fija del video en vez del video en
  movimiento;
- las **vistas previas** del operador se dibujan sin sombra ni contorno en la
  letra.

Lo que ve la congregación **no cambia**: la letra conserva su sombra y su
contorno, y el video se proyecta igual.

## 14. Música

En la columna derecha, abajo, la pestaña **Música** tiene dos partes.

**Audios.** Una lista propia de archivos de sonido (mp3, wav, m4a, ogg y flac)
para la música de antes de la reunión, la ofrenda o lo que haga falta.

- **➕** agrega audios (o arrastralos desde el Explorador a la lista). El archivo
  se queda donde está: la lista sólo se acuerda de dónde.
- **↑ ↓** suben o bajan el audio elegido; **A–Z** ordena toda la lista por
  nombre. **🗑** lo saca de la lista (el archivo no se borra; también con *Supr*).
- **Doble clic** (o *Enter*) reproduce el audio. Abajo está la barra: **⏮ ⏪ ▶/⏸ ⏩ ⏭**
  (anterior, 10 segundos atrás, reproducir o pausar, 10 segundos adelante,
  siguiente), la barra para moverte dentro del audio y el **volumen de los
  audios**, que es sólo de ellos: no cambia el del video ni el de Windows.
- Al terminar un audio sigue solo con el de abajo; al terminar el último, se
  detiene. **⏮** vuelve al principio del audio si ya pasaron unos segundos, y
  recién ahí pasa al anterior.
- Los audios suenan por la salida de sonido de la computadora, igual que el
  video. Para que no se pisen con otra cosa, bajales el volumen o pausalos
  antes de proyectar un video con sonido.

**Spotify u otro reproductor.** Los botones **⏮ ⏯ ⏭** y de volumen mandan las
**teclas multimedia de Windows**, así que manejan el **Spotify instalado en la
computadora** (o el reproductor que esté abierto). Ojo con el volumen: ése es el
de toda la computadora, el mismo que sale por el proyector.

La cuenta común de Spotify es para uso personal: pasar música en una reunión
pública queda por cuenta de la iglesia.

## 15. Actualizaciones

EcclesiaCast se fija solo si hay una versión nueva: **una vez por día**, a los
pocos segundos de abrirlo, le pregunta a GitHub cuál es la última. No manda
nada tuyo, y si no hay internet no te avisa de nada.

Si hay una versión nueva, se abre un aviso que dice qué trae, con tres botones:

- **Actualizar ahora** baja el instalador (se ve el avance, y **Cancelar** lo
  corta), comprueba que el archivo sea el que se publicó, guarda una copia de tu
  biblioteca y abre el instalador. Windows te pide permiso de administrador:
  aceptalo. EcclesiaCast se cierra, se actualiza y **se vuelve a abrir solo**,
  con todas tus canciones y lo demás como estaban. Si algo falla, el aviso dice
  qué pasó y se puede reintentar; no se instala nada a medias.
- **Más tarde** cierra el aviso. Vuelve a avisar la próxima vez que se mire (como
  mucho, mañana).
- **Saltar esta versión** no avisa más por esa versión. Cuando salga una más
  nueva, avisa de nuevo.

**Nunca interrumpe mientras proyectás.** Con la salida prendida no se abre
ninguna ventana: aparece un botón **⬆ Versión nueva** arriba a la derecha, en la
fila del menú, y al tocarlo se abre el aviso. El botón queda hasta que
actualices o saltes esa versión. Si actualizás con la salida prendida, antes te
pregunta, porque la proyección se va a cerrar.

En el menú **Ayuda**:

- **Buscar actualizaciones…** busca en el momento, aunque ya se haya buscado hoy
  y aunque hayas saltado esa versión. Si no hay nada nuevo, dice «Tenés la última
  versión».
- **Buscar actualizaciones al abrir** (con tilde de entrada) apaga la búsqueda
  automática. Podés seguir buscando a mano.

Antes de cada actualización queda una copia de la biblioteca en
`%APPDATA%\EcclesiaCast\respaldos` (se guardan las tres últimas), por si la
versión nueva no te gusta y querés volver: se restaura desde **Archivo →
Restaurar desde una copia…**.

## Dónde quedan tus datos

Todo (canciones, Biblias, temas, medios y playlists) vive en un solo archivo:

```
%APPDATA%\EcclesiaCast\ecclesiacast.db
```

**Copiar ese archivo es tu backup.** Los logs, por si algo falla, están en
`%APPDATA%\EcclesiaCast\logs\`.

Las copias livianas de los videos pesados están aparte, en
`%LOCALAPPDATA%\EcclesiaCast\copias-livianas\`. No hace falta respaldarlas:
si borrás esa carpeta, el programa vuelve a usar los originales y las rehace
solo. Si cambiás un video original (por ejemplo, lo volvés a exportar), su
copia vieja se descarta y se hace una nueva.
