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

La beta.5 se probó el 6/10/2026: lo que anda bien quedó así (Biblia, achicar
paneles, menú, buscar canciones en internet). Lo que salió de esa prueba y
los pedidos nuevos:

**Ventana**

- [ ] **Acerca de: una forma de colaborar** (todavía no hay a dónde mandar a
      la gente; se agrega cuando esté decidido).

**Para probar con el programa abierto**

Se probó el 7/10/2026 con una copia de la biblioteca real: Ctrl+K («echa
sobre» → Salmos 55:22 · RVR1960, que se abre en esa versión; «las cadenas de
la muerte» → *Hay libertad*), las miniaturas de Anuncios (se rehicieron
solas), el ➕ de canciones, Acerca de, el editor de escenario (mover y
agrandar con el mouse), los cuatro escenarios en el monitor y su cambio con
la pantalla prendida, el modo bajo rendimiento (el LIVE queda quieto y sin
sombra) y la pestaña Música (se ve bien). Falta:

- [ ] **Arrastrar archivos** desde el Explorador a la barra de medios.
- [ ] **Los botones de Música** con Spotify abierto.
- [ ] **El instalador**: que la casilla de bajo rendimiento deje el modo
      prendido en el primer arranque.

Descartado: importar las Biblias de BibleShow. Sus archivos `.bib` son bases
de Access protegidas con usuario y contraseña, y NTV, NVI y PDT son textos con
derechos licenciados para usar dentro de BibleShow.

Descartado: pasar a H.264 también los videos HEVC y AV1 (hoy sólo se copian
los 4K o de 60 cuadros). La PC de la iglesia (i5-10400F, 16 GB, RTX 3050)
decodifica HEVC por hardware, y el AV1 de YouTube lo resuelve el procesador
con muy poco esfuerzo. Sólo serviría en una PC sin decodificación por
hardware: ahí el HEVC cuesta 40 % menos en H.264 (medido el 4/10/2026). Si
algún día hace falta, es sumar una condición a `LightCopyPolicy`.

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
