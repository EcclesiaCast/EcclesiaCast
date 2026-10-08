# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

Hay tres tipos de cosas, cada una en su sección:

1. **Para probar**: ya está programado; falta que Alexis lo pruebe con el
   mouse, con Spotify o instalándolo, porque desde acá no se pudo.
2. **Para hacer**: cambios que todavía hay que programar.
3. **Para llegar a la v1.0.0**: lo que falta para dejar de ser beta.

## 1. Para probar (lo prueba Alexis)

Todo esto está en el instalador **`dist\EcclesiaCast-1.0.0-beta.7-setup.exe`**
(armado el 8/10/2026). Nada se pudo probar con el programa abierto: se probó
por dentro, con pruebas automáticas y dibujando las pantallas.

**Antes de instalar**
- [ ] Guardar una copia de la biblioteca: Archivo → Guardar una copia de la
      biblioteca. La versión nueva actualiza la base sola (se probó sobre una
      copia de la tuya y quedó todo igual), pero por las dudas.
- [ ] Instalar tildando la casilla de **bajo rendimiento** y ver que arranque
      con Ver → Modo PC de bajo rendimiento tildado (después se puede sacar).

**Al abrir por primera vez**
- [ ] Sale la **pantalla de carga** con el logo y se va sola.
- [ ] Sale **«Acerca de»** con el botón «Aceptar», y no vuelve a salir.
- [ ] Que no se quede trabada: la primera vez rehace las miniaturas que
      faltan (en tu biblioteca faltan todas).

**Ventana**
- [ ] Menús de arriba, clic derecho (también en un cuadro de texto), listas
      desplegables y globitos: todo oscuro y legible.
- [ ] LIVE, PREVIEW y ESCENARIO en un solo recuadro con tres pestañas; Ver →
      Separar el PREVIEW del LIVE; al cerrar y abrir se acuerda.
- [ ] Al ensanchar el panel derecho, LIVE y PREVIEW no se deforman.
- [ ] Con un anuncio en primer plano, el PREVIEW muestra la letra sin el
      anuncio.

**Biblia**
- [ ] Versiones en una fila: clic = sólo esa; Ctrl+clic = suma la segunda;
      ▸ despliega los nombres; clic derecho → Editar, Exportar, Eliminar.
- [ ] «➕ Playlist»: escribir «juan 8 del 5 al 10», ver el texto, Agregar.

**Diapositivas y resaltado**
- [ ] Resaltar en el LIVE: arrastrando se marca en azul y al soltar sale en
      pantalla; un clic marca sólo esa palabra; Shift+clic suma otra; en la
      pestaña Resaltar, `gracia; fe` marca todas las apariciones.
- [ ] Reordenar diapositivas moviendo rápido: la barra azul no se escapa.

**Playlist**
- [ ] Con la tecla → apretada, se frena al final del pasaje y avisa; soltando
      y apretando de nuevo pasa al siguiente.
- [ ] Arrastrar un medio de la barra a la playlist.

**Canciones**
- [ ] Crear una canción con un título que ya existe (por ejemplo «mi gozo»):
      pregunta Reemplazar, Guardar como (1) o Descartar.

**Medios**
- [ ] Arrastrar un archivo desde el Explorador y un medio a otra pestaña.
- [ ] Clic derecho sobre una pestaña: Cambiar nombre (con emoji),
      Configurar… (todo fondo o todo primer plano, logo al terminar),
      Eliminar.
- [ ] Propiedades de un medio → «Qué sigue después»: que pase solo a otro
      medio o al logo.
- [ ] Agregar un video de 4K o 60 cuadros: pregunta si hace una copia
      liviana; con Sí, ver la barra; en el LIVE se ve la imagen quieta.

**YouTube**
- [ ] Al agregar un video pregunta «por internet» o «descargar»; los de
      internet tienen la marca 🌐.
- [ ] Descargar uno: la primera vez baja sus componentes (unos 60 MB) y
      después el video, sin errores.

**Tiempos y Música**
- [ ] Pestaña Tiempos: elegir una sección, iniciar, pasarse del tiempo (rojo,
      en negativo, también en el recuadro «Temporizador» del escenario, que
      se agrega desde el editor), ±1 minuto, Siguiente, cronómetro.
- [ ] Pestaña Música: agregar audios, que se oigan, pausa, barra, volumen, que
      siga con el próximo. Y los botones de Spotify con Spotify abierto.

**Si se vuelve a congelar**
- [ ] Mirar el último archivo de la carpeta `logs`: la línea «Ventana
      congelada» dice qué estaba haciendo. Pasámelo.

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
- [ ] **Armar la beta.7.** Las tres tandas de lo pedido al probar la beta.6
      (7/10/2026, en [plan-sonnet-2.md](plan-sonnet-2.md)) ya están
      programadas, pero ninguna está en el instalador de la beta.6: falta
      armar uno nuevo con todo, y probarlo (sección 1).

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
