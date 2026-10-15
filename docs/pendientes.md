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

Se probaron el 14/10/2026 con la beta.11 y quedaron **dados por buenos**: el mini
reproductor de Spotify, «mismo título, distinto artista» y las flechas del
teclado en Propiedades. Lo que sigue se corrigió o se agregó después de esa
prueba y no se abrió desde acá: hay compilación, pruebas y dibujos a imagen.

- [ ] **Con el logo puesto no queda nada corriendo detrás** (lo comprobó Claude el
      14/10 en la PC: con un video de fondo el uso de procesador bajó de 18,7 %
      a 8,4 % de un núcleo con el logo puesto, y al sacarlo el video siguió en el
      mismo segundo, 0:05 antes y 0:05 después de 8 segundos de logo). Falta
      probarlo con el proyector real y con los casos que no se probaron: el
      video que rellena los bordes, una página web o una ventana en vivo, y un
      logo que es un video (ése tiene que seguir moviéndose). Apagar y prender
      la salida con el logo puesto no tiene que arrancar el video. **Lo mismo
      con Black (F1)** (se agregó después, no se vio andar): el video se pausa y
      su sonido se corta mientras la pantalla está en negro, y al sacar el negro
      sigue donde estaba. Ojo: elegir un fondo con el logo puesto saca el logo
      solo (así funcionó siempre).
- [x] **El error de una imagen de Google parpadea.** Comprobado el 14/10 en la
      PC: sin ninguna imagen abierta, «➕ Agregar la abierta» pone la barra de
      abajo en rojo, con letra blanca, pulsando unos segundos, y a los ~8 s
      vuelve a la normalidad.
- [ ] **Cómo preparar una Biblia para importar** (se dibujó la ventana a imagen;
      falta abrirla en el programa). **Archivo → Cómo preparar una Biblia para
      importar…** abre una ventana con los dos formatos que se leen, un ejemplo
      de cada uno (Génesis 1:1-3 y Juan 3:16-17, Reina-Valera 1909) y un
      recuadro «¿Tu Biblia está en otro formato?». Probá: 1) **Guardar este
      ejemplo…** del JSON y del XML: se guardan y se abren bien con el Bloc de
      notas, con los acentos bien; 2) **importá cada ejemplo** con «Importar una
      Biblia» (cada uno trae 5 versículos: Génesis 1:1-3 y Juan 3:16-17) y
      proyectá Juan 3:16; 3) **📋 Copiar instrucciones para una IA** y pegalas en
      el Bloc de notas: se leen enteras y en castellano (idealmente probalas con
      una IA de verdad y una Biblia tuya en Word o texto, y contame si el
      programita que arma sirve); 4) «Importar una Biblia…» de esa ventana abre
      el selector de archivos; 5) con un archivo que no sirve (por ejemplo un
      .txt renombrado a .json), el mensaje de error ahora manda a esa ventana.
- [ ] **Cuatro funciones de antes que nunca tuvieron pasos de prueba** (si ya las
      viste, tildalas y listo):
      1. **Tipografía de cada recuadro del escenario.** En el diseñador de
         escenarios, elegí una letra distinta para las notas del predicador y
         otra para la hora: cada una cambia sólo la suya, en la vista previa y
         en la pantalla de los músicos. Un escenario guardado de antes se ve
         igual que antes.
      2. **Tipografía del texto rápido.** En la pestaña Texto rápido, la lista
         «Tipografía» cambia la letra de la vista previa al instante, y queda
         elegida al cerrar y abrir el programa. Vacía, sigue la del tema.
      3. **«Selah» y títulos de los Salmos.** En el editor de temas, tema de
         Biblia, la casilla «Mostrar Selah y los títulos de los Salmos» viene
         destildada. Hoy ninguna Biblia instalada trae esos textos, así que sólo
         se nota con una importada que los tenga (el ejemplo de «Cómo preparar
         una Biblia» no los trae): verificá al menos que la casilla se guarda.
      4. **Corregir el texto de un versículo.** Clic derecho sobre un versículo
         de la grilla → «Corregir el texto…»: cambiá una palabra, guardá, y se
         ve en la diapositiva (también en pantalla si está proyectado) y sigue
         ahí al cerrar y abrir el programa. Probalo en una Biblia importada de
         prueba, no en la que usás en el servicio.
- [ ] **Biblia, canciones y anuncios en capas separadas para OBS** (el servidor y
      la página se comprobaron con un banco de pruebas y un navegador, sin el
      programa abierto y sin OBS: 20 comprobaciones del servidor y las tres
      páginas dibujadas con una imagen y un video). Con el 📱 prendido, en la
      ventana hay tres botones **Copiar: Biblia / Canciones / Anuncios**. Probá en
      OBS con tres *Browser Source* (uno por dirección, con el ancho y alto de tu
      escena): 1) proyectá un versículo: sale **sólo** en el de Biblia; 2) una
      canción: sólo en el de canciones; 3) un texto rápido, una imagen o un video
      en primer plano, una página de una presentación, un aviso al pie y la cuenta
      regresiva: sólo en el de anuncios; 4) Clear, Black y Logo vacían las tres;
      5) el video se ve, se repite si está en bucle y no hace eco (sin sonido);
      con `&sonido=1` suena; 6) la dirección vieja (`/salida`) sigue mostrando
      lo mismo que antes, sin imágenes. **Lo que no se probó:** cómo se ve en OBS
      de verdad (que el fondo quede transparente y que el video no se corte), y si
      una imagen con zoom o corrimiento se ve distinto que en el proyector (en la
      transmisión no se aplican).
- [ ] **Teclas en los tooltips (diseño avanzado).** Pasá el mouse por Black,
      Clear, Logo, Sin fondo, Poner en vivo / Apagar salida y Proyectar ▶: el
      globito dice la tecla (F1, F2, F3, F4, Esc, Ctrl+Enter) aunque estés en
      el diseño avanzado, que las esconde de los botones.
- [ ] **Cambio de una imagen a otra (arreglado y comprobado en la PC).** El
      14/10 a la noche Claude grabó la Pantalla 2 a 30 cuadros por segundo
      pasando de OverFlow a «26-10-09 Encuentro» y de vuelta: al empezar el
      cambio el relleno desenfocado de la imagen vieja desaparecía de golpe
      (bordes negros) y al terminar el de la nueva aparecía de golpe (estaba
      tapado por la vieja). Ahora la imagen vieja queda entera, con su relleno,
      y la nueva se funde encima con el suyo. Con el arreglo se grabó de nuevo:
      sin cuadro negro y sin tirón. Mirá si a vos también te parece bien, con
      la salida en el proyector de verdad.
- [ ] **Fecha del evento con calendario.** En Propiedades, la fecha ya no se
      escribe: se elige en un calendario (en castellano, la semana empieza el
      lunes), y la ✕ la saca. Se comprobó en la PC que abre y que la nota de
      abajo dice el día en que pasa a «Eventos pasados». Falta ver que una
      fecha vieja (por ejemplo del mes pasado) mueva la imagen al guardar.
- [ ] **Sin Color ni Brillo.** En Propiedades ya no está la sección Color
      (brillo y tinte), y no se dibujan más. Una imagen que tenía brillo o tinte
      puestos se ve sin ellos.
- [ ] **El tiempo de una imagen en la barra de reproducción.** «Cuánto tiempo
      se ve» ahora vale para cualquier imagen (antes sólo en primer plano).
      Con un tiempo puesto, al proyectarla aparece la misma barra que con un
      video: «0:03 / 0:08» avanzando. Se comprobó en la PC con 8 segundos (al
      terminar hizo lo que dice la pestaña: el logo). Probá también ⏸ (pausa
      el tiempo), ±10 s, arrastrar la barra y volver al principio, y que al
      poner otra cosa en pantalla la barra se vaya.
- [ ] **Vencimiento de una imagen («Eventos pasados»).** En Propiedades hay
      «Fecha del evento (opcional)» y «se queda N días más» (10 por defecto).
      Elegí el 17/10/2026 en el calendario: la nota de abajo dice «Pasa a «Eventos pasados» el
      27/10/2026». Cuando llega ese día (al abrir el programa o al cambiar la
      biblioteca) la imagen pasa sola a la pestaña **Eventos pasados** (se
      crea sola) y la barra de estado lo avisa. Para probarlo sin esperar: una
      fecha vieja (por ejemplo 1/1/2026) se mueve al guardar. Si la sacás de
      «Eventos pasados» a mano, no vuelve (la fecha se borra al archivarla).
      La ventana de Propiedades entra en la pantalla de la notebook (se vio el
      14/10).
- [ ] **Imágenes nuevas: «Ajustar» y relleno desenfocado por defecto.** Agregá
      una imagen con ➕ o arrastrándola, o con «Agregar a medios» de Google: en
      Propiedades tiene que venir con Escala «Ajustar» y «Los bordes se rellenan
      con (la misma imagen, desenfocada)». Los videos, los PDF y las imágenes
      que ya estaban no cambian.
- [ ] **«Agregar la abierta» en Google Imágenes.** Con una imagen abierta en
      grande a la derecha: el botón **➕ Agregar la abierta** de la fila de
      imágenes, y el clic derecho sobre cualquier parte de la imagen grande (o de
      lo que la tape), ofrecen «Agregar a medios» y bajan **la imagen original**.
- [ ] **Achicar la lista de audios.** En la pestaña Música, un clic en el título
      **▾ AUDIOS** esconde la lista, sus botones **y el reproductor de audios**
      (la barra, el tiempo, los botones de anterior y siguiente; queda sólo el
      reproductor de Spotify de abajo); otro clic los devuelve. También está en
      Ver → «Achicar la lista de audios». Con un audio sonando, achicar no lo
      corta. Cerrá y abrí el programa: queda como lo dejaste, y «Volver al
      diseño original» la despliega.
- [ ] **Relleno con la misma imagen desenfocada.** Se arregló el reborde negro de
      abajo (era un defecto del desenfoque de WPF cuando va con agrandado: se
      midió 48 filas negras y ahora son 0). Probalo con una imagen en «Ajustar»
      o con tamaño fijo y «(la misma imagen, desenfocada)»: no tiene que quedar
      ningún borde negro arriba, abajo ni a los costados, en la vista previa, el
      LIVE y la pantalla. También el pasaje **de una imagen a otra**: el relleno
      de la nueva ahora sube con el fundido en vez de aparecer de golpe. Ojo: un
      fundido entre dos imágenes siempre las mezcla mientras dura; si lo ves
      demasiado largo, se acorta en **Proyección → Transiciones… → Medios** (en
      0 corta directo).
- [ ] **Imágenes y GIF de Google.**
      1. En la barra de medios, **🖼 Imágenes** abre el panel web en Google
         Imágenes. La fila nueva del panel busca con Enter, **🖼 Imágenes**,
         **🎞 GIF** y «Sólo licencia libre». Escribir en esa caja no tiene que
         mover diapositivas (F1 sí anda).
      2. **Clic derecho sobre una imagen**, aunque sea una miniatura sin abrir:
         el menú ahora es del programa y tiene que **abrirse donde hiciste clic**
         (antes salía lejos). Con «➕ Agregar a medios» baja **la imagen
         original** (Google pone su dirección en el vínculo de cada resultado:
         se comprobó en el navegador y las direcciones bajan completas). Mirá que
         entre grande y no borrosa; el estado dice si es chica.
      3. Si por algún motivo sólo se conoce la miniatura de Google, no la baja:
         avisa que hagas clic en la imagen para verla grande y pruebes sobre la
         grande.
      4. En una página común, el menú tiene Abrir vínculo, Copiar vínculo,
         Copiar (con texto elegido), Atrás, Adelante y Recargar.
      5. GIF animado: queda como video en bucle y sin sonido. Un webp animado
         queda quieto. Quitar de la biblioteca borra el archivo bajado de
         `%APPDATA%\EcclesiaCast\imagenes-web`. Sin internet: «No se pudo bajar
         la imagen».
      6. Cuidado con los derechos: Google muestra imágenes de cualquiera; el
         filtro de licencia es lo que declara Google, no una garantía.
- [ ] **Buscar desde el celular (y Ctrl+K).** Arriba de todo en la página del
      celular hay una caja «Buscar canción o versículo…». Mejoró lo que busca:
      1. **Escribí «juan 3 16»** (o «sal 23», «1 co 13 4-7»): el primer
         resultado es el versículo o el capítulo (con la versión elegida), y al
         tocarlo se carga en la computadora para elegir cuál proyectar. Lo
         mismo en Ctrl+K.
      2. **Escribí el nombre de una canción** («hay libertad»): sale primera la
         que se llama así; después las que lo tienen en el nombre, el artista y,
         al final, las que sólo lo dicen en la letra (marcadas «En la letra»).
         Antes salían por orden alfabético y la tuya podía quedar afuera.
      3. Una palabra suelta («gracia») sigue buscando en las letras y en el
         texto de los versículos, sin necesidad de poner «?».
      4. Tocar un resultado lo **carga** sin proyectarlo; la página baja a las
         diapositivas. Sin resultados dice «No encontré nada».

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
      1. [x] Preparado en el repositorio y **subido a `main`** (14/10/2026):
         sección «Code signing policy» en el README, [política de
         privacidad](privacidad.md), firma en `.github/workflows/release.yml`
         (apagada hasta tener las claves) y `.signpath/`.
      2. [x] Alexis activó la verificación en dos pasos en GitHub y **mandó la
         solicitud a SignPath el 15/10/2026** (los textos están en
         [signpath-solicitud.md](signpath-solicitud.md)). **Esperando la
         respuesta** por correo (mirar también el correo no deseado).
      3. [ ] Cuando aprueben: en GitHub, *Settings → Secrets and variables →
         Actions*, el secreto `SIGNPATH_API_TOKEN` y la variable
         `SIGNPATH_ORGANIZATION_ID`; en SignPath, el proyecto con el nombre
         `EcclesiaCast`, la política `release-signing` (con aprobación
         manual) y **dos configuraciones de artefacto**: la de instalador,
         por defecto (`.signpath/artifact-configuration.xml`), y otra con el
         identificador `app-exe` (`.signpath/artifact-configuration-app.xml`).
         Desde ahí cada versión pide **dos firmas**, una detrás de la otra, y
         las dos esperan que las apruebes en SignPath (una hora de plazo cada
         una) antes de publicarse.
      4. [x] **Para que las actualizaciones comprueben la firma** (preparado el
         14/10/2026, sin probar con SignPath real). El programa sólo exige que
         el instalador nuevo tenga el mismo firmante cuando el programa
         **instalado** está firmado (`EcclesiaCast.App.exe` o `unins000.exe`).
         Por eso el flujo firma primero `EcclesiaCast.App.exe` y recién
         después arma el instalador con el programa ya firmado, y luego firma
         el instalador. Si SignPath devolviera algo sin firma, el flujo se
         corta en vez de publicar. Mientras SignPath no esté configurado, la
         comprobación que queda es el tamaño y el SHA-256 que publica GitHub.
- [ ] Actualizar README y CHANGELOG (hoy dicen «Beta») y taguear `v1.0.0`. La
      lista paso a paso, ya redactada, está en [publicar-1.0.md](publicar-1.0.md).

## 3. Para hacer después de la 1.0


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

## 4. Para versiones futuras

- **Acerca de: una forma de colaborar.**
- **NDI para OBS.** Las salidas separadas ya están como páginas web
  (`/salida/biblia`, `/salida/canciones`, `/salida/anuncios`). NDI de verdad, si
  hace falta más adelante, necesita el kit de NDI (con sus condiciones de
  licencia) y el complemento de NDI en el OBS.

## 5. Decisiones tomadas

Descartado (15/10/2026): bajar una presentación de Google Slides pegando su
enlace. Quien lo necesite descarga el PDF y lo agrega (los PDF y PowerPoint ya
se importan), o abre Canva o Google Slides en el panel **🌐 Web** con la sesión
iniciada y los proyecta como foto o en vivo.

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
