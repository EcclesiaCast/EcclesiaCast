# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [Sin publicar]

## [1.0.0] - 2026-10-21

Primera versión estable: todo lo que se fue sumando en las betas, probado en los
servicios de la Iglesia Pan de Vida. Si tenés una beta instalada, se actualiza
sin perder canciones, Biblias, medios ni ajustes.

### Qué trae

- **Canciones:** biblioteca con búsqueda al instante, importación desde `.txt` y
  desde ProPresenter 7, y búsqueda de letras en internet.
- **Biblia:** varias versiones, búsqueda por referencia («jn 3:16», «sal 23») o
  por texto, dos versiones a la vez e historial de versículos. El instalador trae
  Biblias libres para elegir (Reina-Valera 1909, Biblia libre para el mundo,
  Versión Biblia Libre y Palabra de Dios para ti); las demás se importan, o se
  llevan de otra computadora tuya con un paquete de Biblias.
- **Medios:** imágenes, videos, GIF, PDF y PowerPoint, en pestañas, como fondo o
  en primer plano. Imágenes y GIF de Google, páginas web y otras ventanas de
  Windows en pantalla, y las imágenes de un evento pasan solas a «Eventos
  pasados» cuando termina.
- **Proyección:** Clear, Black, Logo, aviso al pie, cuenta regresiva,
  transiciones y resaltado de palabras desde el LIVE.
- **Temas y diseño:** tipografía, color, contorno, márgenes y fondo, con un
  editor visual y varios cuadros de texto por diapositiva.
- **Pantalla de escenario** para los músicos, con varios escenarios y su editor.
- **Playlist del servicio**, que se maneja entera con el teclado, y búsqueda de
  todo junto con Ctrl+K.
- **Música:** lista de audios y mini reproductor de Spotify.
- **Desde el celular:** manejar la proyección y buscar canciones o versículos.
- **Para la transmisión (OBS o vMix):** la salida como página transparente, todo
  junto o en tres capas (Biblia, canciones y anuncios).
- **Copia de seguridad**, modo para PC de bajo rendimiento y actualizaciones
  automáticas.

## [1.0.0-beta.5 a 1.0.0-beta.16] — 2026-10-03 a 2026-10-16

Lo que salió del primer servicio en vivo (27/9/2026).

### Agregado

- **Una Biblia libre más en el instalador: «Palabra de Dios para ti»** (PDDPT, de
  la Asociación Bíblica Latinoamericana; licencia libre CC BY-SA 4.0). Se tilda
  en la página de Biblias del instalador, junto a la Reina-Valera 1909, la Biblia
  libre para el mundo y la Versión Biblia Libre. Sigue el texto crítico, así que
  algunos versículos difieren de la Reina-Valera (en Marcos 16 el texto termina
  en el versículo 9).
- **Logo nuevo.** El ícono del programa, la ventana «Acerca de» y la pantalla de
  carga ya no llevan la cruz: ahora es una pantalla con una llama sobre azul
  noche. La pantalla de carga se rehízo: un haz de luz de proyector sobre el
  nombre y la versión, con el lema del programa.

- **La Biblia, las canciones y los anuncios, cada uno en su capa de la
  transmisión.** Además de la dirección de siempre (`/salida`, todo junto), hay
  tres más para pegar en un **Browser Source** distinto de OBS (o un Web Input de
  vMix): `/salida/biblia`, `/salida/canciones` y `/salida/anuncios`. Cada una
  muestra sólo lo suyo, con el fondo transparente, y queda vacía mientras eso no
  está al aire. La de anuncios incluye el texto rápido, las **imágenes y videos**
  que están solos en pantalla (también las páginas de una presentación), el
  aviso al pie y la cuenta regresiva. En la ventana del 📱 hay un botón que copia
  la dirección de cada una. El video va sin sonido (agregando `&sonido=1` suena) y
  se repite si el medio está en bucle. Por dentro, el servidor ahora entrega los
  archivos por tramos, que es lo que hace falta para que un video se repita en
  el navegador de OBS.
- **Cómo preparar una Biblia para importar.** En **Archivo → Cómo preparar una
  Biblia para importar…** hay una ventana que explica los dos formatos que se
  leen (JSON y Zefania XML), con un ejemplo de cada uno que se puede guardar y
  abrir con el Bloc de notas, y un botón **📋 Copiar instrucciones para una IA**:
  un texto para pegar en un chat de IA junto con unas líneas de tu archivo, para
  que arme un programita que convierta tu Biblia (Word, PDF, texto) al formato
  del programa. Los ejemplos se arman con el mismo escritor del programa y las
  pruebas comprueban que el importador los lee, así que no pueden quedar
  desactualizados. Si una importación falla, el mensaje manda a esa ventana.
- **Vencimiento de una imagen.** En Propiedades, «Fecha del evento (opcional)»
  y «se queda N días más» (10 por defecto): cuando llega ese día, la imagen pasa
  sola a la pestaña **Eventos pasados** (que se crea sola) y el programa lo
  avisa en la barra de estado. Se revisa al abrir el programa y al cambiar la
  biblioteca. El archivo no se toca: sólo cambia de pestaña.
- **Una imagen nueva entra en «Ajustar» y con el relleno de la misma imagen
  desenfocada.** Vale al agregarla con ➕, arrastrándola o con «Agregar a
  medios» de Google. Los videos, los PDF y lo que ya estaba no cambian.
- **«➕ Agregar la abierta» en Google Imágenes.** Un botón en la fila de
  imágenes del panel web, y el clic derecho sobre la imagen grande (aunque
  Google la tape con un vínculo), agregan a los medios la imagen original.
- **Imágenes y GIF de Google.** El botón **🖼 Imágenes** de la barra de medios
  abre Google Imágenes en el panel web (en castellano, con todos sus filtros), y
  el panel tiene una fila para escribir qué buscás, con **🖼 Imágenes**, **🎞 GIF**
  (sólo animados) y «Sólo licencia libre» (el filtro «Derechos de uso» de
  Google). Con clic derecho sobre una imagen (aunque sea una miniatura sin abrir)
  aparece un menú propio, en el lugar del clic, con **➕ Agregar a medios**: baja
  la imagen **original** (la dirección la da el propio Google) y entra a la
  pestaña abierta, y si sólo se conoce la miniatura avisa en vez de bajarla
  borrosa. Un GIF animado se convierte en video en
  bucle como los que arrastrás. Avisa si la imagen es chica para una pantalla
  grande. Quitarla de la biblioteca borra el archivo bajado. No usa API ni
  claves.
- **Buscar desde el celular.** La página del celular tiene arriba una caja para
  buscar canciones y versículos (todas las versiones de la Biblia), con la
  misma búsqueda que Ctrl+K de la computadora, sin los medios: «juan 3 16» da
  el versículo, y una canción que se llama como lo escrito sale antes que las
  que sólo lo dicen en la letra (eso también vale para Ctrl+K). Tocar un
  resultado lo carga en la computadora y la página baja a sus diapositivas
  para elegir cuál proyectar.
- **Rellenar los bordes con la misma imagen, desenfocada.** En Propiedades de
  una imagen, «Los bordes se rellenan con» ofrece **(la misma imagen,
  desenfocada)** además del color sólido: donde la imagen no llega (en
  «Ajustar», con un tamaño fijo o con zoom menor a 100 %) se ve una copia
  agrandada y borrosa de ella misma, para que parezca una sola imagen sin
  borde negro (el desenfoque se dibuja a mano para que no deje un borde negro
  abajo). Al pasar de una imagen a otra, ese relleno sube con el fundido. Se ve
  igual en la vista previa, las tarjetas, el LIVE y la
  pantalla.
- **Ajuste fino con las flechas del teclado en Propiedades.** Con el cuadro de
  vista previa elegido (arranca así), las flechas corren la imagen de a 0,1 %
  de la pantalla, y con Mayús de a 1 %.
- **Achicar la lista de audios.** En la pestaña Música, un clic en el título
  **AUDIOS** (o Ver → «Achicar la lista de audios») esconde la lista y sus
  botones y el reproductor de audios (la música sigue sonando). Se recuerda al
  cerrar el programa.
- **Mini reproductor en la pestaña Música.** Donde estaban los botones de
  «Spotify u otro reproductor» ahora se ve la carátula, el título, el artista,
  de qué programa viene y una barra de avance de lo que suene en la
  computadora, con anterior, pausa y siguiente que le hablan directo al
  reproductor (si no hay ninguno, mandan la tecla multimedia como antes). El
  volumen sigue abajo. Lo lee de los controles multimedia de Windows: no pide
  cuenta de Spotify. Sólo escucha mientras la pestaña está a la vista. No
  muestra la fila de reproducción: Windows no la da.
- **Dos canciones con el mismo título y distinto artista.** La «clave» de una
  canción ahora es título + artista: «Hay Libertad» de IBI y «Hay Libertad» de
  Christina Aguilera entran las dos sin preguntar. Sólo se pregunta qué hacer
  si coinciden las dos cosas (o si una de las dos no tiene artista escrito),
  y la pregunta nombra al artista.
- **Historial de versículos buscados, como en BibleShow.** Al lado de la caja
  de búsqueda de la Biblia hay un botón **🕘** (o la flecha ↓ con la caja
  vacía) que abre la lista de los últimos 30 versículos que proyectaste desde
  ahí, el más reciente arriba, con la versión en que los leíste («Juan 3:16 ·
  NTV»). Un clic carga el capítulo y marca el versículo (también vuelve a
  poner la versión, y la segunda si había, siempre que sigan instaladas); doble
  clic o Enter lo proyecta. **Borrar historial** la vacía. La lista se
  recuerda aunque se cierre el programa. No se anotan los versículos a los que
  se llega con las flechas ni con clics en las tarjetas.
- **Usar una imagen o un video de la barra de medios como logo.** Con clic
  derecho en la tarjeta, **Usar como logo** lo suma a la lista de logos sin
  pasar por el administrador de logos. Si ese archivo ya era un logo, lo deja
  elegido en vez de repetirlo. No cambia lo que hay en pantalla: **F3** lo
  muestra. No aparece en las páginas de un PDF o de una presentación, porque se
  borran junto con el medio.

- **Silenciar una pestaña de medios entera.** Con clic derecho en la pestaña,
  **🔇 Silenciar la pestaña** (y **🔊 Quitar el silencio** para volver): todos los
  videos de esa pestaña se proyectan sin sonido, incluidos los de YouTube, y
  la pestaña muestra un 🔇 al lado del nombre. También está como casilla en
  **Configurar…**. Si lo que suena en pantalla es de esa pestaña, el cambio se
  escucha en el momento, sin reiniciar el video. Las Propiedades de cada medio
  no se tocan: al quitar el silencio, cada uno vuelve a sonar como estaba.

- **Varios cuadros de texto en el diseñador de diapositivas, sin pisarse.** Antes
  el segundo cuadro caía justo encima del primero y no se podía elegir ni
  editar el de abajo. Ahora: cada cuadro nuevo aparece en un lugar libre (abajo
  al centro, arriba al centro, las esquinas…); los otros cuadros se ven con un
  contorno punteado y su nombre («Letra», «Cuadro 2»); un clic en cualquiera lo
  elige, aunque se esté escribiendo en otro; **Tab** pasa al siguiente cuadro; y
  **▲ Traer adelante** / **▼ Enviar atrás** cambian cuál se dibuja encima. Al
  cambiar de cuadro con la lista, la tipografía escrita sin confirmar ya no se
  pierde.

- **Tamaño de las tarjetas.** Al pie de la grilla de diapositivas y debajo de
  las pestañas de medios hay una barra para agrandar o achicar las tarjetas.
  Con Ctrl y la rueda del mouse sobre la grilla o la barra de medios se cambia
  lo mismo. El tamaño queda guardado para la próxima vez.
- **Diseño de aprendizaje o avanzado.** En Ver elegís si los botones muestran
  los atajos de teclado (F1, F2, Esc, Ctrl+Enter) y los textos de ayuda, o si
  los ocultan para ver menos texto. Los atajos andan igual en los dos.
- **Tipografía y tamaño para el aviso al pie.** En la pestaña **Aviso al pie**
  hay una lista **Tipografía** (se elige de las que tiene la computadora, o se
  escribe el nombre) y un **Tamaño** de 20 a 160. El LIVE y la pantalla lo
  muestran al instante, y se recuerda aunque se cierre el programa. Si queda
  vacía, sigue Segoe UI de tamaño 54, como antes.

- **Transiciones en segundos para el logo, los medios, el negro y el apagado.**
  En **Proyección → Transiciones…** se elige cuántos segundos tarda: el logo en
  aparecer y desaparecer (antes aparecía de golpe), el cambio de una imagen o
  video a otro (antes 0,32 s fijos), la pantalla negra (antes 0,25 s fijos) y
  el apagado de la salida, que ahora puede irse a negro de a poco en lugar de
  cerrarse seca (por defecto sigue cortando). El aviso al pie y la cuenta
  regresiva también se funden al irse. Con 0 s cada cosa corta de golpe, y con
  el modo de bajo rendimiento todo corta igual. En los temas, la duración de la
  transición de la letra ahora se escribe en **segundos** (la de siempre, 0,28
  s, queda igual) y el máximo sube a 5 s. Un YouTube no se puede fundir (es una
  ventana aparte de Windows): se esconde de golpe.
- **Los GIF animados se mueven.** Antes un GIF se veía como una foto fija (sólo
  su primer cuadro). Ahora, al agregar un GIF animado (con 📥 o arrastrándolo),
  el programa lo convierte en un video MP4, en bucle y sin sonido, que guarda
  en su carpeta de datos y agrega a la pestaña; el GIF original queda donde
  estaba. La conversión se hace en segundo plano (unos segundos) y la barra de
  estado avisa. Si no se encuentra ffmpeg, o la conversión falla, el GIF entra
  como imagen fija, como hasta ahora. Quitar el video de la biblioteca borra
  también el MP4 convertido.

- **Ver y cambiar las presentaciones desde el celular.** La página del celular
  ahora muestra las páginas de la presentación abierta como miniaturas (no
  como texto), con la que está en pantalla marcada, y un toque en una la
  proyecta. Aparece una sección **Presentaciones** con las pestañas marcadas
  como presentación: un toque la abre en la computadora. Con una página en el
  aire, el cuadro «En vivo» del celular muestra su miniatura en lugar de la
  letra de antes, y «Sigue» muestra la página que viene. Las miniaturas se
  piden una sola vez y el celular las guarda una hora; sólo se envía la
  miniatura chica, nunca la imagen grande ni ningún otro archivo.

- **«En vivo» por sección.** Antes, después de proyectar un versículo y pasar a
  una página de una presentación, la lista de versículos seguía diciendo «EN
  VIVO». Ahora la marca sigue lo que de verdad está en pantalla: la grilla
  central tiene **borde rojo** y «EN VIVO» sólo mientras muestra lo que se
  proyecta; la barra de medios tiene borde rojo cuando lo que está en pantalla
  es un medio (y su tarjeta, también); las pestañas **Canciones** y **Biblia**
  llevan un punto rojo cuando lo que sale viene de ahí. Con Black, el logo o la
  salida apagada, nada queda en rojo. La tarjeta que se proyectó por última vez
  pero ya no está en el aire queda con un borde apagado. Un medio en pantalla
  completa aparece en «En vivo: …», y al volver la letra, vuelve su título. El
  celular marca en vivo sólo lo que de verdad está en el aire.

- **Una pestaña de medios como presentación.** Al importar un PDF o una
  presentación, el programa pregunta si las páginas van a una **pestaña nueva,
  como presentación** (lo recomendado) o a la pestaña que está abierta. Una
  pestaña marcada como presentación (casilla en **Configurar…**, o clic derecho
  → **Abrir como presentación**) se abre en el cuadro central como una canción:
  una tarjeta por página, con su miniatura. Un clic proyecta la página sola
  (sin letra encima), las flechas pasan de una a otra, y el PREVIEW muestra la
  que sigue. Al proyectar una estrofa o un texto rápido, vuelve el fondo que
  había antes. **➕ Playlist** (junto al título de la grilla) agrega la
  presentación entera a la playlist: las flechas la recorren y siguen con lo
  que viene después. Entran las imágenes y los videos de la pestaña, no los de
  YouTube ni las páginas web, y las imágenes no avanzan solas.
- **Proyectar otra ventana de Windows.** El botón **🖥 Ventana** de la barra de
  medios abre la lista de ventanas abiertas (con ícono, título y programa);
  elegís una y se ve en la pantalla **en vivo**, tal como está: un PowerPoint
  en presentación, un reproductor, una planilla. También se puede usar el
  selector de Windows (que deja elegir una pantalla entera) y tildar **Mostrar
  el puntero**. No sale a internet: es la captura de ventanas del propio
  Windows. Se apaga con F4 o al proyectar otro fondo; si cerrás la ventana se
  deja de proyectar sola, y si la minimizás queda el último cuadro. En
  «bajo rendimiento» baja a 12 cuadros por segundo. Con **💾 Guardar en
  medios** la ventana queda como una tarjeta 🖥 (guarda el nombre del programa
  y de la ventana, no una imagen): un clic la busca y la proyecta, y también se
  puede poner en la playlist.

- **Secciones que se achican a íconos.** Además de canciones/Biblia y la
  playlist, ahora se pueden achicar la **barra de medios** (queda sólo su
  cabecera, con los botones 📥 ▶ 🌐 🔗 🎲: un clic en «MEDIOS» la achica y la
  vuelve a agrandar) y las **herramientas de texto** (Texto rápido, Aviso,
  Resaltar, Notas, Música y Tiempos quedan como una tira de íconos; un clic en
  cualquiera la abre en esa pestaña). Y **canciones, Biblia y playlist** pueden
  quedar juntas como una columna angosta de íconos (botón ⇤ del panel, o menú
  **Ver**): cada ícono abre lo suyo. El menú **Ver** tiene una entrada para
  cada una, y «Volver al diseño original» las apaga. Se recuerda para la
  próxima reunión, y el alto o ancho que habías elegido no se pierde al
  achicar.
- **Elegir qué Biblias instalar.** El instalador tiene ahora una página
  **Biblias** con una casilla por versión: **Reina-Valera 1909** (viene
  tildada), **Biblia libre para el mundo** y **Versión Biblia Libre**. Son las
  únicas que se pueden repartir con el instalador: la primera es de dominio
  público, la segunda también (es un borrador que sus autores siguen
  revisando) y la tercera tiene una licencia libre (CC BY-SA 4.0). Las que
  tienen derechos (RVR1960, NVI, NTV…) no vienen, porque el instalador se
  publica para cualquiera. Al abrir el programa, trae a su biblioteca las que
  todavía no tiene y avisa abajo cuáles; si después borrás una, no vuelve a
  aparecer. El origen y la licencia de cada una están en
  `installer/bibles/LICENCIAS.md`.

- **Paquetes de Biblias (`.ecbiblias`).** Para llevar a otra computadora tuya
  las Biblias con derechos que ya tenés: en **Archivo → Exportar paquete de
  Biblias…** (o con clic derecho sobre una versión) marcás las versiones y se
  guarda un solo archivo; en la otra, **Archivo → Importar paquete de
  Biblias…** muestra lo que trae, con las que ya tenés destildadas y marcadas
  «ya está en la biblioteca», e importa las que elijas. El instalador tiene
  además una página opcional **Paquete de Biblias propio**: si elegís el
  archivo ahí, la primera vez que abrís el programa aparece la ventana para
  importarlo (una sola vez). Los paquetes son para usar en tus propias
  computadoras: las versiones con derechos no se pueden repartir.

- **Tipografía para cada recuadro del escenario.** En el diseñador de
  escenarios, cada recuadro (las notas del predicador, la hora, un texto fijo…)
  puede tener su propia letra: se elige de la lista de las que tiene la
  computadora, o se escribe el nombre. Si no se elige ninguna, queda la de
  siempre, así que los escenarios que ya estaban guardados se ven igual.

- **Tipografía para el texto rápido.** En la pestaña **Texto rápido** hay una
  lista **Tipografía** arriba del cuadro: se elige la letra de las que tiene la
  computadora, o se escribe el nombre. La vista previa cambia al instante, y se
  recuerda aunque se cierre el programa. Si queda vacía, sigue la letra del
  tema de las canciones, como antes.

- **Actualizaciones automáticas.** Una vez por día, a los pocos segundos de
  abrir, EcclesiaCast mira en GitHub si hay una versión nueva y, si la hay,
  abre un aviso con lo que trae, en castellano, y tres botones: **Actualizar
  ahora**, **Más tarde** y **Saltar esta versión**. Actualizar baja el
  instalador (con barra de avance y **Cancelar**), comprueba que el archivo sea
  el publicado (tamaño, SHA-256 y, cuando el programa instalado esté firmado,
  que lo firme el mismo editor), guarda una copia de la biblioteca en
  `respaldos` (las tres últimas), instala y vuelve a abrir el programa solo.
  **Nunca interrumpe mientras se proyecta**: con la salida prendida sólo
  aparece un botón **⬆ Versión nueva** arriba a la derecha, y si se actualiza
  con la salida prendida antes pregunta. Si no hay internet, o GitHub no
  contesta, no avisa de nada. En **Ayuda** están **Buscar actualizaciones…**
  (busca en el momento) y **Buscar actualizaciones al abrir** (se apaga ahí).
  Las notas de cada versión publicada pasan a ser esta lista de cambios, en
  castellano, en lugar de los títulos de los commits.

- **Proyectar una página web.** El botón **🌐 Web** de la barra de medios abre
  una página en el cuadro central, en lugar de las diapositivas: la usás con el
  mouse y el teclado y la mandás a la pantalla de dos formas. **📷 Proyectar
  foto** saca una foto de lo que se ve y la proyecta en primer plano (si
  seguís navegando, la pantalla no cambia hasta la próxima foto); **🔴 En vivo**
  muestra la página en movimiento (desplazarse, videos, animaciones), con una
  flecha donde tenés el mouse si dejás tildado **Mostrar el puntero**. La
  página se arma como en una pantalla de 1920×1080, así que lo que ves es lo
  que se proyecta; **A−** y **A+** agrandan la letra. Se escribe una dirección
  o lo que quieras buscar en Google. Al cerrar el panel, una página en vivo
  sigue proyectándose. **Guardar en medios** guarda la foto o, sin foto, la
  página como tarjeta 🌐 (un clic la abre; se puede agregar a la playlist, y el
  doble clic la pone en vivo). Mientras escribís en la página, los atajos del
  programa no se disparan, salvo F1 a F4 (negro, limpiar, logo, quitar fondo).
  La página sigue viéndose en la pantalla aunque otro programa tape la ventana
  del operador o la minimices. Las fotos de más de 7 días se borran solas.

- **En Propiedades, el cuadro de vista previa muestra también lo que queda
  afuera de la pantalla.** Se ve la imagen entera, con la parte de afuera
  **oscurecida** y la pantalla marcada con un **rectángulo blanco**, como en un
  recortador de fotos: así se ve qué parte de una página se va a proyectar y
  hacia dónde correrla. Arrastrar y la ruedita funcionan igual (el movimiento
  del mouse ahora se mide contra el rectángulo de la pantalla). El cuadro es un
  poco más alto para tener lugar alrededor, y la ventana crece unos 15 píxeles.
  En las tarjetas de la barra de medios sigue viéndose sólo lo que sale en
  pantalla.

- **Selección múltiple en la barra de medios, como en el Explorador.**
  **Ctrl+clic** suma o saca una tarjeta, **Mayús+clic** elige el tramo desde la
  última, **Ctrl+A** elige todas las de la pestaña, **Esc** limpia la selección
  (cuando hay varias) y arrastrar un **rectángulo** en el espacio vacío elige
  las que toca. Con varias elegidas: **Supr** (o clic derecho → **Quitar de la
  biblioteca**) las quita todas con **una sola** pregunta que dice cuántas;
  **arrastrar** una de ellas a otra pestaña las mueve a todas; arrastrarlas a
  la playlist las agrega todas, en su orden; y clic derecho → **Mover a la
  pestaña…** deja elegir la pestaña de una lista. Un clic común sigue
  proyectando y deja elegida sólo esa tarjeta. Las elegidas se ven con borde y
  fondo azul.

- **La miniatura muestra cómo se va a ver en pantalla.** En **Propiedades del
  medio**, arriba a la derecha, hay un cuadro 16:9 que dibuja la imagen (o el
  póster del video) igual que la salida: con su escala, zoom, corrimiento,
  tamaño fijo, color de relleno, brillo, tinte y desenfoque. Se actualiza **en
  vivo** al cambiar cualquier campo, antes de guardar. Se puede **arrastrar** el
  cuadro para correr la imagen y usar la **ruedita** para el zoom; los números
  de «Zoom» y «Correr» se mueven juntos. Las tarjetas de la barra de medios
  usan el mismo dibujo (sin desenfoque), así que después de guardar se ve el
  encuadre: por ejemplo, una página de PDF con «Ajustar» aparece con sus
  barras.

- **«Selah» y los títulos de los Salmos, a elección.** En el editor de temas,
  en un tema de Biblia, hay una casilla nueva: **Mostrar «Selah» y los títulos
  de los Salmos**. Viene **destildada**: «Selah» no se proyecta y tampoco el
  título que algunos archivos traen como versículo 0 de cada Salmo («Al músico
  principal…»). Tildándola vuelven. Vale en las diapositivas y en los
  resultados de búsqueda. Si un archivo trae el título pegado al principio del
  versículo 1 no hay forma segura de separarlo: se corrige a mano con
  «Corregir el texto…». Ninguna de las Biblias instaladas hoy trae «Selah» ni
  títulos, así que por ahora no se nota; está para las que se agreguen.

- **Corregir el texto de un versículo.** Clic derecho sobre un versículo de la
  grilla → **Corregir el texto…**. Se abre una ventanita con el texto tal como
  está guardado en la Biblia (de la versión principal; con dos versiones, la
  ventanita avisa que se corrige la principal), se arregla y se guarda: queda
  en la Biblia para siempre, no sólo en esa diapositiva, y si el versículo
  está en pantalla se actualiza solo. Si la versión cuenta varios versículos
  como un solo párrafo (la TLA), la corrección va a todos los de la
  diapositiva. Si dos versículos de la misma diapositiva tienen textos
  distintos en la versión principal, avisa y pide corregirlos de a uno.

- **Lista de audios en la pestaña Música.** Además de los botones para
  Spotify, la pestaña tiene ahora una **lista de audios propia**: se agregan
  archivos mp3, wav, m4a, ogg o flac (con ➕ o arrastrándolos desde el
  Explorador), se **ordenan** (↑ ↓, o A–Z por nombre) y se reproducen con su
  propia barra: reproducir y pausa, anterior y siguiente, adelantar y atrasar
  10 segundos, una barra para moverse dentro del audio y un **volumen propio**
  que no toca el del video ni el de Windows. Doble clic (o Enter) reproduce el
  audio elegido; al terminar, sigue solo con el que viene después, y en el
  último se detiene. La lista queda guardada: sólo se anota dónde está cada
  archivo, que no se mueve ni se copia. Va con su propio reproductor, aparte
  del que pasa los videos de fondo.

- **Videos pesados: el programa pregunta y se hace la copia al momento.** Al
  agregar un video de 4K o de 50 cuadros por segundo o más (con el botón
  Agregar o arrastrándolo), aparece «Este video es muy pesado para proyectar.
  ¿Hago una copia más liviana?». Con **Sí** se hace en el momento, con una
  barra que avanza y un botón Cancelar; con **No** el video se proyecta
  tal cual y el programa se acuerda, así que no hace la copia a escondidas.
  Si agregás varios, una casilla permite contestar lo mismo para todos. La
  biblioteca guarda los dos: el original, que no se toca, y la copia liviana,
  que es la que se proyecta. Para hacerla después, clic derecho sobre el
  video → **Hacer una copia liviana…**. Además, en el panel LIVE los videos
  pesados se ven con su imagen quieta (como los de YouTube), porque dibujar
  ahí un segundo video en movimiento le quitaba trabajo al proyector.

- **Pantalla de carga y «Acerca de» la primera vez.** Al abrir el programa
  aparece una pantalla con el logo, el nombre y la versión mientras carga, y se
  va sola cuando se ve la ventana principal. La **primera vez** que se abre, se
  muestra además «Acerca de EcclesiaCast», con el botón **Aceptar** en vez de
  Cerrar; no vuelve a salir (se sigue abriendo cuando quieras desde Ayuda).
  «Acerca de» ahora lleva el logo.

- **Pestaña Tiempos** (abajo a la derecha, junto a Música): las **secciones
  de la reunión** con su tiempo (viene con Alabanza 25 minutos, Anuncios 5 y
  Prédica 35; se cambian, se agregan, se quitan y se ordenan, y quedan
  guardadas). Se elige una y se maneja con Iniciar, Pausar, volver a cero,
  +1 y −1 minuto y Siguiente sección (si estaba corriendo, la que sigue
  arranca sola). Si se pasa del tiempo sigue contando en negativo y se pone
  en rojo. Abajo, el **cronómetro** del servicio con Iniciar, Pausar y Cero;
  es el mismo que muestra el recuadro «Cronómetro» del escenario (que sigue
  arrancando solo al abrir el programa). En el escenario hay un recuadro nuevo,
  **Temporizador**, que muestra lo que le queda a la sección en marcha, en
  rojo al pasarse; se agrega desde el editor de escenarios. El recuadro que
  antes se llamaba «Temporizador (cuenta regresiva)» pasó a llamarse **Cuenta
  regresiva (antes de empezar)** y sigue mostrando la cuenta que se proyecta.

- **Arrastrar medios dentro del programa.** Un medio de la barra se puede
  soltar en la playlist, en el lugar exacto y con la misma línea azul que las
  canciones, o en otra pestaña de la lista de la izquierda, a la que se muda
  (la pestaña de destino se resalta mientras pasás por encima). El archivo no
  se mueve de lugar.

- **Un vigía anota los cuelgues en el registro.** Si la ventana del operador
  se queda sin responder más de 2 segundos, el registro (carpeta «logs»)
  deja una línea «Ventana congelada» con las últimas cosas que hizo el
  programa (proyectar una diapositiva, poner un fondo, abrir un capítulo,
  guardar una canción o la playlist…, cada una con su nombre), y otra
  «Ventana descongelada» con cuánto duró. No cuesta nada mientras todo anda
  bien.

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

- **Pasajes en la playlist**: el botón «➕ Playlist» de la Biblia abre una
  cajita con una cita ya escrita: el tramo del buscador («juan 8 5-10»), si
  no el versículo que está en pantalla, si no el capítulo. Ahí se puede
  escribir otra («juan 8 del 5 al 10»), ver el texto del pasaje con la
  versión, y tocar «Agregar» (o Enter; Esc cierra). Si la cita no existe en
  esa versión, la cajita lo dice y no deja agregar. Con clic derecho sobre un
  versículo se agrega ese, o todo lo que va desde el que está en pantalla
  hasta él, como antes.

- **Editar el nombre y la abreviatura de una Biblia** (clic derecho sobre la
  versión). La abreviatura nueva sale en la referencia y en los pasajes que
  ya estaban en las playlists.

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

- **Con el logo o el negro puesto, el video de fondo se pausa.** Antes seguía
  decodificándose y dibujándose detrás sin que nadie lo viera (con Black, hasta
  sonaba). Ahora el video de fondo, el video que rellena los bordes y la página
  web o ventana en vivo se detienen mientras el logo o el negro están en
  pantalla, y al sacarlos siguen donde estaban. Usa menos procesador y placa de
  video durante esos momentos.
- **El error de una imagen de Google se nota.** Si no se pudo agregar, la barra
  de abajo parpadea en rojo y el mensaje se destaca unos segundos.
- **La fecha del evento se elige en un calendario.** Ya no se escribe a mano:
  un selector de fecha en castellano, con ✕ para sacarla.
- **Se sacaron el brillo y el tinte de Propiedades.** Ya no se dibujan; un
  medio que los tenía se ve sin ellos.
- **El tiempo de una imagen se ve en la barra de reproducción.** «Cuánto tiempo
  se ve» vale ahora para cualquier imagen (antes sólo en primer plano), y
  mientras está en pantalla la misma barra de los videos muestra el tiempo
  que pasa: se puede pausar, adelantar, atrasar, arrastrar y volver al
  principio.
- **Las Biblias del instalador se traen antes de abrir la ventana.** La
  primera vez que abrís el programa después de instalarlo, mientras se ve la
  pantalla de carga, se copian a tu biblioteca las Biblias que elegiste (unos
  dos segundos por cada una en una notebook modesta). Antes se copiaban con la
  ventana ya abierta, y si tocabas algo en esos segundos el programa se
  quedaba esperando. Las veces siguientes no se nota: no queda nada por traer.

- **La letra de una canción ya no se dibuja encima de los cuadros de texto
  extra.** Si una diapositiva tiene un cuadro que ocupa al menos la mitad del
  ancho de la letra (un título, una traducción a lo ancho), la letra se achica
  y se acomoda en el espacio que queda, con un respiro de 12 píxeles. Los
  cuadros angostos de una esquina no le sacan nada. **Esto cambia cómo se ven
  las canciones que ya tengan un cuadro a lo ancho**: la letra puede quedar
  algo más chica que antes. Revisalas en el diseñador si alguna no te gusta.

- **Las miniaturas de las imágenes y de las páginas de un documento se dibujan
  con la forma de la imagen**, con 320 píxeles del lado largo (una página
  vertical queda vertical; una imagen 16:9, 320×180 como siempre). Antes las
  armaba Windows con el tamaño que le salía, y a veces no coincidía con lo que
  se proyecta. Así la tarjeta puede encuadrar la imagen igual que la pantalla.
  Las miniaturas que ya existían se rehacen **solas, una sola vez**, en
  segundo plano, al abrir el programa. Las de video no cambian.

- **Las páginas de un PDF o un PowerPoint entran con «Ajustar»**: se ven
  enteras, con barras a los costados si hace falta. Antes llenaban la
  pantalla y una página vertical quedaba recortada a una franja.
- **La ventana de Propiedades de un medio es más ancha y más baja**: dos
  columnas (a la izquierda qué es y cómo se encuadra, a la derecha cómo se
  ve y qué sigue), en vez de una sola que no entraba en una notebook.
- **Las notas de la edición impresa ya no se proyectan.** Algunos archivos de
  Biblias traen pegadas al texto cosas del libro de papel. Ahora se sacan al
  mostrar: las **rayas** (`----------`) y la nota entre corchetes que viene
  después (por ejemplo, la de Juan 7:52 y la de Marcos 16:8 en la NTV), los
  **títulos cortos entre corchetes** como «[Final largo de Marcos]» y los
  **números de versículo que la TLA deja en el medio** («43b (44) Sin
  embargo…»). Un párrafo entre corchetes que es texto de la Biblia, como el de
  1 Samuel 10:27 en la NTV, se queda. Vale en las diapositivas, en los
  resultados de búsqueda (Ctrl+K y el `?`) y en la cajita de «➕ Playlist», y
  por lo tanto también en el escenario, el control remoto y la salida web. Lo
  guardado no cambia, así que la búsqueda y la exportación siguen igual. En
  la biblioteca de prueba cambia 3 versículos de la NTV y 3 de la TLA; la
  RVR1960 y la NVI quedan como estaban.

- **LIVE, PREVIEW y ESCENARIO en un solo recuadro.** Arriba a la derecha, en
  vez de dos recuadros uno abajo del otro, ahora hay uno solo con tres
  pestañas: **LIVE** (lo que se proyecta), **PREVIEW** (la diapositiva que
  sigue) y **ESCENARIO** (lo que ven los músicos). Así queda más lugar para
  Texto rápido, Tiempos y las demás pestañas de abajo. Si hay un video en
  pantalla, sus botones (pausa, adelantar, la barra) se ven también en la
  pestaña PREVIEW. Para volver a los dos recuadros: **Ver → Separar el PREVIEW
  del LIVE**; el programa se acuerda de lo que elijas.

- **Menús, listas desplegables y globitos oscuros.** Los menús de arriba, los de
  clic derecho, las listas desplegables (la pantalla de salida, la playlist, los
  tipos de letra…) y los globitos que explican cada botón salían con el blanco
  de Windows, que desentonaba con todo lo demás. Ahora son oscuros, con la
  misma paleta del programa: lo que está bajo el mouse se marca en azul, lo
  tildado lleva una tilde clara y el valor actual de una lista se ve
  resaltado. Los globitos largos ya no se cortan: bajan de renglón.

- **Pestañas de medios: nombre, regla y borrado.** Con clic derecho sobre una
  pestaña: **Cambiar nombre** (se pueden usar emojis; sus medios y sus
  opciones la acompañan), **Configurar…** y **Eliminar** (avisa que los medios
  ya no se van a ver en el programa y que los archivos no se borran). En
  Configurar se elige que todo lo de la pestaña vaya como **fondo**, como
  **primer plano** o **como diga cada medio**; para primer plano hay una
  casilla «Al terminar, mostrar el logo». El programa ahora se acuerda de las
  pestañas por sí mismo: una pestaña vacía, o en otro orden, sigue ahí al
  volver a abrirlo.

- **Qué sigue después de un medio**, en Propiedades. Un video puede repetirse,
  quedar quieto, mostrar el logo o **seguir con otro medio de la biblioteca**.
  Una imagen en primer plano puede tener **segundos**: pasados, sale de la
  pantalla, muestra el logo o sigue con otro medio (por ejemplo, un aviso que
  se ve 10 segundos y deja paso al siguiente). Con 0 segundos se queda hasta
  que cambies.

- **Se sacó la lista inteligente** (botón ✨, ventana y menú). Casi nadie la
  usaba y complicaba la playlist. Las listas inteligentes que hubiera se borran
  solas al abrir esta versión (no guardaban canciones, sólo la regla); las
  playlists armadas a mano no se tocan.

- **Canción repetida: el programa pregunta.** Al guardar una canción nueva o
  traerla (escrita desde cero, de un archivo, de ProPresenter o de internet)
  con un título que ya existe (sin distinguir mayúsculas ni acentos), una
  ventanita ofrece **Guardar como «Título (1)»** (con el primer número libre;
  las dos quedan), **Reemplazar** la que ya estaba (conserva su tema) o
  **Descartar** la nueva. Si se traen varias juntas, una casilla «Hacer lo
  mismo con las demás» evita responder una por una. Antes los archivos y
  ProPresenter salteaban en silencio las repetidas, y desde cero se guardaba
  una segunda igual. En la ventana de ProPresenter ya no está la casilla
  «Omitir las que ya están»: la pregunta la reemplaza.

- **Resaltar: varias cosas, y sólo la que elegís.** Al marcar en el LIVE
  (clic o arrastre) se resalta sólo esa vez en el texto, no todas las veces que
  aparece la palabra. Con **Shift** se suma otra marca sin perder las
  anteriores, y marcar de nuevo algo resaltado lo saca. En la pestaña Resaltar
  se pueden escribir varias palabras o frases separadas por **;** («gracia;
  fe»), y esas sí se pintan en todas las veces que aparecen. Al cambiar de
  diapositiva se borra todo. El resaltado también sale en la página para OBS y
  vMix.

- **Las versiones de la Biblia, en una fila.** Cada versión es un botón con su
  abreviatura (RVR1960, NVI, NTV, TLA): un clic usa sólo esa; Ctrl+clic suma
  o saca una segunda, que se proyecta debajo de la primera (si ya había dos,
  la nueva reemplaza a la segunda y la primera se queda; debajo de la fila lo
  dice «Ctrl+clic: sumar una segunda versión»). Las activas se ven
  encendidas, la primera llena y la segunda con borde. El botón ▸ abre la
  lista con los nombres completos y el programa se acuerda si la dejaste
  abierta. Con clic derecho sobre una versión: editar nombre y abreviatura,
  eliminar y **exportar a un archivo** que el mismo programa vuelve a importar
  (📥), para llevarla a otra computadora. Los botones ✏️ y 🗑 sobraron y se
  fueron.

- **Videos de YouTube más claros en la barra de medios.** Al agregar uno (por
  el navegador o pegando el link) el programa pregunta si se reproduce por
  internet o si se descarga para usarlo sin conexión. Los que se reproducen por
  internet llevan una marca «🌐 internet» en la tarjeta, y los descargados no.
  La descarga queda en la pestaña de medios que estaba elegida (ya no en
  «Descargados»), y «Descargar para uso local» aparece sólo en el menú de los
  videos de YouTube.

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

- **Textos de ejemplo libres.** Las vistas previas del editor de temas y de
  escenarios mostraban «Grande es tu fidelidad», «Sublime gracia» y la RVC/NTV,
  que tienen derechos de autor. Ahora muestran «Santo, santo, santo» y Juan 3:16
  en la Reina-Valera 1909 y la Biblia libre para el mundo.
- **La página del celular decía «Content»** (en inglés) arriba a la derecha:
  ahora dice «En pantalla».
- **El ícono de cada ítem de la playlist se veía negro** sobre el fondo oscuro:
  ahora es blanco.

- **El cambio de una imagen a otra es un fundido limpio.** Grabando la salida
  cuadro por cuadro se vio que el relleno desenfocado de la imagen vieja
  desaparecía de golpe al empezar el cambio (bordes negros) y el de la nueva
  aparecía de golpe al terminar. Ahora la imagen vieja queda entera, con su
  relleno, mientras la nueva se funde encima con el suyo.
- **El cambio de una imagen a otra ya no se demora.** Se midió: la imagen nueva
  se leía en el momento del cambio y bloqueaba la ventana (120 ms una portada
  de 1350 px, 60 ms una página 4K), la agrandaba al leerla aunque fuera más
  chica que la pantalla, y el relleno desenfocado se dibujaba en vivo (250 ms,
  y de nuevo en cada paso del fundido). Ahora se lee en segundo plano (la
  imagen vieja sigue en pantalla hasta que la nueva está lista), se empieza a
  leer cuando apoyás el mouse sobre la tarjeta, no se agranda de más, y el
  relleno desenfocado es una imagen chica ya desenfocada (9 ms).
- **El menú del teclado se ve en los tooltips.** Con el diseño avanzado las
  teclas F1, F2, F3, F4, Esc y Ctrl+Enter ya no están escritas en los botones;
  ahora las dice el globito de cada uno.
- **La flecha pasa al versículo siguiente después de buscar uno.** Escribías
  «juan 3 16», Enter, y al apretar → el cursor se movía dentro de la caja en
  lugar de ir al 17. Ahora, mientras no escribas nada más ni hagas clic en la
  caja, las flechas ← → siguen moviendo las diapositivas, como en BibleShow.
  Además, si marcabas un versículo sin proyectarlo y apretabas →, saltaba al
  capítulo anterior: ahora proyecta el versículo marcado (y ← el anterior).
- **El sonido vuelve solo si cambiás la salida de audio de Windows.** Al
  cambiar los parlantes por los auriculares (o un parlante Bluetooth) y
  volver, la música y los videos quedaban mudos hasta cerrar y abrir el
  programa. Ahora el programa se entera del cambio: lo que está sonando se
  vuelve a abrir en la salida nueva, en el mismo lugar, y lo que se ponga
  después ya arranca bien. Por las dudas, en **Ver → Reiniciar el sonido**
  hay un botón que hace lo mismo a mano. Los videos de YouTube siguen la
  salida de Windows por su cuenta.

- **Al elegir otro medio ya no hay un salto a negro.** Pasar de una imagen a
  otra, de un video a una imagen o de una imagen a un video hacía un
  parpadeo negro: la imagen vieja desaparecía de golpe y la nueva subía desde
  la nada. Ahora la anterior se queda en pantalla (la imagen, o el último
  cuadro del video) y la nueva se funde por encima. Con **bajo rendimiento**
  se mantiene lo anterior en pantalla pero el cambio es directo, sin fundido.
  Hacia un video de YouTube todavía hay un instante negro mientras carga.

- **El logo y el negro tapan también a un video de YouTube.** Con un YouTube
  sin descargar en pantalla, F3 (logo) y F1 (negro) no lo tapaban: había que
  cambiar a otro medio para que el logo se viera. Ahora, mientras el logo o el
  negro están en pantalla, el YouTube se esconde y se pausa (no suena por
  detrás), y al volver sigue desde donde estaba.

- **El aviso de cada tarjeta de la grilla dice lo que hace.** En la Biblia el
  globo de ayuda decía «Arrastrar: cambiarla de lugar», pero los versículos y
  los capítulos no se pueden arrastrar. Ahora cada tarjeta muestra lo suyo: el
  versículo dice cómo agregarlo a la playlist o corregirlo, y las tarjetas
  **ANTERIOR** y **SIGUIENTE** dicen a qué capítulo llevan. Las canciones
  siguen diciendo que se pueden arrastrar.

- **La imagen del tema no tapa al medio elegido.** Si el tema tenía una imagen
  de fondo (como el de la Biblia), esa imagen quedaba encima del video o de la
  foto que el operador había puesto, y se veía la del tema. Ahora, mientras hay
  un medio en pantalla, gana el medio; la imagen del tema vuelve cuando se
  quita el medio.

- **Con «Clear (F2)» se van también los cuadros de texto extra.** Al quitar la
  letra de la pantalla se iba el texto principal, pero los cuadros que se le
  agregaron a una diapositiva quedaban a la vista. Ahora se van y vuelven junto
  con la letra.

- **Varios cuadros de texto en una diapositiva del diseñador.** Con más de un
  cuadro sólo se podía editar el que elegías en la lista de la derecha, y el
  marco grande de la letra quedaba encima de los cuadros chicos sin dejar
  tocarlos. Ahora un clic (o doble clic para escribir) sobre cualquier cuadro
  lo elige, y si se superponen gana el más chico.

- **El zoom y el corrimiento de «Rellenar» trabajan sobre la imagen entera.**
  Antes, «Rellenar» recortaba lo que sobraba **primero** y el zoom y el
  corrimiento movían ese pedazo ya recortado: con una página vertical (la tapa
  de un manual) no se podía mostrar la parte de abajo, y al alejar el zoom el
  recorte se achicaba con negro alrededor en vez de mostrar más de la página.
  Ahora la imagen se dibuja entera y lo único que recorta es el borde de la
  pantalla: alejando el zoom se ve más de la imagen (con el color de relleno
  donde no llega), y corriéndola se llega de arriba a abajo. El zoom ahora gira
  alrededor del **centro de la pantalla**. «Ajustar» y «Estirar» no cambiaron.
  Vale para la salida, el LIVE y el PREVIEW, los videos y las miniaturas. Para
  poder llegar al final de una página alta, **«Correr» ahora admite hasta
  ±1000 %** de la pantalla (antes ±100 %). Con un tamaño fijo, el cuadro se
  queda quieto y la imagen se mueve y se agranda detrás.

- **«Rellenar» deja la imagen centrada.** Antes quedaba pegada arriba a la
  izquierda: una foto vertical mostraba sólo su parte de arriba y una muy
  ancha, sólo su parte izquierda. Ahora lo que sobra se recorta parejo de los
  dos lados, como en los demás programas. Las imágenes 16:9 se ven igual; si
  alguna otra ya tenía un corrimiento puesto, puede que haya que acomodarla.

- **Las páginas de PDF y PowerPoint se ven nítidas con zoom.** Se dibujaban a
  1920 píxeles, justo el tamaño de la pantalla, y al hacer zoom en el Encuadre
  se agrandaba esa copia y se pixelaba. Ahora las páginas nuevas se dibujan al
  **doble** (4K: 3840 píxeles del lado largo, sea la página vertical o
  apaisada), y al proyectar una imagen con zoom se lee al tamaño que de verdad
  hace falta (el ancho de la pantalla por el zoom, sin pasar del tamaño del
  archivo ni de 4096). Sin zoom todo se lee igual que antes. Las páginas que ya
  estaban importadas siguen como estaban: clic derecho sobre una página →
  **Volver a importar en alta calidad…** dibuja de nuevo todo ese documento
  (usa el PDF que el programa guarda desde ahora; para páginas viejas pide
  elegir el original una vez). Cada documento guarda ahora una copia de su PDF
  junto a sus páginas, y se borra con la última página.

- **Cambiar la escala o el encuadre de un medio que está en pantalla no
  llegaba a la pantalla principal**: el LIVE lo mostraba, pero la salida
  seguía igual hasta volver a elegirlo. Ahora se ve en el momento, y un video
  no se reinicia por eso.
- **La barra azul al reordenar diapositivas ya no se escapa.** Si se movía el
  mouse rápido, la barra (y la copia de la tarjeta) aparecían encima de la
  barra de medios. Ahora se dibujan sólo dentro del panel de diapositivas, y
  la barra se esconde si el mouse está afuera.

- **LIVE y PREVIEW ya no se deforman al cambiar el ancho del panel
  derecho.** Los dos cuadros mantienen siempre la proporción de la pantalla
  elegida en «Salida» (por ejemplo 16:9). Si el panel se ensancha, el cuadro
  no crece hacia abajo: queda del alto que tenía, centrado, con espacio a los
  costados; si se angosta, se achica entero.

- **El PREVIEW ya no muestra la letra encima del anuncio.** Con un medio en
  primer plano en pantalla, el cuadro «Siguiente» dibujaba la próxima
  diapositiva sobre la imagen del anuncio, pero al proyectarla el anuncio
  sale. Ahora el PREVIEW muestra la letra sobre el fondo que va a quedar (el
  fondo normal, o ninguno).

- **Las flechas ya no se pasan solas al ítem siguiente de la playlist.** Con
  la tecla apretada, las flechas llegaban al final del pasaje, ponían de fondo
  el video que seguía en la playlist y saltaban al ítem de después, todo en
  menos de un segundo. Ahora se frenan en el final (o el principio) de cada
  ítem y la barra de estado avisa «Fin del pasaje: → otra vez pasa a «…»».
  Sólo una pulsación nueva (no la repetición de la tecla apretada) pasa al
  siguiente ítem, y la repetición de esa misma pulsación se ignora.

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
- **Descargar de YouTube ahora anda solo, sin instalar nada.** Fallaba con
  «Could not copy Chrome cookie database» y con el error 403 porque el
  programa de descargas (yt-dlp) de la PC era viejo. Ahora EcclesiaCast baja
  y mantiene su propio yt-dlp y el componente que YouTube exige hoy (unos 60
  MB, con una barra de progreso la primera vez), lo actualiza cada tres días y, si YouTube lo rechaza, lo actualiza y
  vuelve a intentar. La sesión de YouTube de la iglesia se la pide al
  navegador del programa, en lugar de leer el archivo que Windows tiene
  bloqueado. Si igual falla, el aviso lo explica en castellano y recuerda que
  el video se puede proyectar por internet.
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
