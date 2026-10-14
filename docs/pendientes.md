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

La beta.7 se probó entera el 8/10/2026 y quedó **dada por buena**: se usa el
domingo 11/10 en la reunión. Lo que sigue se programó después y no se pudo
abrir desde acá (la PC estaba en uso); se probó con compilación, pruebas y
dibujando a imágenes. Para la **beta.7 reconstruida** (instalador nuevo):

- [ ] **Zoom y corrimiento sobre la imagen entera («Rellenar»).** Es lo más
      importante de esta tanda. Poné una **página vertical** (la tapa de un
      manual) en «Rellenar» y abrí Propiedades:
      1. Sin tocar nada, tiene que verse **centrada**: se ve el medio de la
         página, no la parte de arriba como antes (las imágenes anchas
         recortan igual a los dos costados). Las imágenes 16:9, «Ajustar» y
         «Estirar» se ven igual que antes. Si alguna imagen ya tenía un
         corrimiento puesto, puede que haya que acomodarla de nuevo.
      2. **Alejá el zoom** (ruedita sobre el cuadro, o el número): se tiene que
         ver **más de la página**, achicándose, con el color de relleno donde no
         llega. Antes se achicaba el recorte con negro alrededor.
      3. **Corré hacia arriba y hacia abajo** (arrastrando, o «Correr» ↕ con
         un número negativo o positivo): tiene que llegar hasta **el final**
         y hasta **el principio de la página**. «Correr» ahora admite hasta
         ±1000 %.
      4. Acercá el zoom (por ejemplo 200 %) y corré: se mueve sobre la página
         entera. El zoom gira alrededor del centro de la pantalla.
      5. Proyectala de verdad (clic en la tarjeta) y mirá que la pantalla, el
         LIVE y el PREVIEW se vean igual que el cuadro de Propiedades. Probá con
         un **video** en «Rellenar» con zoom y corrimiento (no se pudo probar
         con la app abierta: el video sólo se verificó leyendo el código), con
         **desenfoque**, y con «Tamaño fijo en pantalla» (el cuadro queda
         quieto y la imagen se mueve y se agranda detrás).
- [ ] **Lo que queda afuera, en Propiedades.** El cuadro de arriba a la derecha
      muestra la imagen entera con lo de afuera **oscurecido** y la pantalla
      marcada con un **rectángulo blanco**. Arrastrar y la ruedita tienen que
      seguir andando (la imagen se mueve lo mismo que el mouse). Mirá que la
      ventana siga entrando en la pantalla de la notebook: ahora es unos 15
      píxeles más alta. Las tarjetas de la barra de medios no muestran lo de
      afuera, sólo lo que sale en pantalla.
- [ ] **Miniaturas con su forma.** Al abrir el programa por primera vez con esta
      versión, las miniaturas de imágenes y de páginas se rehacen solas, en
      segundo plano (puede verse un instante de cambio). Una página vertical
      tiene que verse en su tarjeta como en la pantalla. Las de video no
      cambian. Las miniaturas que ya existían ya eran de la forma de la imagen
      (se comprobó mirando la carpeta, sin tocarla); el cambio las deja de un
      tamaño parejo y sin depender de Windows.
- [ ] **Miniatura fiel en Propiedades.** Clic derecho sobre un medio →
      Propiedades. Arriba a la derecha hay un cuadro 16:9. Cambiá zoom,
      «Correr», escala, tamaño fijo, color de relleno, brillo, tinte y
      desenfoque: el cuadro tiene que cambiar al instante (sin guardar).
      **Arrastrá** sobre el cuadro: la imagen se corre y los números de
      «Correr» se mueven. **Ruedita** sobre el cuadro: cambia el «Zoom» de a 5.
      Probalo con una imagen, con un video y con uno de YouTube. Mirá que la
      ventana entre entera en la pantalla de la notebook.
- [ ] **Tarjetas con el encuadre.** Después de guardar, la tarjeta de la barra
      de medios muestra el encuadre (por ejemplo una página de PDF con
      «Ajustar», con sus barras negras; una imagen con zoom, acercada).
      Abrí una pestaña con muchos medios y fijate que no tarde más que antes.
- [ ] **Selección múltiple.** Ctrl+clic suma o saca; Mayús+clic elige el
      tramo; Ctrl+A elige todos los de la pestaña; arrastrar un rectángulo en
      el espacio vacío elige los que toca (con Ctrl suma a los ya elegidos);
      clic en un espacio vacío limpia; Esc limpia cuando hay varios. Los
      elegidos se ven con borde y fondo azul. Con varios elegidos: Supr (o
      clic derecho → Quitar de la biblioteca) pregunta **una sola vez** y dice
      cuántos; arrastrar uno a otra pestaña mueve todos; arrastrarlos a la
      playlist los agrega todos, en orden; clic derecho → «Mover a la
      pestaña…» muestra la lista. Un clic común sigue proyectando. Mirá que el
      Esc siga apagando la pantalla después de proyectar un medio con un clic.
- [ ] **Páginas de PDF nítidas.** Importá un PDF o una presentación de
      PowerPoint (las páginas nuevas se dibujan al doble, 4K), ponele zoom
      160 % en Propiedades y compará con el navegador: no tiene que
      pixelarse. Una imagen común sin zoom tiene que proyectarse igual que
      siempre, sin demora nueva.
- [ ] **Volver a importar en alta calidad.** Clic derecho sobre una página de
      un documento **ya importado antes** → «Volver a importar en alta
      calidad…»: pide elegir el PDF o la presentación original y dibuja de
      nuevo todas las páginas de ese documento. En un documento importado con
      esta versión no pide nada. Si hay una página en pantalla, tiene que
      cambiar sola a la versión nítida.

- [ ] **Proyectar una página web** (pedido de Alexis, 8/10/2026). Claude lo
      probó con la app abierta el 8/10 a la noche, sin mouse ni teclado (los
      botones por automatización): la página en el monitor grande, el en vivo
      (A+ se ve al instante), la foto que queda quieta, el panel cerrado en
      vivo, otro fondo que lo apaga, «Guardar en medios» con una foto, y la app
      minimizada un minuto sin que la pantalla se apague. Ahí apareció un
      error que quedó arreglado: con otra ventana encima, la página se ponía
      **negra** en la pantalla. Quedan para vos el mouse, el teclado y el
      puntero.
      1. «🌐 Web» → escribí `google.com` y buscá algo. Probá escribir en el
         buscador de la página: las flechas, la barra espaciadora y Esc **no**
         tienen que mover las diapositivas ni apagar la pantalla. F1 (negro)
         sí tiene que andar aunque estés escribiendo en la página.
         Mirá que el **mouse** apunte bien (clics, ruedita, seleccionar texto)
         aunque el panel sea más chico que 1920×1080: la página se achica con
         un Viewbox.
      2. «📷 Proyectar foto»: la pantalla muestra esa foto, entera y tapando la
         letra. Seguí navegando: la pantalla no cambia hasta la próxima foto.
      3. «🔴 En vivo»: la pantalla sigue lo que hacés (desplazarte, un video de
         la página) y muestra el puntero (probá con la casilla destildada).
         Volvé a las diapositivas con «✕ Cerrar»: tiene que seguir en vivo.
         Proyectá otro fondo, o una diapositiva: el en vivo se apaga y el
         botón se despinta. Probá F1, F2, F3 con una página en vivo. Abrí
         otro programa a pantalla completa encima de EcclesiaCast: la página
         tiene que seguir viéndose en la pantalla grande.
         Con el modo de PC de bajo rendimiento, LIVE y PREVIEW muestran una
         foto que se renueva cada 2 segundos.
      4. A− / A+ agrandan la letra de la página en la pantalla.
      5. «Guardar en medios» → aparece una tarjeta 🌐; un clic la abre; agregala
         a la playlist: al elegirla se abre el panel y el doble clic la pone en
         vivo. Con una foto tomada, «Guardar en medios» la guarda como imagen.
         Clic derecho en la tarjeta 🌐: sólo cambiar nombre, mover de pestaña,
         playlist y quitar.
      6. Probalo con **Canva** o **Google Slides** con la sesión iniciada, y con
         un video de la página en vivo (los videos con protección, como
         Netflix o Spotify, se ven negros: ya se sabe).
      7. Cuántos cuadros por segundo da en la PC de la iglesia con el proyector
         real (en la notebook de prueba dio 55 a 58 con una animación a 60).

- [ ] **Actualizaciones automáticas** (se programaron sin abrir el programa:
      sólo hay pruebas y dibujos). Hace falta que esté publicada en GitHub una
      versión **mayor** que la instalada, con su instalador (la 1.0.0-beta.6 y
      la beta.7 todavía no están: por ahora lo último publicado es la beta.5).
      Con la beta.7 instalada:
      1. Abrí el programa: a los pocos segundos (15) aparece el aviso con lo
         que trae la versión nueva, en castellano. Si hoy ya buscó, no busca
         de nuevo: usá Ayuda → «Buscar actualizaciones…».
      2. «Más tarde» → cierra el aviso y queda el botón **⬆ Versión nueva** en
         la fila del menú; no vuelve a avisar hasta mañana. «Saltar esta
         versión» → el botón desaparece y no avisa más por esa.
      3. Con la salida prendida, no aparece ninguna ventana: aparece «⬆ Versión
         nueva» arriba a la derecha, y tocándolo se abre el aviso.
      4. «Actualizar ahora» → baja con la barra de avance (probá «Cancelar»),
         se cierra, Windows pide permiso, el instalador actualiza y el
         programa se vuelve a abrir solo. Las canciones y todo lo demás siguen
         ahí, y hay una copia en `%APPDATA%\EcclesiaCast\respaldos`
         (`antes-de-<versión>.db`). Con la salida prendida, antes pregunta
         «Se va a cerrar la proyección para actualizar. ¿Seguimos?».
         Mirá sobre todo que **el programa se reabra solo** y que lo haga
         **sin permisos de administrador** (no pudo probarse).
      5. Ayuda → «Buscar actualizaciones…» sin nada nuevo dice «Tenés la
         última versión». Sin internet: «No se pudo buscar: revisá la
         conexión a internet». Destildá «Buscar actualizaciones al abrir» y
         reabrí: no busca.
      6. Si decís que no al permiso de administrador de Windows, el aviso
         tiene que quedarse abierto diciendo que no se instaló nada.

- [ ] **Secciones que se achican a íconos.** Se probó dibujando la ventana a
      imágenes (sin abrirla); falta verla de verdad:
      1. Clic en el título **▾ MEDIOS**: queda sólo la cabecera con los
         botones y las diapositivas ocupan el lugar. Otro clic la devuelve
         **con el alto que tenía**. Probalo con «Medios arriba de las
         diapositivas» y con una pestaña con muchos medios.
      2. En el panel de canciones, botón **⇤**: queda una columna de íconos
         🎵 📖 📋. Cada ícono abre lo suyo y saca el panel del riel; **⇥** lo
         devuelve. Probalo con «Canciones y playlist a la derecha». Cerrá y
         abrí el programa: tiene que volver como lo dejaste, y al agrandar,
         con el ancho de antes.
      3. **▾** de las herramientas de texto (abajo a la derecha): queda una
         tira de seis íconos; un clic en uno abre esa pestaña. Si estás
         escribiendo un texto rápido, no se pierde lo escrito.
      4. Menú **Ver**: las tres entradas nuevas se tildan solas y «Volver al
         diseño original» las apaga. Con el riel prendido no se puede arrastrar
         el borde de la columna (es a propósito).

- [ ] **Proyectar otra ventana (🪟 Ventana).** Se probó con un programa de
      consola que capturó una ventana ajena 5 s (22 cuadros por segundo, en
      12 con bajo rendimiento) y dibujando la vista a imagen; falta usarlo
      con la salida prendida:
      1. Abrí un PowerPoint (o un video en un reproductor), tocá **🪟 Ventana**,
         elegí la ventana y **Proyectar ▶**. La pantalla tiene que mostrarla
         entera, en movimiento, y el LIVE también. Moví la ventana, cambiale el
         tamaño: la imagen se acomoda sin parpadear.
      2. Probá con una **presentación a pantalla completa** y con una ventana
         de un **video con sonido** (el sonido sale de la PC, no de la
         captura). Mirá si se nota que la computadora trabaja de más; si anda
         lento, probá el modo de bajo rendimiento.
      3. **Minimizá** la ventana: queda el último cuadro y el aviso de abajo lo
         dice. Restauralá: sigue. **Cerrala**: se deja de proyectar sola.
      4. F4, o proyectar un medio o una diapositiva: la captura se apaga (el
         borde amarillo de la ventana desaparece).
      5. **Elegir con la ventana de Windows…** y probar con una pantalla
         entera. Con **Mostrar el puntero** tildado, la flecha se ve en la
         pantalla.
      6. Una ventana de un programa abierto «como administrador»: tiene que
         avisar que Windows no la deja capturar (no pudo probarse).

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
