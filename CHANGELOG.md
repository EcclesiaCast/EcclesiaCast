# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [Sin publicar]

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
  doble clic sobre la diapositiva.
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

- El panel LIVE del operador muestra el video de fondo **en movimiento**, no
  una foto fija: dibuja el mismo cuadro que está en el proyector, sin
  decodificar nada dos veces (+32 MB y la misma CPU). Los videos de YouTube
  siguen mostrándose como póster.
- El video de fondo usa casi 100 MB menos de memoria (742 → 649 MB con un
  1080p): el colchón de lectura de VLC pasó de segundo y medio a medio
  segundo, que para un archivo del disco de la misma PC alcanza.
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
