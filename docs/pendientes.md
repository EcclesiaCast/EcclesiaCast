# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

Hay cuatro tipos de cosas, cada una en su sección:

1. **Para probar**: ya está programado; falta que Alexis lo pruebe con el
   programa abierto, porque desde acá no se pudo.
2. **Para la v1.0.0**: lo que falta para dejar de ser beta.
3. **Para hacer después de la 1.0**: cambios que todavía hay que programar.
4. **Para versiones futuras**: ideas que quedan para más adelante.

## 1. Para probar (lo prueba Alexis)

Nada pendiente. La beta.7 se probó entera el 8/10/2026 y quedó **dada por
buena**: se usa el domingo 11/10 en la reunión.

## 2. Para la v1.0.0

- [ ] **Usar la beta.7 en la reunión del domingo** y anotar lo que falle o no
      guste.
- [ ] **Actualizaciones automáticas.** Tiene que estar en la 1.0: si no, quien
      instale la 1.0 nunca se entera de las versiones nuevas y tiene que
      bajarlas a mano. Al abrir, el programa se fija si hay una versión nueva
      (en las versiones publicadas de GitHub) y, si la hay, avisa con la
      lista de lo nuevo y lo corregido, en castellano. Botones «Actualizar
      ahora», «Más tarde» y «Saltar esta versión». Si acepta, baja el
      instalador, lo ejecuta y vuelve a abrir el programa. Nunca avisa
      mientras se está proyectando.
- [ ] **Firma digital del instalador.** Hoy Windows muestra «Editor
      desconocido» y hay que darle a «Más información → Ejecutar de todas
      formas». Opciones averiguadas el 8/10/2026:
      - **SignPath Foundation** (recomendada): firma gratis para proyectos de
        código abierto con licencia aprobada (GPL-3.0 lo es). Firma desde
        GitHub Actions, en el mismo workflow que arma el instalador. Pide
        publicar una «política de firma» en la página del proyecto, y en
        Windows el editor aparece como «SignPath Foundation». Se pide en
        https://signpath.org/apply.html.
      - **Certum, certificado para código abierto**: a nombre de Alexis,
        unos 50 a 70 euros por año (precio a confirmar en shop.certum.eu).
        Windows igual puede avisar al principio, hasta que el programa junta
        «reputación».
      - **Azure Artifact Signing** (Microsoft): no está disponible para
        personas de Uruguay (sólo Estados Unidos y Canadá).
- [ ] Actualizar README y CHANGELOG (hoy dicen «Beta») y taguear `v1.0.0`.

## 3. Para hacer después de la 1.0

- [ ] **Ejemplo de formato para importar Biblias.** Un archivo de ejemplo en
      JSON y otro en Zefania XML (unos pocos versículos), y en la ventana de
      importar: «Ver un ejemplo» y «Copiar instrucciones para una IA» (un
      texto listo para pegar en un chat de IA, que explica el formato para
      que convierta o revise un archivo).
- [ ] **Biblias: cómo distribuirlas, al menos en Uruguay.** Lo averiguado el
      7/10/2026: las versiones con derechos (RVR1960, NVI, NTV, TLA, y
      también la TCB y la PDT que Alexis quiere sumar) no se pueden repartir con el programa ni desde un
      repositorio, aunque se avise; OpenLP y CrossWire no lo hacen por eso.
      Opciones:
      - Pedir permiso a quien tiene los derechos (Sociedad Bíblica del
        Uruguay, American Bible Society para la RVR1960, Biblica para la NVI,
        Tyndale para la NTV, Liga Bíblica para la PDT). Un mail explicando
        que es un programa libre y gratuito para iglesias.
      - Un botón «Agregar Biblias» con un catálogo de Biblias libres de
        varios idiomas (https://github.com/seven1m/open-bibles: en castellano
        RV1909, Biblia en Español Sencillo, Palabra de Dios para ti, Versión
        Biblia Libre).
      - Una carpeta privada `biblias` junto al instalador, para las PCs de la
        iglesia.
- [ ] **Canva y Google Slides.** Los PDF y los PowerPoint ya se importan: cada
      página queda como una imagen en la pestaña de medios elegida, en orden.
      Para Canva y Google Slides hoy hay que descargarlos como PDF y
      agregarlos. Mejora posible: pegar el enlace de una presentación pública
      de Google Slides y que el programa la baje como PDF sola (Canva pide
      iniciar sesión, así que ahí sigue siendo descargar el PDF).

## 4. Para versiones futuras

- **Acerca de: una forma de colaborar.**
- **Salidas separadas para OBS (NDI).** Mandar al OBS, por separado, la
  Biblia, las canciones y los anuncios (imágenes y videos). Primero con
  páginas web separadas (`/salida/biblia`, `/salida/canciones`,
  `/salida/anuncios`, con fondo transparente, sobre la página `/salida` que ya
  existe); NDI de verdad después, si hace falta (necesita el kit de NDI, con
  sus condiciones de licencia, y el complemento de NDI en el OBS).

## 5. Decisiones tomadas

Descartado: importar las Biblias de BibleShow. Sus archivos `.bib` son bases
de Access protegidas con usuario y contraseña, y NTV, NVI y PDT son textos con
derechos licenciados para usar dentro de BibleShow.

Descartado: pasar a H.264 también los videos HEVC y AV1 (hoy sólo se copian
los 4K o de 60 cuadros). La PC de la iglesia (i5-10400F, 16 GB, RTX 3050)
decodifica HEVC por hardware, y el AV1 de YouTube lo resuelve el procesador
con muy poco esfuerzo. Sólo serviría en una PC sin decodificación por
hardware: ahí el HEVC cuesta 40 % menos en H.264 (medido el 4/10/2026). Si
algún día hace falta, es sumar una condición a `LightCopyPolicy`.

## 6. Ideas tomadas de Spresenter (10/9/2026)

Comparación completa contra [spresenter.com](https://spresenter.com) hecha el
10/9/2026. Las tres que valían la pena están hechas: salida web para OBS y
vMix, buscador único y soporte de PDF y PowerPoint.

Fuera de alcance a propósito (es otro producto: iglesias grandes con equipo
técnico): edge blending y warp entre proyectores, display slices para paneles
LED, sincronización de varias máquinas en red, automatización OSC/MIDI,
timeline y VFX, macOS. (NDI estaba acá; desde el 8/10/2026 está anotado para versiones futuras: ver sección 4.)

## 7. Conocidos y aceptados

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
