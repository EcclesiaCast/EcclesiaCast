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

Todo lo anterior se probó el 14/10/2026 con la beta.10 y quedó dado por bueno
(salvo lo que se anotó como falla). Lo que sigue se programó después y no se
abrió desde acá: sólo hay compilación y pruebas.

- [ ] **Mini reproductor en Música.** Con Spotify sonando, en la pestaña Música:
      1) se ven la carátula, el título y el artista, y la barra avanza cada
      segundo; 2) el ícono cambia entre ⏸ y ▶ al pausar desde Spotify o desde
      el botón; 3) anterior, pausa y siguiente andan; 4) al cambiar de canción
      desde Spotify se actualiza sola; 5) con Spotify cerrado dice «No hay nada
      sonando» y los botones siguen mandando la tecla multimedia; 6) probalo
      también con un video de YouTube en el navegador; 7) el volumen de abajo
      sigue andando. La barra no se puede arrastrar y no hay fila de
      reproducción (Windows no la da).
- [ ] **Mismo título, distinto artista.** Guardá «Hay Libertad» con artista IBI
      y otra con artista Christina Aguilera: quedan las dos, sin pregunta
      (probá escribiéndola, importando un archivo y desde la búsqueda en
      internet). Si repetís la misma con el mismo artista, o sin artista,
      tiene que preguntar y nombrar al artista. En la lista se ve el artista
      debajo del título.
- [ ] **Achicar la lista de audios.** En la pestaña Música, un clic en el título
      **▾ AUDIOS** esconde la lista y sus botones (queda el reproductor); otro
      clic la devuelve. También está en Ver → «Achicar la lista de audios»
      (se tilda sola). Cerrá y abrí el programa: queda como lo dejaste, y
      «Volver al diseño original» la despliega.
- [ ] **Flechas del teclado en Propiedades.** Clic derecho en una imagen →
      Propiedades: el cuadro de vista previa arranca elegido. Las flechas
      corren la imagen de a 0,1 % (con Mayús, de a 1 %) y los números de
      «Correr» se mueven. Si hacés clic en un campo de texto o en una lista,
      las flechas vuelven a hacer lo de siempre ahí; un clic o un arrastre
      sobre el cuadro las devuelve a la imagen.
- [ ] **Relleno con la misma imagen desenfocada.** En Propiedades de una
      **imagen**, «Los bordes se rellenan con»: la lista tiene **(la misma
      imagen, desenfocada)** (en videos y YouTube no aparece). Elegila con una
      imagen en «Ajustar» (o con un tamaño fijo, o con zoom menor a 100 %): en
      vez de barras negras, los costados se ven con la misma imagen agrandada y
      borrosa, como una sola imagen. Mirá la vista previa, la tarjeta de la
      barra de medios, el LIVE y la pantalla; con un color sólido (la opción de
      antes) todo sigue igual. Fijate que no quede un borde oscuro en las
      orillas de la pantalla y que no pese en la PC de la iglesia (en el modo de
      bajo rendimiento también se ve).
- [ ] **Imágenes y GIF de Google (lo más incierto: no se pudo probar nada
      con Google de verdad).**
      1. En la barra de medios, **🖼 Imágenes** abre el panel web en Google
         Imágenes, en castellano. Dentro del panel hay una fila nueva: escribí
         «cruz» y Enter (o **🖼 Imágenes** / **🎞 GIF**): abre la búsqueda;
         **🎞 GIF** muestra sólo animados. Con «Sólo licencia libre» tildada
         se ve el filtro «Derechos de uso» de Google. Escribir en esa caja no
         tiene que mover diapositivas ni apagar la pantalla (F1 sí anda).
      2. En Google, clic en una imagen para verla grande y **clic derecho
         sobre la grande**: el menú tiene arriba **➕ Agregar a medios**. Se baja
         y entra a la pestaña de medios abierta, con el nombre de lo que
         buscaste («cruz», «cruz (2)»…). **Lo importante:** mirá que lo que
         entra sea la imagen grande y no la miniatura; si es chica, el estado
         lo dice con su tamaño. Si el clic derecho sobre la grande no da
         «Agregar a medios» (Google a veces tapa la imagen con un enlace),
         avisame qué pasa y lo cambio.
      3. Con un GIF animado: tiene que quedar como video en bucle y sin sonido
         (como un GIF que arrastrás). Probá también con un webp animado: ése
         queda quieto.
      4. Clic derecho → «Quitar de la biblioteca» borra el archivo bajado de
         `%APPDATA%\EcclesiaCast\imagenes-web`. Sin internet: «No se pudo
         bajar la imagen».
      5. Cuidado con los derechos: Google muestra imágenes de cualquiera; el
         filtro de licencia es lo que declara Google, no una garantía.
- [ ] **Buscar desde el celular.** Con el control desde el celular prendido,
      arriba de todo hay una caja «Buscar canción o versículo…». Escribí dos
      letras o más (por ejemplo «gracia» o «juan 3 16»): salen canciones (♪) y
      versículos (†) de todas las versiones, como en Ctrl+K pero sin medios.
      Tocar un resultado lo **carga** en la computadora (no lo proyecta) y la
      página baja a las diapositivas de esa canción o de ese capítulo, para
      elegir cuál proyectar. Mirá que la búsqueda no trabe la computadora
      mientras se escribe y que, sin resultados, diga «No encontré nada».

## 2. Para la v1.0.0

- [ ] **Usar la beta.7 en la reunión del domingo** y anotar lo que falle o no
      guste.
- [x] **Actualizaciones automáticas.** Hechas (programadas y probadas con
      pruebas y dibujos; falta probarlas con el programa abierto, ver §1).
      Para que las notas de cada versión salgan en castellano, al publicar la
      1.0 hay que **renombrar `[Sin publicar]` a `[1.0.0] - fecha`** en el
      `CHANGELOG.md` (y dejar arriba un `[Sin publicar]` vacío): el workflow
      toma de ahí la sección de la versión que se publica, y si no la
      encuentra usa `[Sin publicar]`.
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

      Se eligió SignPath. Pasos:
      1. [x] Preparado en el repositorio (8/10/2026): sección «Code signing
         policy» en el README, [política de privacidad](privacidad.md),
         firma en `.github/workflows/release.yml` (apagada hasta tener las
         claves) y `.signpath/artifact-configuration.xml`. **Falta subirlo a
         GitHub** (push a `main`), porque SignPath lo mira ahí.
      2. [ ] Alexis: verificación en dos pasos activada en GitHub (la piden
         para todo el equipo). Llenar el formulario de
         https://signpath.org/apply.html y esperar la respuesta.
      3. [ ] Cuando aprueben: en GitHub, *Settings → Secrets and variables →
         Actions*, el secreto `SIGNPATH_API_TOKEN` y la variable
         `SIGNPATH_ORGANIZATION_ID`; en SignPath, el proyecto con el nombre
         `EcclesiaCast`, la política `release-signing` y la configuración
         del archivo de `.signpath/`. Desde ahí cada versión espera que la
         apruebes en SignPath (hay una hora de plazo) antes de publicarse.
      4. [ ] **Para que las actualizaciones comprueben la firma.** El programa
         sólo exige que el instalador nuevo tenga el mismo firmante cuando el
         programa **instalado** está firmado (`EcclesiaCast.App.exe` o
         `unins000.exe`). La configuración actual de `.signpath/` firma sólo
         el `setup.exe`, y los archivos que el instalador deja en la
         carpeta del programa quedan sin firma, así que esa comprobación nunca
         se activaría. Falta firmar también `EcclesiaCast.App.exe` (en el mismo
         pedido de SignPath, antes de armar el instalador) o que el instalador
         firme el desinstalador (`SignedUninstaller`). Mientras tanto, la
         comprobación que queda es el tamaño y el SHA-256 que publica GitHub.
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
      iniciar sesión, así que ahí sigue siendo descargar el PDF). Mientras
      tanto, con el panel **🌐 Web** se puede abrir Canva o Google Slides con la
      sesión iniciada y proyectarlos como foto o en vivo.

## 4. Para versiones futuras

- **Acerca de: una forma de colaborar.**
- **Salidas separadas para OBS (NDI).** Mandar al OBS, por separado, la
  Biblia, las canciones y los anuncios (imágenes y videos). Primero con
  páginas web separadas (`/salida/biblia`, `/salida/canciones`,
  `/salida/anuncios`, con fondo transparente, sobre la página `/salida` que ya
  existe); NDI de verdad después, si hace falta (necesita el kit de NDI, con
  sus condiciones de licencia, y el complemento de NDI en el OBS).

## 5. Decisiones tomadas

Buscador de imágenes y GIF (14/10/2026): se hace con **Google Imágenes dentro
del panel web**, sin API ni claves. Se descartó Tenor (Google cerró su API el
30/6/2026), la API de Google (cerrada a clientes nuevos y sin imágenes como
las de Google Imágenes) y Giphy (100 búsquedas por hora para todas las
iglesias juntas con una clave compartida, o una clave por iglesia). Google
entiende el castellano, así que tampoco hace falta traducir.

Descartado: importar las Biblias de BibleShow. Sus archivos `.bib` son bases
de Access protegidas con usuario y contraseña, y NTV, NVI y PDT son textos con
derechos licenciados para usar dentro de BibleShow.

Descartado: pasar a H.264 también los videos HEVC y AV1 (hoy sólo se copian
los 4K o de 60 cuadros). La PC de la iglesia (i5-10400F, 16 GB, RTX 3050)
decodifica HEVC por hardware, y el AV1 de YouTube lo resuelve el procesador
con muy poco esfuerzo. Sólo serviría en una PC sin decodificación por
hardware: ahí el HEVC cuesta 40 % menos en H.264 (medido el 4/10/2026). Si
algún día hace falta, es sumar una condición a `LightCopyPolicy`.

Decidido con Alexis (8/10/2026): «Rellenar» deja la imagen **centrada**, como
lo hacen los demás programas. Hasta la beta.6 quedaba pegada arriba a la
izquierda (así lo hace WPF): una foto 4:3 mostraba su parte de arriba y una
imagen muy ancha, su parte izquierda. Cambió cómo se ven las imágenes que no
son 16:9 (y las 16:9 con un tamaño fijo que no es 16:9); las que tenían un
corrimiento puesto pueden necesitar acomodarse de nuevo.

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
    salvo que proyectes un YouTube (ni el del panel web, hasta que lo abrís): verificado, 0 procesos con la salida
    encendida y un video local corriendo.
