# Pendientes

Lista única de lo que falta. Si algo no está acá, no está anotado en ningún
lado. Al cerrar un punto, borralo y anotalo en el [CHANGELOG](../CHANGELOG.md).

Hay cuatro tipos de cosas, cada una en su sección:

1. **Para probar**: ya está programado; falta que Alexis lo pruebe con el
   programa abierto, porque desde acá no se pudo.
2. **Después de publicar la v1.0.0**: comprobar la versión publicada,
   la firma digital y la página web.
3. **Para hacer después de la 1.0**: cambios que todavía hay que programar.
4. **Para versiones futuras**: ideas que quedan para más adelante.

## 1. Para probar (lo prueba Alexis)

Una sola lista numerada con **todo lo que todavía no probaste o de lo que no diste tu opinión**,
sea de la versión que sea. Va primero lo más nuevo (lo de la 1.5.0), después lo de versiones
anteriores. Cuando pruebes algo, decile a Claude el número («el 7 anduvo, el 12 falla así…») y
pasa a **Ya probado**, más abajo.

### Falta probar

**Visto por Claude el 25/11 en la 1.5.0-prueba.4 (no cuenta como probado por Alexis)**

- Anduvo: la ventana flotante («⧉ En una ventana»), una placa en EN VIVO que se va sola a los 10 s,
  buscar y escuchar en YouTube sólo audio (el proyector no cambia), ➕ a la lista, el menú Transmisión, y
  la página `/salida` de OBS dibujando una canción y la placa de «Pestaña».
- **Abrir el programa dos veces dejaba dos copias** (la segunda tomaba el puerto 8082 y OBS miraba la
  vacía). **Arreglado en la prueba.5, falta que lo pruebes:** con el programa abierto, abrirlo de nuevo
  desde el acceso directo tiene que traer al frente el que ya está, sin abrir otro.
- **Nuevo en la prueba.6, falta probar:** Ayuda → «Tutoriales…» (8 recorridos guiados sobre el programa real; ver `docs/tutoriales.md`). Fijate que cada tarjeta arranque, que el foco señale el botón correcto y que se pueda salir a mitad.
- **Nuevo en la prueba.5, falta probar:** «Transmisión → Salida para OBS o vMix…» da direcciones con
  `localhost`. Pegala una vez en OBS, reiniciá la PC y mirá que siga funcionando sin tocar nada. Debajo
  muestra la dirección con el nombre de la PC, para OBS en otra computadora.
- No se pudo comprobar (sonido, proyector real, otra PC): Pads, 🔂, 💾, volumen, YouTube silenciado,
  logo/negro frenando el video, y el de la pantalla única (la PC de prueba tiene dos pantallas).
- En la lista de Música el 🌐 no se ve cuando el nombre es largo, y el tacho no quitó una pista que
  estaba sonando.

**De la 1.5.0, lo último (prueba.3 y prueba.4)**

1. **Traer las canciones de Holyrics.** Es nuevo y **todavía no se probó con un Holyrics de verdad**:
   las pruebas automáticas usan respuestas armadas según la documentación de Holyrics.
   - En Holyrics, una sola vez: Archivo → Configuraciones → API Server → activar el servidor
     local, crear un token y habilitarle «GetSongs» y «GetLyrics».
   - Cargá antes dos o tres canciones de prueba, con acentos y con estrofas y coro marcados.
   - Con Holyrics abierto, en EcclesiaCast: ➕ Agregar → **Traer de otro programa…** →
     **Holyrics**. Escribí el puerto (viene 8091) y el token, y tocá **Conectar**: tiene que decir
     cuántas canciones encontró.
   - **Importar**, y mirar título, autor, acentos y los nombres de las estrofas (Estrofa 1, Coro,
     Puente…).
   - Importar otra vez: tiene que preguntar qué hacer con las repetidas.
   - Cerrar y volver a abrir la ventana: se conecta sola.
   - Con Holyrics cerrado, «Conectar» explica qué falta.
   - Las canciones archivadas en Holyrics no se traen: la ventana dice cuántas quedaron afuera.
2. **Un audio guardado, sin internet.** Claude guardó un pad con 💾 (quedó en Música › EcclesiaCast
   y arrancó al instante desde la lista); falta desconectar internet y ver que suene igual.
3. **La barra «Volumen» con la música de YouTube sonando:** que suba y baje esa música (en el código
   está conectada; falta oírlo).
4. **Pestaña Placas más aireada.** Lo pediste («queda todo muy apretado»). Cada dato va en un
   renglón con su nombre al costado (Nombre, Cargo, Imagen, Estilo, Dura). El botón «◧» / «◨» al
   lado de «Elegir…» cambia la imagen de lado, y Mostrar y Quitar van anchos. Claude la vio andar
   con una copia de la biblioteca; falta tu opinión.
5. **¿Cuánto tarda en sonar la música de YouTube?** Claude midió entre 18 y 25 segundos desde
   ▶ hasta que suena (la barra dice «Preparando la descarga…» mientras tanto). Decí si te parece
   aceptable o si hay que buscar que arranque antes.
6. **«🕘 Historial» en la Biblia.** El botón a la derecha de la caja de la cita ahora dice
   «🕘 Historial». Proyectá dos o tres versículos y abrilo: clic carga, doble clic proyecta. Achicá
   el panel de la Biblia: el botón pasa a ser sólo 🕘 y la caja de la cita no tiene que quedar
   chica. (Lo pidió alguien por el formulario; Claude no lo vio con el programa abierto.)

**De la 1.5.0, la primera tanda (18/11)**

Claude ya probó en esta PC la ventana flotante, la importación del XML de OpenSong y YouTube a
pantalla completa con Esc y doble clic. Falta:

7. **YouTube en Windows 10**, con la notebook sola (donde le pasó a la iglesia): clic sobre el
    video y Esc vuelve al programa; el doble clic también.
8. **La ventana flotante compartida por Discord** o una videollamada.
    - Que se transmita bien y que se pueda minimizar y volver.
    - Adentro: un video, el logo, el aviso al pie y la cuenta regresiva.
    - Que las páginas /salida (OBS, celular, escenario) sigan igual.
    - Con dos monitores: dejarla en el segundo, cerrar el programa, desenchufar el monitor y
      abrir: tiene que aparecer en el medio de la pantalla principal.
9. **Con una sola pantalla** (una notebook sin proyector, instalación nueva u otra PC).
    - «⧉ En una ventana» tiene que ser la primera de la lista «Salida:» y venir elegida.
    - Con el proyector en «duplicar», Windows ve una sola pantalla, así que también arranca en la
      ventana.
    - Para pantalla completa se elige «Pantalla 1» una vez, y al cerrar y abrir sigue ahí.
10. **El teclado y la ventana flotante.** Si se toca la ventana flotante, las teclas del panel
    (flechas, Ctrl+Enter) no andan hasta volver a hacer clic en el panel. Ver si molesta.
11. **«➕ Agregar» en Canciones** con el panel angosto: que el título «CANCIONES» no quede tapado.
12. **Una Biblia XML real** de OpenSong o de Beblia (la que exportan Holyrics o FreeShow). Si el
    archivo trae los libros en inglés, así se muestran («John 3:16»).

**De versiones anteriores, sin tu respuesta**

13. **Logo y Black frenan el video de atrás.** Con el logo (F3) o el negro (F1) puestos, el video
    de fondo se pausa y su sonido se corta, y al sacarlos sigue donde estaba.
    - Claude lo midió en la PC con un video común.
    - Falta con el proyector real y con estos casos: el video que rellena los bordes, una página
      web o una ventana en vivo, y un logo que es un video (ése tiene que seguir moviéndose).
14. **Tipografía de cada recuadro del escenario:** en el diseñador de escenarios, una letra para
    las notas y otra para la hora; cada una cambia sólo la suya.
15. **Tipografía del texto rápido:** la lista «Tipografía» de la pestaña Texto rápido cambia la
    vista previa y queda elegida al cerrar y abrir.
16. **«Selah» y títulos de los Salmos:** en el editor de temas (Biblia), la casilla «Mostrar Selah
    y los títulos de los Salmos» viene destildada y se guarda.
17. **Corregir el texto de un versículo:** clic derecho en un versículo de la grilla → «Corregir el
    texto…». El cambio se ve en la diapositiva y queda al cerrar y abrir. Probalo en una Biblia
    importada de prueba.
18. **Menú Transmisión y estilos para OBS** (plan en [plan-obs-estilos.md](plan-obs-estilos.md)).
    Con OBS abierto:
    - **El menú:** «Transmisión» tiene «Salida para OBS o vMix…» y «Estilos de la transmisión…».
    - **Plantillas:** las seis plantillas listas cambian la vista previa.
    - **Placas:** las ocho (Ninguna, Pestaña, Vidrio, Sólida, Degradé, Halo, Filete, Línea), y las
      posiciones Superior y Centro.
    - **Biblia y anuncios:** en la Biblia la cita va arriba de la placa; los anuncios salen en el
      centro.
    - **Fondo fijo:** con un video, se ve en bucle y se va con F1, F2 y F3.
    - **«Igual que en el proyector»:** apaga el resto de los controles.
    - **OBS sigue los cambios:** cambia a los 0,3 s de guardar, sin tocar nada.
19. **Teclas en los tooltips:** pasá el mouse por Black, Clear, Logo, Sin fondo y Poner en vivo: el
   globito dice su tecla (F1, F2, F3, F4, Esc). Claude los leyó el 19/11: anduvieron todos salvo el
   de **Logo**, que decía sólo el nombre del logo; se arregló para la prueba.4 (mirá que ahora diga
   «Muestra el logo a pantalla completa (tecla F3)…»). El de los botones de versión de la Biblia
   tenía el mismo error y también se arregló.
20. **El paso de una imagen a otra en el proyector real:** sin cuadro negro ni tirón, el relleno de
    la nueva sube con el fundido. Si el fundido te parece largo, se acorta en Proyección →
    Transiciones… → Medios.
21. **Fecha del evento y «Eventos pasados».** En Propiedades la fecha se elige en un calendario.
    Una fecha vieja (por ejemplo 1/1/2026) mueve la imagen a la pestaña **Eventos pasados** al
    guardar, y la barra de estado lo avisa.
22. **Sin Color ni Brillo en Propiedades:** una imagen que tenía brillo o tinte se ve sin ellos.
23. **El tiempo de una imagen en la barra.** Con «Cuánto tiempo se ve» puesto, al proyectarla
    aparece la barra «0:03 / 0:08». Probá ⏸, ±10 s, arrastrarla, y que se vaya al poner otra cosa.
24. **Imágenes nuevas con «Ajustar» y relleno desenfocado:** una imagen agregada viene así en
    Propiedades. Los videos, los PDF y las imágenes de antes no cambian.
25. **Imágenes y GIF de Google** (🖼 Imágenes en la barra de medios).
    - La fila de búsqueda con 🖼 Imágenes, 🎞 GIF y «Sólo licencia libre».
    - El clic derecho sobre una imagen abre el menú donde hiciste clic.
    - «➕ Agregar a medios» y «➕ Agregar la abierta» bajan la imagen original, grande.
    - Un GIF queda como video en bucle.
26. **Achicar la lista de audios:** clic en «▾ AUDIOS» (o Ver → «Achicar la lista de audios»)
    esconde la lista y su reproductor sin cortar lo que suena, y queda así al cerrar y abrir.
27. **Relleno con la misma imagen desenfocada:** sin ningún borde negro, en la vista previa, el
    LIVE y la pantalla.
28. **Buscar desde el celular.** En la página del celular, la caja «Buscar canción o versículo…»:
   «juan 3 16» sale primero el versículo; el nombre de una canción sale primero; tocar un
   resultado lo carga sin proyectarlo. (Ctrl+K en la computadora ya lo probó Claude: anduvo.)
29. **Fondos en video del instalador:** la casilla «Agregar 10 fondos en video».
    - Aparece la pestaña **Fondos de ejemplo** con los 10.
    - El bucle no se nota.
    - Uno quitado de la biblioteca no vuelve.
    - Sin la casilla, la pestaña no aparece.
30. **Traer de otro programa** (➕ Agregar en Canciones, o Archivo).
    - Los botones ProPresenter y EasyWorship, cada uno con su cantidad de canciones.
    - Importar una de cada uno y probar «Elegir carpeta…».
    - Falta probar una biblioteca grande de EasyWorship.
31. **Agregar paquete de fondos:** Archivo → «Agregar paquete de fondos (.zip)…».
    - Los videos van a la pestaña **Fondos agregados**, en bucle y sin sonido.
    - Un archivo que no sirve avisa sin romper nada.
32. **Sólo el texto para OBS:** a la dirección de la Biblia agregale `&formato=no`. En OBS sale sólo
    el texto en blanco, y el CSS de ejemplo de la guía le cambia color y tamaño.
33. **Lo de la tanda del 1/11.**
    - Arrastrar un renglón de la playlist (marca dónde cae).
    - Temas → **Plantillas listas** con una canción y con la Biblia.
    - Transmisión → Estilos → «Configuración extra del recuadro»; «Cancelar» devuelve lo de antes.
    - Cambiar de Biblia con un versículo en pantalla: OBS no queda vacío ni un instante.
    - Al actualizar saltando versiones, se ven las notas de todas.
34. **Presentación con una pestaña de medios:** clic derecho en una pestaña de Medios → Configurar…
    → «Pasar los medios solos», en orden y al azar. Cada video llega al final antes de pasar.
35. **Lo de la 1.2.0 que no viste.**
    - Las franjas del tema con una **canción**.
    - La lista de **Música** sonando mientras suena un video.
    - Todo con el proyector de la iglesia.
36. **Las flechas en el recorrido de bienvenida.** Claude pasó los 9 pasos con el mouse y con Enter y
   cerró con Esc (anduvo); las flechas ← → no pudo probarlas porque su teclado simulado no las
   manda. Probalo vos: Ayuda → «Recorrido del programa» y las flechas.
37. **Tema de cada canción (1.4.0).**
    - Clic derecho en una canción → «Tema de esta canción…»: cambiarla y que no cambie con el tema
      principal.
    - «Volver al tema principal» la vuelve a ligar.
38. **Instalar encima de la versión anterior:** conserva canciones, Biblias y medios, y Ayuda →
    «Buscar actualizaciones…» ofrece la nueva con sus notas.
39. **Lo de la beta.16 que nunca se miró:** la pantalla de carga, el ícono de la barra de tareas,
    la «Palabra de Dios para ti» del instalador y los textos de ejemplo del editor de temas y de
    escenarios.

### Al publicar la 1.5.0 (no es para probar antes)

- **Contador de uso.**
  1. Subir el `worker.js` nuevo y borrar lo contado (pasos en `servicios/api/LEEME.md`, «Empezar
     de cero»).
  2. Con la 1.5.0 **instalada**, proyectar algo: a los minutos tiene que aparecer en el resumen.
     Un instalador de prueba (`1.5.0-prueba.N`) no aparece.
- La página `sitio/cargar-canciones.html` dice «botón ➕»: cambiarlo a «➕ Agregar».

### Ya probado

Para no mezclarlo con lo que falta. Lo probaste vos, salvo donde dice «Claude».

- **Claude, con la prueba.3 abierta (19/11 a la noche)**, con una copia de la biblioteca:
  - YouTube sólo el audio:
    - buscar («Hillsong en español», 20 resultados) y escuchar sin que cambie el proyector;
    - los pads («Re»), y 🔂, que vuelve a empezar al terminar;
    - sumar a la lista y pasar solo al siguiente casi sin silencio;
    - 💾 guardar (arrancó al instante desde la lista);
    - pegar un link (salió con su título).

    Se arregló: los títulos con acentos salían «Espa?ol».
  - YouTube entra silenciado («🔇 Sin sonido» debajo del EN VIVO).
  - Volver a descargar un video que ya estaba en la carpeta: usó el archivo, sin error.
  - La ventana «Cómo preparar una Biblia para importar» (está en el menú **Biblias**) y
    «📋 Copiar instrucciones para una IA» (se copian enteras, en español).
  - Ctrl+K: «juan 3 16», el nombre de una canción y una palabra suelta.
  - El recorrido de bienvenida: los 9 pasos con el mouse y con Enter, y Esc lo cierra.
  - Bajar Biblias:
    - una en español y otra en inglés juntas («Descargar (2)»);
    - al bajar de nuevo una que ya estaba, pregunta una sola vez y queda una sola.
  - Los globitos con la tecla: Black, Clear, Sin fondo y Apagar salida. El de Logo se arregló.

- **1.5.0, prueba.1 y prueba.2 (19/11):**
  - YouTube y descargas: el aviso cuando YouTube no deja pasar un video (hay que descargarlo) y la
    mejor calidad de los descargados.
  - Salida y volumen: un solo volumen y la salida vertical 9:16.
  - Canciones:
    - el orden trabado (también traba Ctrl+Z / Ctrl+Y) y «Crear canción desde cero…» en el menú;
    - el fondo propio con «🎲 al azar por canción»;
    - deshacer después de «Editar como texto plano»;
    - buscar en la letra con «Líneas por diapositiva».
  - Placas:
    - los estilos de placa y «✏ Diseñar…»;
    - las placas en la notebook con proyector, con el aviso al pie;
    - placas con imagen o video (Claude);
    - la Biblia XML de prueba.
  - **Decidido (19/11):** la placa no se ve sobre un YouTube en la salida (el video la tapa; en el
    EN VIVO y en OBS sí se ve). Se deja así.
  - **Decidido:** placa y letra a la vez se deja así; cada uno la acomoda con «Desde abajo» del
    estilo o con el diseño de la letra.
- **Pedidos de GitHub #32 y #37 (Claude, 19/11):**
  - orden trabado y destrabado;
  - Ctrl+Z / Ctrl+Y;
  - buscar en la letra;
  - «➕ Diapositiva»;
  - fondo propio de cada canción;
  - placa en proyector, EN VIVO y OBS, que se va sola.
- **La ventana flotante (Claude, 18/11):**
  - forma, tamaños rápidos, «Siempre visible», maximizada;
  - se acuerda del lugar;
  - YouTube adentro;
  - pasar a pantalla completa y volver.

  También en esa tanda: el formulario de comentarios, «➕ Agregar» y su menú, e importar un XML de
  OpenSong.
- **Transmisión:** Biblia, canciones y anuncios en capas separadas para OBS (28/10).
- **Beta.11 (14/10):**
  - el mini reproductor de Spotify;
  - «mismo título, distinto artista»;
  - las flechas del teclado en Propiedades;
  - el error de una imagen de Google que parpadea (Claude).

## 2. Después de publicar la v1.0.0

**La 1.0.0 salió el 21/10/2026** como versión estable, sin firma:
<https://github.com/EcclesiaCast/EcclesiaCast/releases/tag/v1.0.0>. Además se
armó en esta PC el instalador con Biblias (el último:
`dist\EcclesiaCast-1.0.1-con-biblias-setup.exe`, de la 1.0.1), que lleva
adentro el paquete `Biblias.ecbiblias` del Escritorio (del 16/10), **sólo para
las computadoras de la iglesia**: no se sube a ningún lado. Para armarlo de
nuevo (con otro paquete o con otra versión), después de `dotnet publish`:
`iscc /DAppVersion=<versión> /DPaqueteBiblias="<ruta del .ecbiblias>" installer\EcclesiaCast.iss`.

- Lo que quedaba por probar de la 1.0 (instalar encima, la beta.16) pasó a la lista numerada de §1.
- [ ] **Una prueba que a veces falla en GitHub.** Al publicar la 1.0.1,
      `ReleaseFeedTests.A_server_error_is_a_failure` tardó 17 segundos en la
      máquina de GitHub y dio «sin conexión» en vez de «error del servidor»;
      volviendo a correrla pasó. **Volvió a fallar el 28/10/2026** en el
      chequeo de GitHub del pedido #5 (21 s; pasó al repetirlo): es la que más
      puede frenar una publicación, porque `release.yml` corre las pruebas
      antes de armar el instalador. Acá nunca falló. Si se repite, mirar el
      servidor de prueba (`LocalHttpServer`) con una respuesta 500 sin cuerpo.
      **Arreglado el 30/10/2026** (pedido #13): esas pruebas ahora esperan hasta
      120 s en vez de 15.
- Para las próximas versiones: el flujo de publicación toma las notas de la
  sección `## [<versión>]` del `CHANGELOG.md` (por ejemplo `## [1.0.1] - fecha`)
  y, si no la hay, de `## [Sin publicar]`.
- [ ] **Firma digital del instalador.** Hoy Windows muestra «Editor
      desconocido» y hay que darle a «Más información → Ejecutar de todas
      formas».

      **Estado (15/10/2026): SignPath Foundation rechazó la solicitud.** No
      dijeron nada contra el programa: piden que el proyecto ya tenga
      «visibilidad pública» (estrellas, bifurcaciones, colaboradores, notas de
      prensa, menciones en Reddit, YouTube o Stack Overflow, respaldo de una
      institución) y hoy el repositorio tiene 0 estrellas y 5 descargas. Se
      puede **volver a pedir** cuando haya más. El README ya no dice que el
      instalador está firmado: explica cómo instalarlo igual.

      El flujo de GitHub que firma el programa y el instalador **queda armado y
      apagado** (no hace nada sin las claves), por si algún día se consigue un
      certificado, sea el que sea.

      Opciones que quedan (averiguadas el 15/10/2026):
      1. **Seguir sin firmar y juntar visibilidad** (costo 0). La 1.0 sale sin
         firma, con las instrucciones del README. Para volver a pedir a SignPath:
         un video corto mostrando el programa, contarlo en comunidades de iglesias y
         de tecnología para iglesias, y que otras iglesias lo usen y lo
         marquen con estrella. Nada de estrellas compradas: lo notan.
      2. **Certificado propio de Certum, en la nube** (SimplySign): desde unos
         49 € según su tienda, a **tu nombre** (Windows mostraría tu nombre como
         editor), con un plazo más corto desde marzo de 2026 que hay que mirar
         antes de comprar. Windows igual puede avisar al principio, hasta que el
         programa junta «reputación». Habría que averiguar cómo firmar desde
         GitHub (lo más probable es firmar en tu PC al armar el instalador).
      3. **SignPath con suscripción paga**
         (<https://docs.signpath.io/change-subscription>): no hay precios
         públicos que se vean (los planes dependen de cuántos proyectos), hay que
         pedir cotización, y falta confirmar si el certificado viene incluido o
         hay que comprarlo aparte.
      4. **Azure Artifact Signing** (Microsoft): **no sirve**, sólo da firma a
         personas de Estados Unidos y Canadá y a organizaciones de EE.UU., Canadá,
         la Unión Europea y el Reino Unido.
      5. Sin averiguar: distribuir por la Microsoft Store (Microsoft firma el
         paquete), que cambiaría cómo se instala y se actualiza el programa.

      **Decidido (15/10/2026): la opción 1.** La 1.0 sale sin firma y se junta
      visibilidad (ver «Página web y visibilidad», abajo). La Microsoft Store
      (opción 5) queda como proyecto para después.
- [ ] **Página web y visibilidad** (decidido el 15/10/2026: se sigue sin firma y
      se junta visibilidad). Una página en GitHub Pages que muestre el programa
      (gratis): el encargo completo está en
      [sitio-web-encargo.md](sitio-web-encargo.md). **Estado (15/10/2026):
      publicada en <https://ecclesiacast.org/>, con logo
      nuevo, capturas y un video de 40 segundos; el flujo `pages` la vuelve a
      publicar con cada cambio en `sitio/` y después de cada versión.** **Las capturas reales del aviso de
      Windows («Ejecutar de todas formas») ya están en la página (30/10/2026).**
      Es sólo para la página, no tiene que ver con la firma (para la firma lo
      que falta es visibilidad). Después: un video de
      demostración, contarlo en comunidades de iglesias y que otras iglesias lo
      usen y lo marquen con estrella. Volver a pedir la firma a SignPath cuando
      haya señales.

## Pedidos de permiso para Biblias con derechos (enviados el 15/10/2026)

Alexis pidió permiso para ofrecer estas versiones dentro del programa. Hasta
que haya un sí por escrito, no se incluyen ni se ofrecen para bajar.

**8/11/2026, a la mañana:** Alexis contó que contestaron todas que no. **El
mismo día, más tarde,** dijo que le permitieron ofrecerlas **para bajar desde el
programa, sólo para quien las use**, y que los correos están en una PC de la
iglesia. Hecho: Biblia → **📥 Biblias** → «Bajar Biblias de internet…» lee una
lista (`catalogo.json`) en Cloudflare R2 y baja sólo lo que se marca (ver
[installer/biblias-para-bajar/README.md](../installer/biblias-para-bajar/README.md)).
La lista empieza con 8 Biblias libres (español, inglés, portugués y francés),
listas en el Escritorio, carpeta «Biblias para subir a R2».

- [x] **Cuenta de Cloudflare R2** (8/11/2026): bucket `ecclesiacast-biblias`
      con acceso público en `https://pub-ec2cd8b767e842a2970aa9564a3ea4fb.r2.dev`,
      ya en `AppInfo.BibleCatalogUrl`. Las 8 Biblias y la lista se bajaron de ahí
      con la misma huella SHA-256 que los archivos del Escritorio.
- [ ] **Las con derechos (NTV, NVI, RV1960, TLA)** entran a la lista sólo con el
      correo de cada editorial anotado acá (quién, qué versiones, qué
      condiciones). Claude no las sube sin haber visto el permiso.

| Versión | A quién | Cómo | Respuesta |
|---|---|---|---|
| RVR1960 y TLA | Sociedad Bíblica del Uruguay | info@sociedadbiblica.org.uy | — |
| RVR1960 | American Bible Society | licensing@americanbible.org (sin el formulario adjunto: los datos van en el correo) | — |
| NVI | Biblica, Inc. | formulario en biblica.com (sin las capturas) | Respuesta automática: mínimo 15 días hábiles, no escribir para apurar |
| NTV | Tyndale | permisos@tyndale.com → reenviado a pedidos-tyndale@tyndale.com | Respuesta automática: la persona se jubiló |
| PDT | Bible League International | formulario en bibleleague.org/contact-us | — |
| TCB | Proyecto Evanggelio | jcontreras@ con copia a edarocha@proyectoevanggelio.org | — |

Si para el 5/11/2026 no contestaron: llamar a la Sociedad Bíblica del Uruguay
(2411 0034) y escribir a Proyecto Evanggelio por Facebook o Instagram. A Biblica
no escribirle antes de esa fecha. **5/11/2026: no contestó nadie** (aparte de
las respuestas automáticas de la tabla); toca dar estos pasos.

## 3. Para hacer después de la 1.0

### Pedidos de la prueba de la 1.4.0 prueba.11 (17/11/2026)

- [ ] **Para próximas versiones (pedidos del 17/11/2026):**
      1. **Máximo de caracteres por versículo:** poder poner el tope; si el
         versículo lo pasa, se corta y sigue en la diapositiva que viene.
      2. **Proyectar a una ventana** en lugar de a una pantalla (para quien
         tiene una sola pantalla).
      3. **Mandar la salida principal o la del escenario por wifi** a un
         navegador (como la página que se usa con OBS, pero para ver la
         pantalla entera, en un celular o una tele).
      4. **Cuenta de uso:** hoy sólo se pide el nombre de la iglesia
         (opcional). Alexis no quiere el correo.

- [x] **Ningún video entra silenciado, y el sonido se ve y se controla en el
      LIVE.** (Hecho el 17/11 en la prueba.12; falta que Alexis lo pruebe.) Alexis agregó a Medios → Anuncios
      `presentacion-v3-final.mp4` (9:16, 26 s, con audio AAC) y salió mudo; el
      registro dice «sin sonido». Causa: `GiveSoundToLongVideosAsync`
      (`MainViewModel.Media.cs`) deja mudos todos los videos nuevos salvo los
      que traen sonido y duran 40 s o más (`SoundedVideoMinimum`). **Lo que
      pide Alexis (decisión suya, 17/11):**
      1. **Un video nunca entra silenciado**, dure lo que dure. Silenciar lo
         elige la persona, nunca el programa. (Sacar la regla de «videos
         cortos = fondos sin sonido»; revisar también GIF convertidos y
         YouTube descargados, y qué pasa con los videos ya cargados en la
         biblioteca.)
      2. **Que se vea si un video está silenciado en los controles de
         reproducción del LIVE** (hoy sólo está el 🔇/🔊 de la barra «Fondo en
         pantalla»).
      3. **Que se pueda controlar el volumen de lo que sale de EcclesiaCast**
         desde ahí mismo (volumen del video en pantalla; evaluar también un
         volumen general de la salida).
      Alexis pasa más pruebas antes de hacer los cambios: se hacen todos
      juntos. Hay que cambiar el CHANGELOG de la 1.2.0 («los cortos entran sin
      sonido») y los textos de las pruebas.


> **«Seguir la canción» sale escondida en la 1.4.0.** Para verla: crear un
> archivo vacío `seguir-la-cancion.txt` en `%APPDATA%\EcclesiaCast` y volver a
> abrir el programa. Sus entradas del CHANGELOG están guardadas al final de
> [plan-seguimiento-por-audio.md](plan-seguimiento-por-audio.md).

### Pedidos de la prueba de la 1.4.0 (11/11/2026)

- [x] **«Seguir la canción» no abría ninguna entrada** («Unable to cast
      MMDeviceEnumeratorComObject»). Era un choque dentro del programa: el
      vigía del sonido y el karaoke declaraban el mismo componente de
      Windows. Arreglado (b23aece) y probado en un banco con el código real:
      lista las entradas y escucha. Probado por Alexis el 13/11: ya abre la
      entrada (sigue lo de la lentitud, más abajo).
- [x] **Bajar Biblias dejaba bajar otra vez una que ya estaba.** Ahora las
      saltea. Probado el 13/11: no la baja, pero el aviso iba al pie del
      programa y nadie lo lee. **Desde el 13/11 lo dice en una ventana**
      (también si alguna no se pudo bajar). Falta que lo vea Alexis.
- [ ] **Medios: ninguna pestaña silenciada de entrada.** El programa ya no
      silencia ninguna por su cuenta; en la biblioteca de Alexis «Fondos»
      quedó silenciada (regla guardada `item+mute`), seguramente por un clic
      en «Silenciar la pestaña». Se quita con clic derecho en la pestaña →
      «Quitar el silencio». Decidir si además hace falta un aviso más
      visible de que una pestaña está silenciada.
- [x] **Carteles propios en vez de los de Windows.** Hechos (los 47) y
      probados por Alexis el 13/11: le gusta que quede todo centralizado. Hoy hay 47 `MessageBox`
      con el aspecto gris de Windows. Una ventana de aviso con el aspecto del
      programa (oscura, botones propios, textos de los botones en español
      y no «Sí / No / Cancelar»), y reemplazarlos de a poco. Mediano.
- [ ] **Bajar fondos de Pexels desde el programa.** **Programado** (12/11,
      e626d8c): botón «🎞 Fondos» en Medios y Archivo → «Buscar fondos en
      Pexels…». La clave de Pexels no puede ir en el programa (código
      público): la búsqueda pasa por nuestro servicio `api.ecclesiacast.org`
      (`servicios/api/`, el mismo del contador), que guarda la clave y
      recuerda cada búsqueda un día. Probado con un servicio de prueba y la
      ventana dibujada a imagen; **falta**: que Alexis saque la clave en
      pexels.com/api y publique el servicio (`servicios/api/LEEME.md`), y
      probar la descarga de verdad con el programa abierto.
      **13/11: Pexels dejó de dar claves nuevas** («New API key issuance is
      currently paused»). Se cambia por **Pixabay** (videos e imágenes
      gratis, clave al instante con una cuenta, pide guardar cada búsqueda
      24 h y bajar los archivos en vez de enlazarlos: justo lo que hace el
      servicio). **Hecho el 13/11**: el servicio (`worker.js`, secreto `PIXABAY_KEY`),
      el lector del programa y todos los textos pasaron a Pixabay; la ventana
      dice «Fotos y videos de Pixabay» con enlace. Compila y pasan las
      pruebas. **Falta**: sacar la clave en pixabay.com/api/docs, publicar el
      servicio y probar la descarga de verdad.
- [ ] **Saber cuántas iglesias lo usan por fin de semana.** Alexis eligió el
      contador anónimo (11/11/2026). **Programado**: cuenta la primera
      proyección de cada día (no la apertura), con un interruptor en Ajustes
      → Actualizaciones; el servicio está en `servicios/api/` con sus
      pruebas. **Falta que Alexis lo publique en Cloudflare** siguiendo
      `servicios/api/LEEME.md` (cinco pasos en el panel), y fusionar el
      cambio de la página de privacidad cuando salga la versión. Los dos
      caminos que se plantearon:
      1. *Contador anónimo:* el programa ya pregunta una vez por día si hay
         versión nueva; esa consulta pasaría por un servicio propio (en
         Cloudflare, donde ya están las Biblias) que cuenta números al azar
         de instalación por semana, sin nombres ni datos. Hay que decirlo en
         la página de privacidad y dejar apagarlo. Chico.
      2. *Cuentas:* cada iglesia se registra. Sirve también para la copia
         en la nube que Alexis tiene en mente, pero es mucho más grande
         (servidor, base, inicio de sesión, contraseñas, privacidad, costo).
         Va junto con la copia en la nube, cuando se decida hacerla.

### Prueba del instalador prueba.4 (13/11/2026)

Alexis probó la lista de `docs/pruebas-1.4.0-prueba.md`. Bien: el aviso
pregunta (2), 3, 4, 5, 7, 8 y la búsqueda de fondos (10, con el servicio de
prueba). Lo que quedó:

- [ ] **Seguir la canción anda lento** (1). Con lo que suena en la PC
      reconoce bien la letra, pero tarda: el registro del 12/11 muestra que
      cada pedazo de 8 s tarda unos 2,7 s en pasarse a texto y se escucha
      cada 3 s, así que va unos 6 s atrás; además salta mal a veces (de la 1
      a la 6, de la 10 a la 14) porque decide con pocas palabras. Con el
      micrófono no entendió nada. **Se rehace en una sesión aparte** (rama
      propia) con el modelo más fuerte: usar la placa de video, pedazos más
      cortos, un reconocedor que sólo busque las palabras de la canción y la
      forma de la canción (estrofa, coro) para no saltar. Puede pedir la PC
      para probar.
- [x] **El botón «Cerrar» de Ajustes no cerraba** (6). La ventana se abre
      sin esperar respuesta y el botón no tenía qué hacer. Arreglado el 13/11.
- [x] **Aviso de Biblias que ya estaban** (9): ahora en una ventana (ver
      arriba).
- [x] **Pexels no da claves** (11): se pasó a Pixabay (ver arriba).
- [x] **El menú Archivo tiene demasiadas cosas** (12). Hoy son 16 opciones.
      Propuesta: dejar en Archivo sólo las copias de la biblioteca, Ajustes
      y Salir, y pasar lo demás a tres menús nuevos al lado: **Canciones**
      (buscar en internet, traer de otro programa, importar), **Biblias**
      (bajar de internet, importar una Biblia, cómo prepararla, exportar e
      importar paquete) y **Fondos** (agregar archivos, paquete .zip, bajar
      más fondos, buscar en Pixabay). Otra forma: los mismos tres grupos
      como submenús dentro de Archivo. **Alexis eligió los tres menús (13/11); hecho.** Falta que lo vea.

### Prueba de Claude con la prueba.5 abierta (14/11/2026)

Con una copia de la biblioteca (la real no cambió) y la grabación del 13/11:

- Menús Canciones / Biblias / Fondos / Archivo, «Cerrar» de Ajustes, aviso
  de Biblias ya instaladas, aviso de «Bajar más fondos» (Esc cancela) y
  Pixabay sin servicio: **todo bien**.
- **Seguir la canción, Adelantarse, sonido de la PC, «Bautizados en Fuego»:**
  16 de 18 cambios bien; casi todos entre 4 s antes y 5 s después de que se
  canta, dos tarde (8 y 13 s). Cada pasada 0,5–0,9 s en la placa de video.
  Las dos últimas no las puso porque en la grabación cantan otra cosa ahí.
  Cuando empezó otra canción no se movió.
- [ ] **Micrófono de la notebook: nivel 0,000**, ni ruido. El aviso nuevo
      («No llegó sonido por esta entrada…») salió bien. Puede ser la tecla
      de silenciar micrófono o que Windows no lo deja a programas abiertos
      desde Claude: Alexis lo mira el domingo con el instalado.
- [x] **Detalle:** en Buscar fondos, «La búsqueda de fondos todavía no está
      disponible» sale chiquito abajo y el centro queda vacío: mostrarlo en
      el medio. **Hecho el 14/11** (también «No encontré…»), en la prueba.5.

- [ ] **YouVersion: en pausa** (5/11/2026, Alexis: «queda en el freezer por
      ahora»). La integración está programada y guardada en la rama
      `claude/youversion-integracion`, sin fusionar: necesita que cada iglesia
      saque su clave, y RVR1960, NTV, TLA, DHH y TCB no están en su API. No
      tocarla hasta que Alexis la retome.
      Lo de antes: **escribirle a YouVersion para ver cómo integrar su plataforma**
      (28/10/2026). Contestaron el 1/11/2026: se puede proyectar y transmitir,
      se puede guardar el texto respetando `Cache-Control`, cada iglesia
      registra su propia clave, y el límite es de 200 consultas por hora en la
      beta (10.000 en vivo). La respuesta completa está anotada en esa rama. Lo
      averiguado: YouVersion tiene una **Plataforma para
      desarrolladores** (https://platform.youversion.com, documentación en
      https://developers.youversion.com) con una API y kits de desarrollo
      gratis; se entra con una cuenta, se registra la aplicación y dan una
      clave. Dicen que ofrece más de 1.400 versiones de la Biblia, con
      licencias de las editoriales ya resueltas («Fast-track Licensing»), pero
      cada versión tiene sus reglas de atribución y de uso. **Términos leídos
      el 28/10/2026** (`platform.youversion.com/terms`, versión del 17/8/2026):
      - Es gratis, pero hay que registrarse, aceptar los términos (mayor de 18
        años; persona o entidad) y usar una **clave que no se puede compartir con
        nadie**. Un programa libre en GitHub **no puede llevar una clave
        adentro**: cada iglesia tendría que sacar la suya, o habría que pasar por
        un servidor propio. Es el problema más grande.
      - Hay que **dejar prendido el reporte de uso** que traen sus herramientas
        (mandan datos de uso a YouVersion), o sea que necesita internet.
      - Para las Biblias con derechos, YouVersion **pasa datos del desarrollador
        a las editoriales** para que verifiquen que cumple su licencia.
      - Dejan **distribuir la aplicación** a los usuarios. No dicen nada, en los
        términos generales, de proyectar en pantalla ni de guardar el texto sin
        internet: eso lo fijan los «términos de cada herramienta» (se ven al
        registrarse) y las reglas de cada editorial. **Preguntarlo.**
      - No dejan usar sus cosas para hacer un servicio que **compita** con
        YouVersion, ni usar sus marcas. Pueden cortar el acceso con 30 días de
        aviso, o al instante si creen que se incumplió.
      - Los derechos de los textos bíblicos **no los dan ellos**: salen de las
        editoriales («Fast-track Licensing» las tramita).
      Versiones en la Aplicación de la Biblia (`bible.com`; **no es lo mismo que
      lo que la Plataforma deja usar con una clave**, eso se ve registrándose):
      **español 30** (RVR1960, RVR09, RVR95, RVC, RVA2015, NVI, NVIs, NBV,
      NTV, TLA, TLAI, DHH94 y otras, LBLA, NBLA, PDT, TCB, VBL), **inglés 88**
      (KJV, NIV, ESV, NKJV, NLT, NASB, AMP, MSG, CSB, NET y muchas más) y
      **portugués 17** (ARA, ARC, A21, NAA, NVI, NVT, NTLH, NBV-P, TB y otras).
      Conclusión provisoria: serviría, a lo sumo, como una fuente **opcional
      con internet**, no como reemplazo de las Biblias del programa.
      Contacto: la página tiene «Support»
      (`platform.youversion.com/support`) y «Partners» (`/partners`); los
      términos dan **`platform-support@youversion.com`** (avisos y consultas de
      la plataforma) y **`help@youversion.com`** (comentarios y soporte); el
      `press@youversion.com` es de prensa.
      No escribirles hasta que Alexis lo apruebe.
      **Más averiguado el 30/10/2026** (documentación en `developers.youversion.com`,
      versión en texto en `/llms.txt`): el texto se pide pasaje por pasaje
      (`GET /v1/bibles/{id}/passages/JHN.3.16`) con la clave en el encabezado
      `X-YVP-App-Key`; hay que mostrar siempre el aviso de derechos de la
      versión (`copyright`) y usar sus fuentes y CSS (no se pueden alojar). Cada
      Biblia sólo se puede leer si la organización **aceptó la licencia de esa
      editorial** en el portal (si no, 403). La Plataforma tiene **25
      editoriales y 1.478 Biblias**: Biblica (68), The Lockman Foundation (5),
      Bible League International (1), eBible.org (26), Free Bible Ministry (5),
      Latinamerican Textual Bible Foundation (1), Wycliffe (1.035) y otras.
      **No aparecen** Tyndale (NTV), American Bible Society ni Sociedades
      Bíblicas Unidas (RVR1960, TLA, DHH), aunque la aplicación bible.com sí las
      tiene: probablemente la Plataforma no las trae. Sin clave no se pudo ver
      el listado en español. Los límites de guardar sin internet y de proyectar
      los fija cada licencia (no publicado); el límite de consultas tampoco es
      público (un desarrollador contó un 429 a las ~117 consultas sin
      autenticar). No se encontró ningún programa de proyección para iglesias
      integrado a la Plataforma. Preguntas para YouVersion: ¿se puede proyectar?,
      ¿se puede guardar sin internet?, ¿una sola clave dentro de un programa
      libre o una por iglesia?, ¿qué límite de consultas?, ¿cuáles versiones en
      español estarían disponibles?, ¿cómo se pide que una editorial se sume?
      Borrador (en inglés, porque es a ellos):
      > Hello, I'm Alexis, a volunteer in a church in Uruguay. I'm building
      > EcclesiaCast, a free and open-source program (WPF/.NET 8, on GitHub:
      > https://github.com/EcclesiaCast/EcclesiaCast) to project song lyrics and
      > Bible verses in church services and to feed a livestream (OBS). We'd
      > like to let churches use YouVersion Bibles inside it, especially
      > Spanish versions such as RVR1960, NVI and NTV. Five questions:
      > (1) Do the Platform terms allow a desktop app to show verses on a
      > projector or livestream? (2) May the verse text be cached on the
      > computer, since services often run without reliable internet? (3) Is
      > there a way for a free, non-profit app like ours to be approved as a
      > partner, and what attribution do you require? (4) Since the program is
      > open source and installed on many computers, can one key be distributed
      > with it, or must each church register its own? (5) What is the request
      > rate limit per key? Thank you very much.
- [x] **Botón «Donar» con PayPal** (28/10/2026). **Hecho el 30/10/2026:** el enlace es
      <https://paypal.me/ecclesiacast> (cargado en `AppInfo.DonateUrl` y en la
      web; sale con la próxima versión) y Alexis ya vinculó su tarjeta para retirar.
      **Antes, el lado del programa:**
      Ayuda → «Colaborar con EcclesiaCast…» y una línea en «Acerca de», que
      **no aparecen mientras `AppInfo.DonateUrl`
      ([AppInfo.cs](../src/EcclesiaCast.App/Services/AppInfo.cs)) esté vacío**, así
      nunca hay un botón que no lleva a ningún lado. **Falta:** que Alexis cree
      el enlace de PayPal (paso a paso a pedido; confirmar antes que desde
      Uruguay se puede retirar lo que llegue), pegarlo ahí, y poner el botón en
      la página web (`sitio/`). Reemplaza la idea «Acerca de: una forma de
      colaborar».
- [x] **Saber si alguien usa el programa** (30/10/2026, hecho en la 1.1.1).
      En vez de un contador automático, el programa tiene **Ayuda → Enviar
      comentarios…**, un aviso al cerrar (una vez por versión, con «No volver a
      mostrar», se apaga solo el 30/06/2027) y otro al abrir tras un cierre de
      golpe. Lo que se manda va a un Google Form (respuestas en la planilla de
      Alexis) y el resumen de uso es opcional y se ve antes de enviar. Código en
      `src/EcclesiaCast.Core/Feedback/`; textos en la política de privacidad.
      **Falta:** que Alexis borre la fila de prueba de la planilla, vea la
      ventana en el programa real y mire cuántos comentarios llegan.
- [ ] **Que lo encuentren en Google, por partes** (28/10/2026). 1) ~~Registrar
      la página en Google Search Console y Bing Webmaster y mandar el sitemap~~
      (hecho el 30/10/2026: Bing leyó el sitemap; Google decía «No se ha podido
      leer», es la demora normal de un sitio nuevo: volver a mirar en uno o dos
      días). 2) Un video en YouTube. 3) Páginas cortas para
      lo que la gente busca («programa gratis para proyectar letras en la
      iglesia», «alternativa gratis a ProPresenter / EasyWorship», «letra en
      OBS»). 4) Contarlo en grupos de iglesias. 5) ~~Ver si conviene un
      dominio propio~~ (decidido el 30/10/2026: se queda la dirección de
      GitHub).

- [x] **Más fondos y un botón «Agregar fondos»** (21/10/2026; hecho en la 1.1.0). Ya vienen 10
      fondos en video dentro del instalador (`installer\fondos`, dibujados por
      el generador de esa carpeta; pesan de 2 a 6 MB cada uno). Si se quieren
      más sin engordar el instalador, irían en un paquete aparte que se baja
      desde el programa con un botón «Agregar fondos»: es el mismo mecanismo que
      el catálogo de Biblias libres de abajo, así que conviene hacer los dos
      juntos. Fotos o videos de Pexels o Pixabay **no**: sus licencias no dejan
      repartirlos dentro de otro programa.
      **Hecho el 28/10/2026:** el programa ya abre un paquete .zip de fondos
      (Archivo → Agregar paquete de fondos). **Hecho el 29/10/2026:** el
      paquete está armado, 9 fondos nuevos (luces y nieblas de otros colores) en
      `dist\EcclesiaCast-fondos-extra.zip` (22 MB; se vuelve a dibujar con
      `dotnet run … -- <carpeta> 20 --paquete` en el generador). **Subido el
      29/10/2026** como release `fondos-extra-1` (pre-lanzamiento, sin versión)
      y **Archivo → Bajar más fondos de internet…** lo baja, comprueba su
      SHA-256 (`AppInfo.BackgroundPackSha256`) y lo agrega; probado en una
      copia de prueba, dos veces seguidas (la segunda dice «Ya tenías todos»).
      Un paquete nuevo = release nuevo + dirección y huella nuevas en `AppInfo`.

- [ ] **Biblias: cómo distribuirlas, al menos en Uruguay.** Lo averiguado el
      7/10/2026: las versiones con derechos (RVR1960, NVI, NTV, TLA, y
      también la TCB y la PDT que Alexis quiere sumar) no se pueden repartir con el programa ni desde un
      repositorio, aunque se avise; OpenLP y CrossWire no lo hacen por eso.
      Opciones:
      - Pedir permiso a quien tiene los derechos (Sociedad Bíblica del
        Uruguay, American Bible Society para la RVR1960, Biblica para la NVI,
        Tyndale para la NTV, Liga Bíblica para la PDT). Un mail explicando
        que es un programa libre y gratuito para iglesias.
      - ~~Un botón «Agregar Biblias» con un catálogo de Biblias libres~~
        (https://github.com/seven1m/open-bibles): Alexis (28/10/2026) dice que
        nadie las quiere, las iglesias usan la RVR1960, la NVI y la NTV. En pausa.
      - Cómo lo resuelven otros: G-Presenter trae sólo Biblias libres (KJV, ASV
        y Tagalog) e importa Zefania, igual que nosotros. EasyWorship vende las
        versiones con derechos porque les paga licencias a las editoriales. El
        camino legal sigue siendo el permiso de cada editorial.
      - Una carpeta privada `biblias` junto al instalador, para las PCs de la
        iglesia.
      - **API.Bible** (American Bible Society, mirado el 30/10/2026; Alexis dijo
        «por ahora no hagamos nada»). Catálogo de 412 versiones, 13 en español.
        **No trae RVR1960, TLA, DHH ni PDT.** Libres: Nueva Biblia Viva (Biblica
        Open; a Alexis no le gusta), RVR 1909, Versión Biblia Libre, Palabra de
        Dios para ti y Biblia en español sencillo (las tres del medio ya las
        tenemos). Licencia estándar: NTV, LBLA, NBLA. NVI: sólo licencia única,
        con acuerdo directo con Biblica. Plan gratis: sin fines comerciales,
        todas las libres + hasta 3 con derechos a elección, 5.000 consultas por
        mes por cuenta. Piden: renovar el texto guardado cada 30 días, no más
        de 500 versículos seguidos, poner la sigla de la versión. Cada iglesia
        tendría que crear su cuenta y pegar su clave (una clave nuestra
        compartida se gastaría y quedaría pública en el código). Duda: el botón
        «Colaborar» de PayPal frente a «sin fines comerciales» (su guía acepta
        enlaces chicos de donación; confirmarlo con ellos). Si se hace, sería
        una opción para quien quiera, no el camino principal. Retomar junto
        con las respuestas por RVR1960 (5/11/2026).

### Prueba de la prueba.5 en la iglesia (15/11/2026)

Alexis probó la lista entera y casi todo dio bien. Registro de la PC de la
iglesia (11:22–12:53): ningún error ni cierre.

- [x] **Seguir la canción volvía atrás lo que puso el operador** (12:43: puso
      la 8 y a los 4 s volvió a la 6 porque seguían con «Gracias mi Señor
      Jesús»). Ahora no vuelve detrás de la diapositiva que puso el operador
      hasta oír que se canta.
- [x] **La cuenta de uso probó 104 veces** sin internet: ahora espera una hora.
- [x] **Esc vuelve a apagar con un solo toque** (lo pidió Alexis).
- [x] **Editar el diseño de la transmisión lo cambiaba en vivo:** ahora
      espera a «Guardar».
- Datos de Seguir la canción: IN 2 de la Behringer no traía sonido (nivel 0);
  con IN 1 entendió bien. En automático pasó solo unas 20 diapositivas, con
  unas 5 correcciones a mano. Tres retrocesos por coros repetidos.
- [x] **Logo de video negro con F3.** El video se reproducía detrás de la
      vista proyectada, y el color del tema (o una imagen de fondo) lo tapaba:
      sólo se veía con un video de fondo. Ahora sus cuadros se dibujan en la
      capa del logo. Probado con el banco de render (cuadro falso); **falta
      verlo con un video de verdad en el proyector.**
- [x] **Imagen de Google que quedaba encima del logo:** era lo mismo (el
      logo era el de video). Para sacar un fondo: «Sin fondo» (F4).
- [x] **Degradé desde el borde (el de «Clásica») en cualquier diseño de la
      transmisión:** casilla nueva debajo de las placas.
- [x] **Logo de video: bordes con una imagen fija del video,** desenfocada.
- [ ] **Se cortó la salida de sonido de la PC** al terminar (hubo que
      desconectar y conectar, 12:53). El programa llevaba 10 minutos sin
      escuchar ni reproducir nada, y «Seguir la canción» no toca el volumen
      ni la salida. Sin indicios de que sea del programa: avisar si se repite.
- No se pudo probar: si el video de fondo se traba mientras escucha.

## 4. Para versiones futuras

- **Importar Biblias de e-Sword (.bblx)** (pedido del formulario, 20/11/2026). Que cada iglesia
  pueda traer las Biblias que ya tiene en e-Sword. Ojo: muchas de esas versiones tienen derechos
  de autor, así que **no se ofrecen para bajar** en «Bajar Biblias»; sólo las importa cada uno
  con su archivo, como pasa hoy con el JSON y el XML.
- **Versión para Mac** (la piden en Instagram; Alexis quiere hacerla,
  14/11/2026). Es el trabajo más grande que queda:
  - **Qué se aprovecha:** `EcclesiaCast.Core` y `EcclesiaCast.Data` (unas
    24.000 líneas: canciones, Biblias, playlists, base SQLite, control remoto)
    ya son .NET común y funcionan en Mac sin cambios.
  - **Qué hay que rehacer:** la interfaz entera (`EcclesiaCast.App`, unas
    42.000 líneas y 43 ventanas y controles) está hecha con WPF, que sólo
    existe en Windows. El camino es pasarla a **Avalonia**, que se escribe casi
    igual que WPF y funciona en Windows, Mac y Linux. Hacer una sola versión en
    Avalonia para los dos sistemas, no mantener dos interfaces.
  - **Piezas sólo de Windows que necesitan reemplazo:** el video (LibVLC tiene
    versión para Mac y para Avalonia), WebView2 (páginas web, YouTube,
    musica.com: en Mac no existe), y capturar ventanas, teclas multimedia,
    miniaturas de archivos, barra de título oscura, ahorro de energía y la
    lista de pantallas (`Services/` del programa).
  - **Orden propuesto:** primero un prototipo en Avalonia que muestre una
    canción y un versículo en la pantalla del proyector y pase un video de
    fondo, probado en una Mac de verdad. Si eso funciona bien, pasar el resto
    por partes, cuidando que la versión de Windows no empeore.
  - **Cómo probarlo:** GitHub tiene Macs en la nube (gratis para repositorios
    públicos) para compilar, correr las pruebas y sacar capturas de cada
    pantalla; sirve para todo menos el proyector. Para la prueba de verdad
    hace falta una Mac física con proyector o TV: prestada, o de alguno de los
    que la piden en Instagram como probador. Armar Mac con chip Apple
    (arm64) y Mac con Intel (x64).
  - **Instalación en Mac:** sin firma de Apple (cuenta de desarrollador de
    Apple, paga por año) la Mac bloquea el programa y hay que habilitarlo a
    mano en Ajustes del Sistema → Privacidad y seguridad → «Abrir igual».
    Explicarlo en la página con capturas, como el «Ejecutar de todas formas»
    de Windows.
- **Ideas tomadas de G-Presenter** (gpresenter.com, mirado el 28/10/2026;
  Alexis dijo que sí a las seis, para más adelante):
  1. **Cámara o capturadora como diapositiva**, o de fondo detrás de la letra
     o de la cuenta regresiva (sin sonido hasta que el operador lo prenda).
  2. **Mensajes al escenario** que escribe el operador en el momento
     («Próxima canción», «Oración»), aparte de las notas.
  3. **Cualquier pantalla extra desde un navegador**: una TV o tablet de la
     red local abre una dirección y muestra lo mismo que el proyector, sin
     cables. Las páginas de `/salida` ya son media parte.
  4. **Agrandar o achicar la interfaz**, para notebooks de pantalla chica.
  5. **Exportar canciones a .txt** (hoy sólo se importan).
  6. **Otros idiomas** de la interfaz; el primero sería el portugués (Brasil).
- **Una salida como ventana flotante** (pedido de Alexis, 17/11/2026). Hoy la
  salida para la transmisión es una dirección (Transmisión → «Salida para OBS
  o vMix…») que se pega en OBS. Sumar la opción de que una salida se abra
  como **ventana flotante** en la PC: una ventana común, que se puede mover,
  agrandar y poner en cualquier pantalla, sin ocupar la pantalla entera
  como el proyector. Sirve, por ejemplo, para capturarla con OBS como ventana,
  verla en un monitor chico o tenerla al lado del programa. Pensar al
  hacerlo: qué salida muestra (la del proyector o una de la transmisión:
  Biblia, canciones, anuncios), si la ventana queda siempre encima, y que
  recuerde su lugar y su tamaño de un servicio al otro.
- **NDI para OBS: no sirve para lo de arriba.** NDI manda una imagen (como una
  cámara), con la letra ya dibujada: en OBS no se le puede cambiar la letra ni
  el tamaño. Las salidas separadas ya están como páginas web (`/salida/biblia`,
  `/salida/canciones`, `/salida/anuncios`). NDI de verdad, si algún día hace
  falta para otra cosa, necesita el kit de NDI (con sus condiciones de licencia)
  y el complemento de NDI en el OBS.

## 5. Decisiones tomadas


Descartado (28/10/2026, vuelto a mirar el 1/11/2026 sin novedades: no hay API pública ni condiciones que lo permitan): agregar letras.com a la búsqueda de canciones. Le niega
el acceso a todo lo que no sea un navegador (403) y sus condiciones prohíben
eludir medidas de seguridad. La búsqueda sigue con musica.com primero y LRCLIB de
respaldo. Si algún día se quiere, la salida limpia es pedirles permiso por
escrito (Vignoli Comunicação Ltda.).
Descartado (15/10/2026): bajar una presentación de Google Slides pegando su
enlace. Quien lo necesite descarga el PDF y lo agrega (los PDF y PowerPoint ya
se importan), o abre Canva o Google Slides en el panel **🌐 Web** con la sesión
iniciada y los proyecta como foto o en vivo.

Buscador de imágenes y GIF (14/10/2026): se hace con **Google Imágenes dentro
del panel web**, sin API ni claves. Se descartó Tenor (Google cerró su API el
30/6/2026), la API de Google (cerrada a clientes nuevos y sin imágenes como
las de Google Imágenes) y Giphy (100 búsquedas por hora para todas las
iglesias juntas con una clave compartida, o una clave por iglesia). Google
entiende el español, así que tampoco hace falta traducir.

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
