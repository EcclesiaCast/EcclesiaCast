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

Vacío: todo lo que había acá se implementó el 10/9/2026 — transiciones,
encuadre y color de medios, recorte de video, cuadros de texto por diapositiva
y listas inteligentes. Está en el [CHANGELOG](../CHANGELOG.md), sin probar
todavía en un servicio real.

## 3. Ideas tomadas de Spresenter (10/9/2026)

Comparación completa contra [spresenter.com](https://spresenter.com) hecha el
10/9/2026. Las tres que valen la pena, en orden de resultado por esfuerzo:

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
