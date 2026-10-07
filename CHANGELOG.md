# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [Sin publicar]

Lo que salió del primer servicio en vivo (27/9/2026).

### Agregado

- **Pestaña Música** (abajo a la derecha, junto a Texto rápido, Aviso al pie,
  Resaltar y Notas): botones ⏮ ⏯ ⏭ y de volumen que manejan el Spotify
  instalado en la computadora, o el reproductor que esté abierto, con las
  teclas multimedia de Windows. No lleva ningún reproductor adentro.

- **Modo PC de bajo rendimiento** (Ver → Modo PC de bajo rendimiento, con
  tilde): para computadoras viejas o notebooks sin placa de video. Las
  transiciones cortan en la salida, el panel LIVE muestra la imagen fija del
  video en vez del video en movimiento y las vistas previas del operador se
  dibujan sin sombra ni contorno. Lo que ve la congregación conserva su
  diseño. El instalador ahora tiene una casilla, sin tildar, «Configurar para
  una PC de bajo rendimiento»; si se tilda, el programa arranca con el modo
  prendido (mientras todavía no hayas elegido nada en el menú).

- **Editor de escenario y varios escenarios**: la pantalla de los músicos ya
  no es fija. En el editor (▾ junto al botón Escenario, o Proyección →
  Editar los escenarios…) ubicás y agrandás recuadros sobre la pantalla y a
  cada uno le decís qué muestra: letra en vivo, próxima diapositiva, hora,
  cronómetro, temporizador (lo que falta de la cuenta regresiva), notas para
  el predicador, nombre de lo que está en vivo, texto fijo, aviso al pie o
  «igual a la pantalla principal»; cada recuadro con su tamaño de letra,
  color y alineación. Se guardan varios con nombre y se cambia entre ellos
  desde el ▾, también con la pantalla prendida. Vienen tres armados:
  **Cantantes**, **Predicador** e **Igual a la pantalla principal**. Lo que
  tenías configurado antes (hora, cronómetro, siguiente, notas, tamaño de
  letra) pasa solo a un escenario llamado «Mi escenario». El PREVIEW
  «Escenario» muestra el que está elegido.

- **Arrastrar y soltar archivos en la barra de medios**: soltá desde el
  Explorador imágenes, videos, PDF o PowerPoint y se agregan a la pestaña
  elegida, igual que con «Agregar». Mientras arrastrás, la barra se marca con
  un borde de color; si soltás algo que no sirve, la barra de estado lo avisa.

- **Las transiciones también sacan el texto**: la letra que se va sale con
  el mismo movimiento con que entra la nueva (se funde, se va por la
  izquierda, sube o se achica), también al hacer Clear o poner el logo.
  En Temas, al elegir la transición o su duración, la vista previa muestra
  un ejemplo de ida y vuelta, y hay un botón «▶ Ver ejemplo».

- **Resaltar desde el LIVE como en un cuadro de texto**: mientras arrastrás
  sobre la letra, las palabras se marcan en azul sólo en el LIVE; al soltar
  se resaltan en la pantalla.

- **Reordenar diapositivas se ve**: la tarjeta que llevás sigue al mouse, la
  original queda desvanecida y una barra marca el hueco entre las dos donde
  va a caer. Se pone ahí, sin cambiar de lugar con otra. Cerca del borde de
  arriba o de abajo la grilla se desplaza sola.

- **Nombre arriba de las diapositivas**: el panel central dice qué tiene
  cargado («Mi Gozo — Barak», «Juan 8 · RVR1960») y marca «EN VIVO» cuando
  es lo que está en pantalla. Si en pantalla hay otra cosa (estás mirando lo
  que viene), a la derecha dice «En vivo: …».

- **Los medios de la playlist se ven antes de proyectarlos**: un clic los
  muestra grandes en el centro, y el doble clic (o el botón «Proyectar») los
  pone en pantalla. Con las flechas se siguen proyectando solos, como antes.

- **Arrastrar canciones a la playlist**, desde la lista de canciones, al
  lugar exacto: una línea marca dónde va a caer.

- **Suprimir quita de la playlist** el ítem elegido, y se puede apretar
  varias veces seguidas.

- **Pasajes en la playlist**: el botón «➕ Playlist» de la Biblia agrega un
  tramo escrito en el buscador («juan 8 5-10», o como se lee: «juan 8 del 5
  al 10»); si no hay tramo escrito, el versículo que está en pantalla; si no,
  el capítulo. Con clic derecho sobre un versículo se agrega ese, o todo lo
  que va desde el que está en pantalla hasta él.

- **Editar el nombre y la abreviatura de una Biblia** (✏️ en Versiones). La
  abreviatura nueva sale en la referencia y en los pasajes que ya estaban en
  las playlists.

- **Copias livianas de los videos pesados**: los videos 4K o de 60 cuadros
  por segundo (importados, descargados de YouTube o de logo) tienen solos una
  copia en 1080p a 30 cuadros, en H.264, guardada en
  `%LOCALAPPDATA%\EcclesiaCast\copias-livianas`. La biblioteca sigue apuntando
  al original; la copia se usa sólo al proyectar (y no con zoom para
  acercar). Se ve igual (0,996 de similitud) y un 4K a 60 pasó de 107 % a
  20 % de un núcleo y de 670 a 440 MB. Necesita ffmpeg; las copias se hacen
  de a una, con la menor prioridad, y nunca empiezan con la salida prendida.

- **Buscar canciones en internet** (📥 → Buscar en internet…, o Archivo):
  por nombre, artista o una frase de la letra. Busca en musica.com (su
  búsqueda rápida y su buscador completo, que encuentra por frase) y usa
  lrclib.net de respaldo. La letra llega separada en diapositivas, con los
  rótulos de la página («Coro:», «Verso 1») pasados a [Coro]/[Verso 1] y los
  párrafos largos partidos; se revisa y corrige antes de importar, y avisa si
  ya hay una canción con ese título.
- **Buscador de la Biblia por libro**: al escribir se filtran los libros
  («ju» → Jueces, Juan, Judas) y Enter completa el nombre para seguir con el
  capítulo; un solo libro posible abre directo sus capítulos. «juan 3 16»
  anda sin los dos puntos. Para buscar palabras dentro de los versículos se
  empieza con **?** («?paz»); Ctrl+K sigue buscando en todo.
- **PREVIEW con escenario**: dos botones, «Siguiente» y «Escenario», eligen
  si el PREVIEW muestra la diapositiva que sigue o la pantalla de escenario
  tal como la ven los músicos (aunque no esté prendida). Se recuerda.
- **Reordenar diapositivas arrastrando** en la grilla: la que soltás toma el
  lugar de la otra y el orden queda guardado en la canción.
- **Resaltar desde el LIVE**: clic en una palabra del panel LIVE la resalta en
  el proyector, arrastrar resalta una frase, y otro clic la saca.
- **Achicar la playlist o el panel de canciones y Biblia** con un clic en su
  título, para darle todo el alto al otro.
- **Menú arriba** (Archivo, Edición, Ver, Proyección, Ayuda) con todo lo que
  hoy son botones sueltos, más «Acerca de» y la guía de uso.
- **Diseño de la ventana**, desde el menú Ver: canciones y playlist a la
  derecha o a la izquierda, la playlist arriba o abajo de las canciones, los
  medios arriba o abajo de las diapositivas, y el PREVIEW arriba o abajo del
  LIVE. Todo queda guardado, con los anchos de cada panel, y «Volver al
  diseño original» deshace todo.

### Cambiado

- **Las pestañas de medios van apiladas a la izquierda**, una debajo de la
  otra y con la elegida resaltada, en vez de en fila arriba. El ＋ para
  agregar una pestaña quedó junto al título MEDIOS. La barra de medios
  arranca un poco más alta (170 en vez de 150) para que entren.
- **Se fue el botón «Sin fondo» de la barra de medios**: queda el de la barra
  de arriba, el menú Proyección y la tecla F4.
- **Un solo ➕ para agregar canciones**: al tocarlo ofrece Crear desde cero,
  Buscar en internet…, Traer de ProPresenter… e Importar archivo (.txt,
  .pro)…. Desaparece el botón 📥 ▾ que estaba al lado.
- **Menos botones arriba**: el ⏱ (cuenta regresiva) y el 💾 (copia de
  seguridad) salieron de la barra, porque ya estaban en los menús. En
  Proyección → Cuenta regresiva… el ítem lleva una tilde y dice «en marcha»
  mientras la cuenta corre; guardar y restaurar copias siguen en Archivo.
- **Ayuda → Acerca de** es una ventana propia, oscura como las demás: quién
  desarrolló el programa, para qué se hizo, el versículo (Mateo 10:8), la
  licencia y el enlace a GitHub, que se abre con un clic.
- **Ctrl+K busca en todas las Biblias instaladas** y cada versículo dice de qué
  versión es («Salmos 55:22 · RVR1960»). Al elegirlo se abre en esa versión,
  no en la que estaba elegida. Cada versión aporta como mucho unos pocos
  resultados, para que una no tape a las demás. En la Biblia la frase tiene
  que empezar al principio de una palabra: «echa sobre» ya no se llena de
  «derecha sobre» antes de llegar a «Echa sobre Jehová tu carga».
- **F1 es Black y F2 es Clear** (antes al revés), y los botones de arriba
  quedaron en ese orden.
- El panel **LIVE va arriba** y el PREVIEW debajo: lo que está en pantalla es
  lo que más se mira.
- La **barra de reproducción del video** (reproducir, pausar, ±10 s y la
  barra para moverse) pasó de la barra de medios a **debajo del LIVE**. Con el
  logo puesto se oculta, porque el video que sigue corriendo detrás no es lo
  que ve la congregación.
- **Un clic en un ítem de la playlist lo carga sin proyectarlo** (antes hacía
  falta el clic derecho). El doble clic lo proyecta, como siempre. En los
  medios de la playlist el clic sólo lo marca, porque cargar un medio ya lo
  pone en pantalla.
- **Fondo al azar por canción**: si elegiste un fondo a mano, se respeta. El
  sorteo sólo elige cuando no hay fondo o cuando el que está salió sorteado.
- **Los versículos llenan la pantalla**: el tema de Biblia arranca en letra
  120 (era 76). Un versículo corto ya no queda chico en el medio, y uno largo
  se sigue achicando hasta entrar. Si ya habías cambiado ese tamaño a mano,
  no se toca.
- Las **tarjetas de las diapositivas** son un poco más grandes, todas iguales,
  y muestran la letra entera: si no entra, se achica en vez de cortarse con
  «…».
- En la lista de canciones, las que no tienen artista ocupan un solo renglón.
- **El texto nunca se sale del recuadro.** Si ni con la letra mínima del tema
  entra, el versículo se reparte en dos o más diapositivas («2:2-18 (1/2)»),
  cortando al final de una oración. Con dos versiones se reparten las dos
  juntas. En las canciones, que no se parten, la letra se sigue achicando
  por debajo del mínimo en vez de cortarse. Probado con el versículo más
  largo (Ester 8:9) y uno de los más cortos (Juan 11:35).
- **Los versículos que la TLA cuenta juntos van en una sola diapositiva**
  («Génesis 32:3-4»), sin los números que la TLA pone al principio («3-4
  (4-5)»). Antes el mismo párrafo se repetía en cada versículo: en 2 Crónicas
  2 eran 17 diapositivas iguales de casi 3.000 letras.
- **El texto ya no se pisa con la referencia**: si el recuadro del tema pasa
  por encima de la referencia, el texto se acomoda en lo que queda libre.
- El **número de la tarjeta** va abajo a la derecha, encima del texto, en vez
  de ocupar un renglón arriba.

### Corregido

- **Las vistas previas de las imágenes volvían en blanco al reabrir el
  programa** (se notaba en Anuncios). La miniatura es un archivo aparte de
  la imagen; si ese archivo desaparecía, la biblioteca seguía apuntando a él
  y sólo se rehacía en los videos. Ahora al abrir se vuelven a hacer las de
  las imágenes que la necesiten, y la limpieza de miniaturas ya no borra las
  que se están creando en ese momento. Además, un medio sin pestaña (no se
  veía en ninguna) pasa a Fondos, y una copia de prueba del programa ya no
  toca las miniaturas de la biblioteca real.
- **Los saltos de línea ya no cortan la búsqueda en las letras**: «las cadenas
  de la muerte» encuentra *Hay Libertad* aunque la letra tenga un salto entre
  «cadenas» y «de». Los espacios de más y los saltos cuentan como uno solo,
  tanto en lo que se escribe como en lo guardado, y también vale para el
  texto de la Biblia. Sirve en Ctrl+K y en el buscador de la lista de
  canciones.
- **El resaltado quedaba puesto** al pasar a otra diapositiva o versículo,
  marcando la misma palabra en el texto nuevo. Ahora cada diapositiva
  arranca sin resaltar.
- **El doble clic en la playlist no proyectaba**: la fila se quedaba con el
  clic antes de que la lista lo viera. Ahora lo atiende la fila misma, y lo
  mismo en la lista de canciones y en los resultados de búsqueda de la
  Biblia, que estaban armados igual.
- Abrir un capítulo entero desde la playlist podía proyectar, con doble
  clic, un versículo que había quedado marcado de lo anterior.
- **Un versículo solo quedaba corrido hacia arriba**: el lugar de la segunda
  versión se reservaba igual aunque no hubiera segunda versión, y en uno
  largo eso sacaba la última línea del recuadro.
- **Descargar de YouTube fallaba con «Could not copy Chrome cookie
  database»**: el navegador del programa a veces tenía ocupado el archivo de
  la sesión justo cuando yt-dlp lo quería leer. Ahora el navegador no lo
  bloquea, la descarga reintenta una vez y, si igual no puede, baja el video
  sin la sesión (la mayoría no la necesita).
- **Playlist que mostraba siempre lo primero**: si en el buscador de
  canciones quedaba algo escrito, las canciones de la playlist que no
  coincidían con esa búsqueda no se podían abrir. Ahora se borra el filtro.
- **Volver de la Biblia a la misma canción** dejaba el capítulo en la
  grilla, porque la canción «ya estaba elegida».
- Enter en la caja de la Biblia, con una referencia escrita de antes y una
  canción en la grilla, proyectaba una diapositiva de la canción.
- **El video tironeaba con el ahorro de energía de Windows**: ahora lo
  decodifica la placa de video. Medido con los loops HEVC de 1920×1280 de la
  iglesia: cerca de la cuarta parte del procesador que antes, y 22 de 22 a 30
  cuadros por segundo.
- **Imagen después de un video quedaba trancada en el LIVE**: al poner una
  imagen (la de la ofrenda) sobre un video, el panel LIVE del operador seguía
  mostrando el último cuadro del video, y en el proyector ese cuadro quedaba
  escondido detrás de la imagen. Un cuadro que ya estaba en camino se dibujaba
  después de borrar la pantalla: pasaba en 19 de 20 cambios; ahora en 0.
- **El panel LIVE ya no carga la PC con el video**: recibía el cuadro entero
  del proyector (1920×1280, unos 10 MB) 30 veces por segundo para mostrarlo
  en una cajita de 300 píxeles. Ahora recibe una copia chica, 15 veces por
  segundo. Un video pausado y reemplazado por una imagen ya no queda abierto
  ocupando memoria.
- Windows ya no pone a EcclesiaCast en «modo eficiencia» cuando la ventana
  del operador no está al frente (plan Equilibrado), que es lo que puede
  hacer tironear el video si se hace clic en otro programa.

## [1.0.0-beta.4] — 2026-09-11

### Agregado

- PDF y PowerPoint: al agregar medios ahora se pueden elegir archivos `.pdf`
  y `.pptx`. Cada página entra como un fondo de pantalla completa, en orden,
  listo para poner en la playlist y pasar con las flechas. El PDF lo dibuja
  el propio Windows (no hace falta instalar nada) y las presentaciones las
  convierte el PowerPoint que tengas; si no tenés PowerPoint, el programa te
  dice que la exportes a PDF.
- Buscador único (botón 🔍 o **Ctrl+K**): una sola caja que busca al mismo
  tiempo en canciones, en la Biblia activa y en los medios. Las flechas
  recorren los resultados sin sacar el cursor de la caja y Enter carga el que
  elijas.
- Salida para la transmisión: la ventana del control remoto (📱) ahora trae
  una dirección para pegar en el «Browser Source» de OBS o el «Web Input» de
  vMix. Sale la letra que está proyectada, sola y con el fondo transparente,
  para ponerla encima de la cámara — sin placa capturadora ni una segunda
  computadora. Agregando «&fondo=negro» se ve sobre negro, para los programas
  que no manejan transparencia.
- Listas inteligentes (botón ✨ del panel PLAYLIST): listas que se arman
  solas con la biblioteca — lo que agregaste hace poco, lo que hace rato que
  no cantás, todo lo de un artista o todo lo que diga una palabra. Para que
  la segunda funcione, ahora se anota cuándo se proyectó cada canción.
- Recorte y color de los medios, en el Inspector: elegís desde qué segundo
  arranca un video y hasta cuál llega (para saltear la cortina con que
  arrancan muchos videos, y que el bucle respete el recorte), y ajustás el
  brillo —oscurecer un fondo cargado es lo que hace legible la letra— y un
  tinte de color.
- Varios cuadros de texto por diapositiva, desde el diseñador: además de la
  letra, podés agregar los que quieras (el nombre de la serie, la cita, una
  traducción), moverlos y redimensionarlos igual que la caja de siempre, y
  darle a cada uno su tipografía, tamaño, color y alineación. Se escriben con
  doble clic sobre la diapositiva. «Aplicar formato a todas» reparte el
  formato, no las palabras: cada diapositiva conserva sus propios cuadros.
- Encuadre de imágenes y videos en el Inspector de medios: zoom hacia adentro
  y hacia afuera, correrlo a los lados o arriba y abajo, y un tamaño fijo en
  pantalla (con medidas comunes a mano) para cuando la pantalla de la iglesia
  no tiene la forma de la imagen del proyector. Lo que el medio no cubre se
  rellena con un color o con otra imagen o video de la biblioteca — el truco
  de siempre es una copia desenfocada del mismo video.
- Transiciones por tema: además del fundido de siempre, la diapositiva puede
  entrar desde la derecha, subir desde abajo, acomodarse con un zoom suave o
  no tener transición (corte). Se elige en el editor de temas, con su
  duración, así las canciones pueden fundir y la Biblia cortar. Los temas que
  ya existen se quedan con el fundido que venían haciendo.
- Notas para la plataforma, en la pestaña «Notas» de la columna derecha: se
  ven sólo en la pantalla de escenario — el guion de quien predica, un aviso
  para la banda — y nunca llegan a la congregación. Si son muchas, se achican
  para entrar enteras, y quedan guardadas para la próxima vez.
- Cuenta regresiva antes del servicio, en el botón ⏱ de la barra de arriba:
  se proyecta sobre el fondo que tengas puesto, contando los minutos que
  faltan o hasta una hora del reloj, con un texto arriba («Empezamos en») y
  otro para cuando llega a cero («¡Bienvenidos!»). También se ve en la
  pantalla de escenario, para que la banda sepa cuánto falta. Lo que escribís
  queda guardado para el domingo siguiente.
- Respaldo y restauración de la biblioteca desde el programa, en el botón 💾
  de la barra de arriba: guarda todo (canciones, Biblias, temas, medios y
  playlists) en un solo archivo `.ecbackup` con el programa abierto, y lo
  vuelve a poner cuando hace falta. La restauración se aplica al reabrir el
  programa y deja guardada la biblioteca anterior por las dudas.
- Límite de intentos del PIN en el control remoto: cinco PINes equivocados
  desde el mismo teléfono lo dejan afuera cinco minutos, con la espera a la
  vista y contando. Cada teléfono cuenta por su cuenta, así que uno bloqueado
  no deja afuera al resto del equipo, y apagar y prender el control remoto
  destraba a quien se bloqueó solo.
- Varios logos de iglesia, uno por tipo de reunión: cada uno puede ser una
  imagen, un video en bucle o texto libre con su tipografía, tamaño y colores.
  La flecha al lado de Logo (F3) los lista y cambia al vuelo; se administran
  en su propia ventana con vista previa.
- El logo se puede poner de fondo, detrás de la letra, con el desenfoque que
  tenga configurado.
- Importación nativa desde ProPresenter: busca sola dónde está instalado
  (incluida la carpeta de OneDrive), muestra sus bibliotecas con cuántas
  canciones tiene cada una y las trae todas de una vez, salteando las que ya
  están. También se puede señalar una carpeta a mano.
- Corrector ortográfico en español al escribir letras, avisos y textos de
  logo: subraya las palabras dudosas y ofrece sugerencias con el clic derecho.
  Trae un diccionario propio con vocabulario bíblico, al que se le pueden
  agregar palabras desde el mismo menú.
- Reproducción continua por pestaña de medios: al terminar un video arranca
  el siguiente y al final vuelve al primero, para dejar un bucle de avisos.
- Descarga de videos de YouTube para uso local (requiere yt-dlp instalado
  aparte), que quedan en su propia pestaña «Descargados».
- Fondo al azar: un botón que sortea uno de la pestaña que estés viendo, y una
  opción para que cada canción se lleve un fondo distinto al proyectarla. El
  sorteo no repite hasta agotar la pestaña ni da el mismo dos veces seguidas.
- Pantalla de escenario: un tercer monitor para los músicos y el predicador,
  con la letra que está proyectada, la que sigue, la hora y un cronómetro del
  servicio. Muestra la letra incluso con Clear o Black puestos, y avisa cuál
  de los dos está activo. Se elige qué mostrar y de qué tamaño.
- Control desde el celular por la red de la iglesia: flechas, Clear / Black /
  Logo, sin fondo, prender y apagar la salida, saltar a cualquier diapositiva
  y a cualquier elemento de la playlist. Se entra escaneando un código QR y
  con un PIN de cuatro dígitos, y queda encendido para el próximo servicio.

- Los tamaños se escriben como número, con botones − y + de a un punto (Shift
  salta de a diez): tamaño de letra, interlineado, tamaño de la caja en
  porcentaje y su posición exacta en píxeles.
- Contorno de la letra (grosor y color) e intensidad y difusión de la sombra,
  tanto en el tema como diapositiva por diapositiva. Resuelve la letra blanca
  sobre fondo blanco.
- El tema elige el uso de mayúsculas: como está escrito, TODAS MAYÚSCULAS,
  Cada Palabra, como oración o todas minúsculas.
- Barra de reproducción del fondo: volver al principio, ±10 segundos,
  reproducir/pausar y una barra de progreso arrastrable, para videos de
  archivo y de YouTube.
- Desenfoque del fondo (imagen, video o YouTube) ajustable en vivo desde la
  barra de medios, y guardado con cada medio en el Inspector.
- "Sin fondo" pasó a la barra de arriba con la tecla F4.

### Cambiado

- El texto rápido ocupa la columna derecha, desde abajo del Live hasta el pie.
- La barra de medios se ubica abajo a la izquierda, se agranda arrastrando
  (el alto queda guardado) y los medios se recorren en vertical.
- Los botones explican qué hacen al pasar el mouse, y los globos duran 30 s.

### Corregido

- Leer un capítulo a mano ya no salta al siguiente elemento de la playlist:
  antes, si la playlist tenía un pasaje (ej. Juan 3:16-18) y el operador
  recorría ese mismo capítulo por su cuenta, al llegar al final del rango la
  flecha se iba al elemento siguiente.
- El panel LIVE del operador muestra el video de fondo **en movimiento**, no
  una foto fija: dibuja el mismo cuadro que está en el proyector, sin
  decodificar nada dos veces (+32 MB y la misma CPU). Los videos de YouTube
  siguen mostrándose como póster.
- El video de fondo usa casi 100 MB menos de memoria (742 → 649 MB con un
  1080p): el colchón de lectura de VLC pasó de segundo y medio a medio
  segundo, que para un archivo del disco de la misma PC alcanza.
- La página para la transmisión ya no se bloquea sola: si la dirección pegada
  en OBS tiene un PIN viejo, el programa la rechaza pero **no** deja afuera a
  esa computadora, y al corregir la dirección entra en el acto. Antes se
  bloqueaba a segundo y medio de insistir, y quedaba cinco minutos afuera aun
  con la dirección ya corregida.
- Un lector de pantalla ahora dice qué es cada cosa: las canciones, las
  diapositivas, los pasajes y los ítems de la playlist se anuncian por su
  nombre en vez del nombre interno del tipo, y los botones que son sólo un
  ícono llevan su nombre escrito.
- Apagar la salida dejaba el video sonando y avanzando; ahora se pausa y
  retoma donde estaba cuando la salida vuelve.
- Quitar el fondo dejaba la pantalla vacía cuando el fondo se había aplicado
  sin diapositiva en vivo; ahora la letra vuelve sola.

## [1.0.0-beta.2] — 2026-07-20

### Corregido

- Al navegar la playlist con las flechas, entrar a un pasaje bíblico caía en
  el versículo 1 del capítulo en vez del primer versículo del pasaje, el
  pasaje no terminaba nunca (la flecha seguía de largo por el capítulo y hasta
  cruzaba de capítulo en vez de pasar al siguiente elemento), y entrar a un
  medio proyectaba encima el texto del elemento anterior.
- Apretar Enter dos veces sobre una referencia tipeada ("Juan 3:16")
  proyectaba el versículo 1 del capítulo; ahora repite el versículo pedido.

## [1.0.0-beta.1] — 2026-07-19

Primera versión instalable de EcclesiaCast.

### Proyección

- Salida a un segundo monitor, con selector de pantalla y encendido/apagado.
  La ventana de salida nunca roba el foco del teclado.
- Estados Clear / Black / Logo (F1, F2, F3) y apagado de salida (Esc).
- Vista previa de la diapositiva siguiente y de lo que está en vivo.
- Aviso al pie (lower third) por encima de todo lo proyectado.
- Resaltado tipo marcador sobre el texto en vivo.
- Texto rápido para anuncios al momento (Ctrl+Enter).

### Canciones

- Biblioteca de canciones con autor, edición y borrado.
- Importación desde archivos `.txt` y desde archivos nativos de ProPresenter 7
  (`.pro`), incluidos los bloques RTF embebidos.
- Grilla de diapositivas con desplazamiento automático hacia la que está en vivo.

### Biblia

- Varias versiones importables desde JSON (formato de repositorios libres y
  formato tipo YouVersion) y desde XML Zefania.
- Catálogo de los 66 libros con abreviaturas en español y búsqueda de
  referencias tolerante ("Juan 3:16", "jn 3:16-18", "sal 23").
- Hasta dos versiones en pantalla a la vez.
- Navegación continua entre capítulos y libros con tarjetas de salto.

### Temas

- Temas de formato editables, con tema propio por canción.
- Diseñador de diapositiva con caja arrastrable, manijas de tamaño y edición
  del texto en el lugar.
- Tipografía, color, alineación, sombra, fondo y oscurecedor configurables.

### Fondos y medios

- Biblioteca de medios con pestañas por categoría.
- Fondos de imagen y de video en bucle, con el texto por encima.
- Comportamiento Fondo o Primer plano, escalas Rellenar / Ajustar / Estirar,
  fin del video en Bucle / Detener / Logo, silencio y volumen.
- Videos de YouTube proyectados con el reproductor oficial, usando la sesión
  del propio navegador integrado.
- Inspector de propiedades por medio.

### Playlist

- Playlists del servicio con canciones, pasajes y medios.
- Navegación continua con las flechas entre los elementos de la playlist.
- El tamaño de los paneles y de la ventana se recuerda entre sesiones.

### Instalación

- Instalador para Windows de 64 bits, con .NET y VLC incluidos: no hace falta
  instalar nada más.
- El runtime de WebView2 (necesario para YouTube) se instala automáticamente
  si falta.
- Los datos (canciones, Biblias, temas y medios) viven en
  `%APPDATA%\EcclesiaCast` y **no** se borran al desinstalar.
