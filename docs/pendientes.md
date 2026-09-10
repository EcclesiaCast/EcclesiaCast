# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

## 1. Para llegar a la v1.0.0

- [ ] **Correr la [guía de pruebas](pruebas-beta.md) en la PC del proyector.**
      Es el bloqueante real: casi nada de la beta.3 se probó con proyector de
      verdad.
- [ ] Mergear `beta3` a `main` una vez que la prueba salga bien.
- [ ] Actualizar README y CHANGELOG (hoy dicen "Beta") y taguear `v1.0.0`.
- [ ] **Firma digital del instalador.** Hoy Windows muestra "Editor
      desconocido" y hay que darle a "Más información → Ejecutar de todas
      formas". Espanta a cualquier iglesia que lo baje sin conocernos.

## 2. Funciones que faltan

- [ ] **Cuenta regresiva** antes del servicio ("empezamos en 5:00"), en la
      salida y en el escenario.
- [ ] **Notas del predicador** en la pantalla de escenario: hoy muestra la
      letra de la canción, no un guion aparte.
- [ ] **Respaldo y restauración** de la biblioteca desde el programa. Hoy es
      copiar `%APPDATA%\EcclesiaCast\ecclesiacast.db` a mano.
- [ ] **Transiciones** entre diapositivas más allá del fundido actual.
- [ ] **Límite de intentos del PIN** en el control remoto. Hoy se puede probar
      PIN tras PIN sin freno; son 10.000 combinaciones.
- [ ] Múltiples cuadros de texto por diapositiva (diferido desde el sprint 4).
- [ ] Recorte de entrada/salida de video y efectos de color (diferido desde el
      sprint 5).
- [ ] Playlists inteligentes (diferido desde el sprint 5).

## 3. Ideas tomadas de Spresenter (10/9/2026)

Comparación completa contra [spresenter.com](https://spresenter.com) hecha el
10/9/2026. Las tres que valen la pena, en orden de resultado por esfuerzo:

- [ ] **Salida web para transmisión.** Que cada salida se publique como una URL
      HTTP para pegar en el Browser Source de OBS o el Web Input de vMix, sin
      hardware extra. Es la más barata de las tres: ya tenemos el servidor HTTP
      andando para el control por celular.
- [ ] **Búsqueda global unificada.** Una sola barra que busque a la vez en
      canciones, versículos y medios, estilo Spotlight, en vez de un buscador
      por pestaña.
- [ ] **Soporte de PowerPoint (.pptx) y PDF** como contenido proyectable. Es lo
      que más se pide en una iglesia real de todo lo que ellos tienen y
      nosotros no.

Fuera de alcance a propósito (es otro producto: iglesias grandes con equipo
técnico): edge blending y warp entre proyectores, display slices para paneles
LED, sincronización de varias máquinas en red, automatización OSC/MIDI,
timeline y VFX, NDI, macOS.

## 4. Conocidos y aceptados

Cosas que ya sabemos y decidimos bancar por ahora. No son bugs nuevos.

- **Subtítulos automáticos de YouTube** se proyectan sobre el video. Vienen de
  la preferencia de la cuenta con sesión iniciada; se comprobó que no se pueden
  apagar desde el reproductor. Hay que desactivarlos en la cuenta de YouTube.
- **El panel LIVE muestra un póster fijo**, no el video en movimiento: el
  operador no ve avanzar el video ni el YouTube. Duplicar el WebView2 no es
  trivial.
- **~810 MB de RAM** con video 1080p de fondo. Es mucho para una PC de
  proyección modesta; la salida sería bajar el buffer a 720p.
- **Los nombres de playlist no se leen bien** con lector de pantalla (el combo
  expone el nombre del tipo). Cosmético, pero es accesibilidad.
- Navegando a mano el mismo capítulo de un pasaje de la playlist, el borde del
  rango dispara el salto al ítem siguiente. Solo en esa diapositiva exacta.
