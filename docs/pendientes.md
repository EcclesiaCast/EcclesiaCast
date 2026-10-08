# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

Hay tres tipos de cosas, cada una en su sección:

1. **Para probar**: ya está programado; falta que Alexis lo pruebe con el
   mouse, con Spotify o instalándolo, porque desde acá no se pudo.
2. **Para hacer**: cambios que todavía hay que programar.
3. **Para llegar a la v1.0.0**: lo que falta para dejar de ser beta.

## 1. Para probar (lo prueba Alexis)

Todo esto ya está en el instalador `dist\EcclesiaCast-1.0.0-beta.6-setup.exe`
(armado el 7/10/2026). El resto de la beta.6 ya se probó con una copia de la
biblioteca real: Ctrl+K, las miniaturas de Anuncios, el ➕ de canciones,
Acerca de, el editor y los escenarios en el monitor, el modo bajo
rendimiento y cómo se ve la pestaña Música.

- [ ] **Arrastrar un archivo** desde el Explorador a la barra de medios: se
      agrega a la pestaña elegida.
- [ ] **Los botones de Música** con Spotify abierto: anterior, pausa,
      siguiente y volumen.
- [ ] **Instalar la beta.6** tildando la casilla de bajo rendimiento: el
      programa tiene que arrancar con Ver → Modo PC de bajo rendimiento
      tildado.
- [ ] **Resaltar arrastrando en el LIVE**: mientras arrastrás, las palabras
      se marcan en azul sólo en el LIVE, y al soltar se resaltan en la
      pantalla. (El clic en una palabra ya se probó y anda.)
- [ ] **Descargar un video de YouTube** (clic derecho → Descargar para uso
      local): la primera vez baja solo el descargador y un componente de
      YouTube (unos 60 MB, con barra); después tiene que bajar el video sin
      errores de cookies ni 403. No se pudo probar desde acá (no se bajó nada
      de internet).
- [ ] **Flechas con la tecla apretada**: en una
      playlist con un pasaje, un medio y otro pasaje, mantené apretado → en
      el final del pasaje: tiene que frenarse y decir «Fin del pasaje: → otra
      vez pasa a…». Soltá y apretá de nuevo: pasa al siguiente. Probado sólo
      con el programa por dentro, no con un teclado de verdad.
- [ ] **Arrastrar un medio de la barra** a la playlist (línea azul en el
      lugar exacto) y a otra pestaña de la izquierda (se resalta y el medio
      se muda). Los arrastres con el mouse no se pudieron probar desde acá;
      se probaron por dentro la mudanza de pestaña, el resaltado y el lugar
      en la playlist, no el gesto en sí.
- [ ] **LIVE y PREVIEW al ensanchar el panel derecho**: los cuadros no se
      deforman, quedan centrados con el mismo alto y se achican si se angosta.
      Visto sólo en imágenes dibujadas a distintos anchos.
- [ ] **PREVIEW con un anuncio en primer plano** en pantalla: el cuadro
      «Siguiente» muestra la letra sobre el fondo que va a quedar, no sobre el
      anuncio.
- [ ] **Barra azul al reordenar diapositivas** moviendo el mouse rápido: no
      tiene que aparecer sobre la barra de medios.
- [ ] **Vigía de cuelgues**: si el programa se vuelve a congelar unos
      segundos, mirar el último archivo de la carpeta `logs` (línea «Ventana
      congelada») para ver qué estaba haciendo. Probado bloqueando a propósito
      un programa de prueba 3,5 segundos; no con un cuelgue real.
- [ ] **Versiones de la Biblia en una fila**: clic = sólo esa, Ctrl+clic =
      suma o saca la segunda (probado sólo por dentro: el Ctrl con teclado de
      verdad no). Clic derecho → Exportar a un archivo, y traerlo de vuelta
      con 📥 (el round trip está probado con pruebas, no con el diálogo de
      guardar).
- [ ] **La cajita del ➕ Playlist de la Biblia**: tiene que abrirse con el
      campo ya completado y seleccionado (para escribir encima), mostrar el
      texto y agregar con Enter o con el botón. Dibujada y probada por
      dentro; el foco del teclado al abrirse y el Esc no se pudieron ver
      sin abrir la ventana.
- [ ] **Preguntas al agregar un video de YouTube** (🔗 o ▶ YouTube): elegir
      «Reproducir por internet» o «Descargar», y ver la marca 🌐 internet en
      las tarjetas de YouTube. La descarga nueva depende de internet y del
      sitio de yt-dlp: no se bajó nada desde acá.

## 2. Para hacer

- [ ] **Acerca de: una forma de colaborar.** Todavía no hay a dónde mandar a
      la gente; se agrega cuando esté decidido.
- [ ] **Biblias (para decidir más adelante, por las licencias).** Lo
      averiguado el 7/10/2026: las versiones con derechos (RVR1960, NVI, NTV,
      TLA) no se pueden repartir con el programa ni desde un repositorio,
      aunque se avise; OpenLP y CrossWire no lo hacen por eso. Opciones:
      un botón «Agregar Biblias» con un catálogo de Biblias libres de varios
      idiomas (https://github.com/seven1m/open-bibles: en castellano RV1909,
      Biblia en Español Sencillo, Palabra de Dios para ti, Versión Biblia
      Libre); una carpeta privada `biblias` junto al instalador para las PCs
      de la iglesia; y pedirle permiso por la RVR1960 a la Sociedad Bíblica
      del Uruguay o a la American Bible Society.
- [ ] **Lo pedido al probar la beta.6** (7/10/2026): está todo en
      [plan-sonnet-2.md](plan-sonnet-2.md), en tres tandas.

## 3. Para llegar a la v1.0.0

- [ ] **Usar la beta.6 en un servicio de verdad**, en la PC del proyector, y
      anotar lo que falle o no guste (como se hizo con la beta.5).
- [ ] Actualizar README y CHANGELOG (hoy dicen "Beta") y taguear `v1.0.0`.
- [ ] **Firma digital del instalador.** Hoy Windows muestra "Editor
      desconocido" y hay que darle a "Más información → Ejecutar de todas
      formas". Espanta a cualquier iglesia que lo baje sin conocernos.

## 4. Decisiones tomadas

Descartado: importar las Biblias de BibleShow. Sus archivos `.bib` son bases
de Access protegidas con usuario y contraseña, y NTV, NVI y PDT son textos con
derechos licenciados para usar dentro de BibleShow.

Descartado: pasar a H.264 también los videos HEVC y AV1 (hoy sólo se copian
los 4K o de 60 cuadros). La PC de la iglesia (i5-10400F, 16 GB, RTX 3050)
decodifica HEVC por hardware, y el AV1 de YouTube lo resuelve el procesador
con muy poco esfuerzo. Sólo serviría en una PC sin decodificación por
hardware: ahí el HEVC cuesta 40 % menos en H.264 (medido el 4/10/2026). Si
algún día hace falta, es sumar una condición a `LightCopyPolicy`.

## 5. Ideas tomadas de Spresenter (10/9/2026)

Comparación completa contra [spresenter.com](https://spresenter.com) hecha el
10/9/2026. Las tres que valían la pena están hechas: salida web para OBS y
vMix, buscador único y soporte de PDF y PowerPoint.

Fuera de alcance a propósito (es otro producto: iglesias grandes con equipo
técnico): edge blending y warp entre proyectores, display slices para paneles
LED, sincronización de varias máquinas en red, automatización OSC/MIDI,
timeline y VFX, NDI, macOS.

## 6. Conocidos y aceptados

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
