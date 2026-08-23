# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [Sin publicar]

### Agregado

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
