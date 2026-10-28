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
- [ ] **Menú «Transmisión» y estilos propios para OBS** (28/10/2026; hay
      compilación, pruebas y dibujos a imagen de las placas, pero no se abrió
      desde acá). Plan en [plan-obs-estilos.md](plan-obs-estilos.md). Probalo con
      OBS abierto, en este orden:
      1. **El menú.** Entre «Proyección» y «Ayuda» está **Transmisión**, con
         «Salida para OBS o vMix…» y «Estilos de la transmisión…». La ventana
         del celular ya no trae la caja de OBS: sólo dice que ahora está en el
         menú Transmisión.
      2. **Salida para OBS o vMix.** Abre con la dirección general y los
         botones Copiar: Biblia / Canciones / Anuncios, igual que antes, más
         «🎨 Editar los estilos…». Si el control del celular estaba apagado, se
         prende solo. Pegá la dirección de Canciones en un Navegador de OBS
         (1920 × 1080) arriba de la cámara.
      3. **El estilo de cada capa.** Arriba de «Estilos de la transmisión…» hay
         seis plantillas listas: tocá cada una y mirá que cambie la vista previa
         sin mover la posición ni las mayúsculas; retocá algo (el tamaño) y la
         plantilla deja de verse marcada; «Volver a los de fábrica» la marca de
         nuevo como Moderna. Después elegí
         Canciones, placa **Pestaña**, posición **Inferior**, y Guardar. Proyectá
         una canción: la letra sale en mayúsculas dentro de una placa oscura con
         una línea turquesa abajo y la pestañita «CORO» arriba. Al pasar de una
         diapositiva a otra hay un fundido corto, y una estrofa larga se achica
         sola en vez de salirse de la pantalla. En OBS cambia a lo sumo 0,3 s
         después de guardar, sin tocar nada.
      4. **Las ocho placas.** Probá cada una en la vista previa y en OBS: Ninguna,
         Pestaña, Vidrio (se ve la cámara borrosa detrás), Sólida, Degradé,
         Halo, Filete (línea fina que se desvanece) y Línea (vertical a la
         izquierda). Probá también Superior y Centro, y el ancho y el margen.
      5. **Biblia y Anuncios.** Proyectá un versículo: arriba de la placa va la
         cita («JUAN 3:16 · RVR1909») y no aparece aparte. Con un texto rápido,
         la placa de Anuncios sale en el centro. Con «Mostrar la etiqueta»
         destildada no sale ni la cita ni «CORO».
      6. **Fondo fijo.** En una capa elegí «Color» y después «Imagen o video…»
         con un video: tiene que verse siempre mientras la página esté prendida,
         moverse en bucle y sin sonido, y desaparecer con Black (F1), Clear (F2)
         y Logo (F3). Un video grande no tiene que trabarse al pasar de una
         diapositiva a otra.
      7. **«Igual que en el proyector».** Tildala en una capa: esa capa vuelve a
         verse como hasta ahora (con el tema de la diapositiva) y el resto de
         los controles se apagan. «Volver a los de fábrica» sólo cambia la
         pestaña abierta, y Cancelar no guarda nada.
      8. **Si WebView2 no está**, la vista previa dice «No se pudo mostrar la
         vista previa» y lo demás anda igual.
- [x] **Biblia, canciones y anuncios en capas separadas para OBS.** Alexis lo
      probó en OBS el 28/10/2026: llega bien. Ahora se le hace un editor de
      estilos propio (el punto de arriba, «Menú Transmisión y estilos propios»).
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

- [ ] **Fondos en video** (salieron en la 1.0.1, el 21/10/2026; hay pruebas,
      pero no se instaló ni se vio andar). En el instalador, la casilla «Agregar 10
      fondos en video» viene tildada. Al abrir el programa: pestaña nueva
      **Fondos de ejemplo** con los 10, en orden alfabético, con su miniatura, y
      la barra de abajo lo avisa. Proyectá varios con una canción encima: que la
      letra se lea bien, que no tengan sonido y que **el bucle no se note** (mirá
      el momento en que vuelve a empezar, a los 20 segundos). Mirá también que no
      tironeen en la PC del proyector. Quitá uno de la biblioteca, cerrá y abrí:
      no vuelve. Instalando sin la casilla, la pestaña no aparece.
- [ ] **Salir al aire sólo en la transmisión, sin ventana de salida**
      (28/10/2026; hay pruebas de la lista de pantallas, no se abrió el programa).
      Probalo con OBS abierto y, si podés, con el proyector prendido:
      1. En **Salida:** (arriba a la derecha) la lista tiene una opción nueva al
         final, **📡 Sólo la transmisión (sin ventana)**. Elegila: el botón pasa a
         decir «📡 Salir al aire en la transmisión».
      2. Poné en vivo una canción o un versículo (o apretá el botón). **No tiene
         que abrirse ninguna ventana** en ninguna pantalla. En la barra de abajo
         tiene que aparecer, en rojo, «📡 EN VIVO SÓLO EN LA TRANSMISIÓN · sin
         ventana de salida», y el botón pasa a «Apagar la transmisión».
      3. En OBS, las páginas de `/salida` (la general, Biblia, canciones y
         anuncios) tienen que mostrar la letra y el versículo. Probá también
         **Black (F1)**, **Clear (F2)** y **Logo (F3)**: la transmisión se queda sin
         letra, como cuando proyectás. El LIVE del programa, el escenario y el
         celular tienen que andar igual.
      4. Con la ventana ya abierta en el proyector y en vivo, cambiá la lista a
         «Sólo la transmisión»: la ventana tiene que bajar y la transmisión
         seguir. Al volver a elegir la pantalla, la ventana vuelve a abrirse ahí.
      5. **Esc** o «Apagar la transmisión» apaga todo y la barra dice
         «Transmisión apagada». Cerrá y abrí el programa: tiene que volver a la
         pantalla de siempre, **no** a «Sólo la transmisión» (no se recuerda a
         propósito, para no quedarte sin proyector un domingo).
      6. Lo que no anda en este modo: un **video de fondo** o una página web
         proyectada se mueven sólo en la ventana de salida; la página de anuncios
         de la transmisión sí reproduce los videos de la biblioteca por su cuenta.
- [ ] **Etiqueta de los versículos en la transmisión** (28/10/2026; hay pruebas
      de lo que se guarda, no se vio dibujada). **Transmisión → Estilos de la
      transmisión…**, capa Biblia (y las otras): bajo «Mostrar la etiqueta» hay un
      deslizador **Etiqueta** (50 a 300 %, 100 es el tamaño de siempre), tres
      botones **Izquierda / Centro / Derecha** y **Arriba / Abajo de la letra**.
      Mirá en la vista previa que «Juan 3:16 RVR 1960» cambie de tamaño y de
      lugar con cada placa (Pestaña, Vidrio, Sólida, Línea…), que abajo de la
      letra la pestaña quede pegada a la placa, y que en OBS se vea igual.
- [ ] **Líneas por diapositiva en las canciones** (28/10/2026; hay pruebas de
      cómo se parte, no se vio en el programa). Con una canción abierta, arriba
      de la grilla aparece **Líneas por diapositiva: Todas / 1 / 2 / 3 / 4**.
      Probalo con «Ardiendo en fuego»: con 2, el párrafo de cinco líneas queda en
      tres diapositivas (2 + 2 + 1). Mirá que la canción, abierta en el editor,
      siga con el párrafo entero; que cambiar el valor con una canción en vivo la
      vuelva a poner; que al cambiar de canción siga el mismo valor; que se
      recuerde al cerrar y abrir; y que en la transmisión y el escenario salgan
      las mismas partes. La etiqueta (Coro, Verso 1) se repite en cada parte.
- [ ] **Importar de EasyWorship** (28/10/2026). **Archivo → Importar canciones de
      EasyWorship…**: elegís `Songs.db` (`SongWords.db` tiene que estar al lado).
      Se probó (28/10/2026) con la biblioteca de EasyWorship 6.1 que Alexis
      instaló en esta PC, con 2 canciones: la lee bien, con las diapositivas
      separadas. El selector se abre solo en la carpeta de EasyWorship. Falta
      probarlo con una biblioteca grande y con EasyWorship 7 u 8 (en esta PC hay
      también datos de una v8, pero sin canciones). Mirá que importe todas, con
      título, autor y derechos, y que las repetidas pregunten como las demás.

## 2. Después de publicar la v1.0.0

**La 1.0.0 salió el 21/10/2026** como versión estable, sin firma:
<https://github.com/EcclesiaCast/EcclesiaCast/releases/tag/v1.0.0>. Además se
armó en esta PC el instalador con Biblias (el último:
`dist\EcclesiaCast-1.0.1-con-biblias-setup.exe`, de la 1.0.1), que lleva
adentro el paquete `Biblias.ecbiblias` del Escritorio (del 16/10), **sólo para
las computadoras de la iglesia**: no se sube a ningún lado. Para armarlo de
nuevo (con otro paquete o con otra versión), después de `dotnet publish`:
`iscc /DAppVersion=<versión> /DPaqueteBiblias="<ruta del .ecbiblias>" installer\EcclesiaCast.iss`.

- [ ] **Comprobar la 1.0 instalada** (lo de «Después de publicar» de
      [publicar-1.0.md](publicar-1.0.md), salvo la firma): instalarla **sobre**
      una beta y ver que conserva canciones, Biblias y medios; con una beta
      instalada, que Ayuda → «Buscar actualizaciones…» ofrezca la 1.0 con las
      notas nuevas en castellano; y el instalador con tus Biblias en una PC de
      la iglesia (que al abrir el programa ofrezca traerlas).
- [ ] **Usar la 1.0 en la reunión del domingo** y anotar lo que falle o no
      guste.
- [ ] **Una prueba que a veces falla en GitHub.** Al publicar la 1.0.1,
      `ReleaseFeedTests.A_server_error_is_a_failure` tardó 17 segundos en la
      máquina de GitHub y dio «sin conexión» en vez de «error del servidor»;
      volviendo a correrla pasó. Acá nunca falló. Si se repite, mirar el
      servidor de prueba (`LocalHttpServer`) con una respuesta 500 sin cuerpo.
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
      publicada en <https://ecclesiacast.github.io/EcclesiaCast/>, con logo
      nuevo, capturas y un video de 40 segundos; el flujo `pages` la vuelve a
      publicar con cada cambio en `sitio/` y después de cada versión.** Falta también la captura de «Ejecutar
      de todas formas» del aviso de Windows (hoy la página explica ese paso sin
      imagen); es sólo para la página, no tiene que ver con la firma (para la
      firma lo que falta es visibilidad). Después: un video de
      demostración, contarlo en comunidades de iglesias y que otras iglesias lo
      usen y lo marquen con estrella. Volver a pedir la firma a SignPath cuando
      haya señales.
- [ ] **Probar lo que llegó con la beta.16, ahora en la 1.0**: la pantalla de
      carga nueva (se dibujó a imagen, pero no se vio abrir de verdad), el ícono
      nuevo en la barra de tareas y el instalador con la «Palabra de Dios para
      ti» (que aparezca entre las Biblias y que entre al abrir el programa). Los
      textos de ejemplo del editor de temas y de escenarios ahora son libres:
      mirar que se vean bien.

## Pedidos de permiso para Biblias con derechos (enviados el 15/10/2026)

Alexis pidió permiso para ofrecer estas versiones dentro del programa. Hasta
que haya un sí por escrito, no se incluyen ni se ofrecen para bajar.

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
no escribirle antes de esa fecha.

## 3. Para hacer después de la 1.0

- [ ] **Escribirle a YouVersion para ver cómo integrar su plataforma**
      (28/10/2026). Lo averiguado: YouVersion tiene una **Plataforma para
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
- [ ] **Botón «Donar» con PayPal** (28/10/2026), en la página web y en el
      programa (Ayuda → «Colaborar con EcclesiaCast» y en «Acerca de»; reemplaza
      la idea «Acerca de: una forma de colaborar»). Cuando se haga, darle a
      Alexis el paso a paso para crear el enlace de PayPal (la cuenta la crea
      él) y confirmar antes que desde Uruguay se puede retirar lo que llegue.
- [ ] **Saber si alguien usa el programa** (28/10/2026, falta decidir cómo).
      Lo respetuoso: al abrir por primera vez, preguntar «¿Nos dejás contar que
      usás EcclesiaCast?» y, si dice que sí, mandar una vez por día sólo un número
      al azar, la versión y el país, nada personal. Hay que anotarlo en
      [privacidad.md](privacidad.md) y en la web. Sin preguntar, lo único que
      hay son las descargas de GitHub.
- [ ] **Que lo encuentren en Google, por partes** (28/10/2026). 1) Registrar
      la página en Google Search Console y Bing Webmaster y mandar el sitemap
      (con la cuenta de Alexis). 2) Un video en YouTube. 3) Páginas cortas para
      lo que la gente busca («programa gratis para proyectar letras en la
      iglesia», «alternativa gratis a ProPresenter / EasyWorship», «letra en
      OBS»). 4) Contarlo en grupos de iglesias. 5) Ver si conviene un dominio
      propio (unos 10–12 dólares por año).

- [ ] **Más fondos y un botón «Agregar fondos»** (21/10/2026). Ya vienen 10
      fondos en video dentro del instalador (`installer\fondos`, dibujados por
      el generador de esa carpeta; pesan de 2 a 6 MB cada uno). Si se quieren
      más sin engordar el instalador, irían en un paquete aparte que se baja
      desde el programa con un botón «Agregar fondos»: es el mismo mecanismo que
      el catálogo de Biblias libres de abajo, así que conviene hacer los dos
      juntos. Fotos o videos de Pexels o Pixabay **no**: sus licencias no dejan
      repartirlos dentro de otro programa.

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

## 4. Para versiones futuras

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
- **La Biblia y las canciones como texto sin formato para OBS** (lo pidió
  Alexis el 20/10/2026; quedó en pausa para verlo bien después). Quiere que el
  versículo y la letra que se están proyectando lleguen a OBS como texto plano,
  o con un formato mínimo, para acomodarlos como quiera. Hoy `/salida/biblia` y
  `/salida/canciones` mandan texto, pero con el formato del tema (tipografía,
  tamaño, color, contorno y alineación), así que en OBS se ven igual que en el
  proyector. Dos formas de hacerlo (se pueden hacer las dos; falta saber cuál va
  a usar primero):
  1. **Archivo de texto para la fuente «Texto (GDI+)» de OBS**, con «Leer desde
     archivo»: el programa escribe `biblia.txt` y `canciones.txt` con lo que está
     al aire y los vacía con Clear, Black y Logo. El estilo se elige con los
     menús de OBS. Contras: OBS tiene que estar en la misma computadora, y el
     tamaño de letra queda fijo (un versículo largo no se achica solo como en el
     proyector).
  2. **Las mismas páginas «sin formato»** (por ejemplo `&formato=no`): sólo el
     texto blanco, sin el tema, y el estilo se pone en «CSS personalizado» del
     Browser Source, con unas líneas listas para copiar en la guía. Sirve con OBS
     en otra computadora, por la red. Contra: el estilo se cambia tocando esas
     líneas, no con menús.

  En las dos, la cita del versículo puede ir en una línea aparte.
- **NDI para OBS: no sirve para lo de arriba.** NDI manda una imagen (como una
  cámara), con la letra ya dibujada: en OBS no se le puede cambiar la letra ni
  el tamaño. Las salidas separadas ya están como páginas web (`/salida/biblia`,
  `/salida/canciones`, `/salida/anuncios`). NDI de verdad, si algún día hace
  falta para otra cosa, necesita el kit de NDI (con sus condiciones de licencia)
  y el complemento de NDI en el OBS.

## 5. Decisiones tomadas


Descartado (28/10/2026): agregar letras.com a la búsqueda de canciones. Le niega
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
