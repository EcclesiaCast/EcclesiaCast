# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

## 1. Para llegar a la v1.0.0

- [ ] **Correr la [guía de pruebas](pruebas-beta.md) en la PC del proyector.**
      Es el bloqueante real: nada de la beta.4 se probó con proyector de
      verdad. El instalador ya está armado en
      `dist\EcclesiaCast-1.0.0-beta.4-setup.exe` (rearmado el 11/9/2026, con
      la revisión de código de las nueve funciones nuevas ya adentro).
      También está publicado como
      [pre-release en GitHub](https://github.com/EcclesiaCast/EcclesiaCast/releases/tag/v1.0.0-beta.4),
      armado por el workflow en una máquina limpia: si la prueba destapa algo,
      se arregla y sale una beta.5.
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
10/9/2026. Las tres que valían la pena están hechas: salida web para OBS y
vMix, buscador único y soporte de PDF y PowerPoint.

Fuera de alcance a propósito (es otro producto: iglesias grandes con equipo
técnico): edge blending y warp entre proyectores, display slices para paneles
LED, sincronización de varias máquinas en red, automatización OSC/MIDI,
timeline y VFX, NDI, macOS.

## 4. Conocidos y aceptados

Cosas que ya sabemos y decidimos bancar por ahora. No son bugs nuevos.

- **Subtítulos automáticos de YouTube** se proyectan sobre el video. Vienen de
  la preferencia de la cuenta con sesión iniciada; se comprobó que no se pueden
  apagar desde el reproductor. Hay que desactivarlos en la cuenta de YouTube.
- **El panel LIVE muestra el YouTube como póster fijo.** Los videos comunes ya
  se ven moverse ahí (el preview dibuja el mismo cuadro que el proyector), pero
  para YouTube haría falta un segundo WebView2, que no es trivial ni barato.
- **~650 MB de RAM** con un video 1080p de fondo (medido el 11/9/2026: 275 MB
  con el programa abierto, 331 MB proyectando texto, 649 MB con video, estable
  a los 20 s). Sigue siendo mucho para una PC de proyección modesta, pero ya no
  hay una salida obvia:
  - El buffer **no** es el problema: decodificar a 720p ahorra 75 MB de 742 y
    se pierde nitidez, así que se descartó.
  - Bajar el cache de VLC de 1500 a 500 ms sí bajó de 742 a 649 MB, y es lo
    que está puesto. Si en la PC del proyector el video tironea, subilo.
  - El navegador de YouTube (WebView2, que son cientos de MB) **no se crea**
    salvo que proyectes un YouTube: verificado, 0 procesos con la salida
    encendida y un video local corriendo.
