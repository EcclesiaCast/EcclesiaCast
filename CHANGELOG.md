# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [1.0.0] — sin publicar

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
