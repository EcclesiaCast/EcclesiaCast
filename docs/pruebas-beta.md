# Guía de pruebas de la beta

Lista para probar EcclesiaCast en la PC del proyector, ordenada por lo que más
importa. Marcá lo que vaya saliendo bien y anotá lo que falle.

Si algo falla, anotá **qué estabas haciendo, qué esperabas y qué pasó**, y
guardá el archivo de registro más nuevo de `%APPDATA%\EcclesiaCast\logs`.

> **Cómo leer esta lista.** Las secciones 1 y 2 son cosas que **nadie probó
> nunca**: ahí es donde van a aparecer los problemas. La sección 3 son cosas
> que ya vi funcionar en mi máquina y sólo hay que confirmar con tu equipo.

---

## 1. Lo que nunca probó nadie: tu PC y tu proyector

Esto sólo se puede verificar en la PC del proyector. Es lo más importante.

### Instalación desde cero

> **Ojo con la versión.** Lo que hay que probar es la **beta.4**, que todavía
> **no está publicada en GitHub**: la página de releases sigue mostrando la
> beta.2 del 20/7, que no trae nada de esto. El instalador de la beta.4 se
> genera en la máquina de desarrollo y queda en la carpeta `dist` del
> proyecto:
>
> ```
> dist\EcclesiaCast-1.0.0-beta.4-setup.exe
> ```
>
> Se recompila cuando haga falta con:
>
> ```
> dotnet publish src\EcclesiaCast.App\EcclesiaCast.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:Version=1.0.0-beta.4 -o publish
> "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.4 installer\EcclesiaCast.iss
> ```
>
> Se publica en GitHub recién **después** de esta prueba: mergear `beta3` a
> `main` y taguear `v1.0.0-beta.4` dispara el workflow de release.

- [ ] Copiar `dist\EcclesiaCast-1.0.0-beta.4-setup.exe` a la PC del proyector.
- [ ] Confirmar que estás instalando la **beta.4** y no la beta.2 ni el
      `EcclesiaCast-1.0.0-setup.exe` viejo que también está en `dist`.
- [ ] Windows va a avisar que el programa no tiene firma digital ("Windows
      protegió tu PC"). Es esperable: hay que elegir *Más información* →
      *Ejecutar de todas formas*. **Anotá si el aviso asusta o confunde.**
- [ ] Aceptar el cartel de permisos (UAC) e instalar.
- [ ] Si esa PC no tiene WebView2, el instalador lo descarga e instala solo.
      Necesita internet. **Verificá que no se cuelgue ni tire error.**
- [ ] Queda el acceso directo con el ícono nuevo y la app abre.
- [ ] La versión instalada es la correcta: clic derecho en
      `EcclesiaCast.App.exe` → Propiedades → Detalles tiene que decir
      **1.0.0-beta.3**.

> Instalar encima de la beta.2 **conserva tus canciones, Biblias, temas y
> medios**: la base vive aparte, en `%APPDATA%\EcclesiaCast`. Al primer
> arranque corren tres migraciones (contorno de letra, desenfoque de medios y
> la tabla de logos). Si querés dormir tranquilo, copiá
> `%APPDATA%\EcclesiaCast\ecclesiacast.db` antes de instalar.

### El proyector

- [ ] Conectar el proyector y abrir la app.
- [ ] En "Salida" aparece el proyector con su resolución real.
- [ ] "Poner en vivo" proyecta, y `Esc` apaga la salida.
- [ ] Si cambiás de pantalla en la lista, la salida se muda sin cerrarse.
- [ ] **El teclado no se lo roba la salida**: con algo proyectado, escribí en el
      buscador de canciones. Las letras tienen que ir al panel del operador.

### Rendimiento con video (lo que más me preocupa)

- [ ] Proyectar un video de fondo con la letra de una canción encima.
- [ ] Mirar si va **fluido o a tirones**.
- [ ] Abrir el Administrador de tareas y anotar cuánto **CPU y memoria** usa.
- [ ] Repetir **con desenfoque puesto** (el control nuevo de la barra de
      medios). El desenfoque es trabajo extra para la placa de video: si el
      video va bien pero con desenfoque va a tirones, anotalo.

> En mi máquina daba medio núcleo de CPU y unos 810 MB de memoria, pero es una
> máquina de 16 núcleos. En una PC más modesta puede pesar bastante más. **Si va
> a tirones, avisame y bajo el video a 720p**, que es la solución prevista.

### YouTube

Esto sólo lo podés hacer vos, porque va con tu cuenta.

- [ ] Iniciar sesión con la cuenta Premium de la iglesia en el navegador
      integrado (botón "YouTube"). Se hace **una sola vez** y queda guardada.
- [ ] Proyectar un video y confirmar que **no salen anuncios**.
- [ ] **Apagar los subtítulos en la cuenta de YouTube**: Configuración →
      Subtítulos → desactivar "Mostrar siempre los subtítulos". Si no, se
      proyectan encima del video. Verificado que no se pueden apagar desde el
      programa: es una preferencia de la cuenta.
- [ ] Cerrar y volver a abrir EcclesiaCast: la sesión tiene que seguir puesta.

---

## 2. Lo nuevo, que tampoco probó nadie

Todo esto se escribió después de tus comentarios y **está sin verificar en
pantalla**: compila y la app abre sin errores, nada más. Está ordenado por
dónde creo que es más probable que algo falle.

### Importar de ProPresenter

Es la operación más pesada de todo lo nuevo: en tu máquina hay **271 canciones**
en *Canciones PDV*, más *Canciones Convención* (24), *Cuna* (12), *Biblia* (6),
*Musica* (3) y *Default* (2).

- [ ] Botón **🅿** en el panel de Canciones.
- [ ] Verificá que la lista muestre esas bibliotecas con esos números. Las
      encuentra en la carpeta de **OneDrive**, no en Documentos.
- [ ] Importar todas y esperar. **Anotá cuánto tarda** y si la ventana queda
      congelada o va mostrando el progreso.
- [ ] Abrir varias canciones importadas y revisar que **la letra esté completa y
      bien separada** en diapositivas. Si alguna quedó vacía o cortada, decime
      cuál y guardá ese `.pro`.
- [ ] Volver a importar lo mismo: no tiene que duplicar nada.

### Logos

- [ ] Flecha **▾** al lado de *Logo (F3)* → *Administrar logos…*.
- [ ] Armar uno con **la imagen de tu iglesia** y proyectarlo con F3.
- [ ] Armar uno de **texto libre** y ver que se lee bien de lejos.
- [ ] Armar uno de **video en bucle** y proyectarlo. Esto es lo que más partes
      móviles tiene: **mirá que el video se vea, que dé la vuelta sin cortes, y
      que el aviso al pie siga apareciendo por encima del logo.**
- [ ] Tener dos o tres logos y **cambiar de uno a otro con la flecha** mientras
      está proyectado.
- [ ] Botón **🖼▦**: poner el logo de fondo, detrás de la letra de una canción.
      Ajustar el desenfoque y ver que la letra se lea.
- [ ] Cerrar y abrir la app: tiene que acordarse de cuál logo estaba elegido.

### Pantalla de escenario

Necesita **tres pantallas**: la del operador, el proyector y el monitor de
escenario. Si no tenés tres, probalo igual usando la pantalla de la notebook
como escenario y sin proyector.

- [ ] Botón **🎭 Escenario**, elegir el monitor con la flecha **▾**.
- [ ] Que no te deje elegir **la misma pantalla que la salida** (tiene que
      avisarte).
- [ ] Proyectar una canción: en el escenario se ve la letra actual y **la que
      sigue**.
- [ ] Apretar **F1 (Clear)** y **F2 (Black)**: la congregación deja de ver la
      letra, pero **el escenario la sigue mostrando** y avisa cuál está puesto.
      Esto es lo más importante de esta pantalla.
- [ ] La hora y el cronómetro andan; **⟲** pone el cronómetro en cero.
- [ ] Poner un aviso al pie y ver que también aparece en el escenario.
- [ ] Probar los tamaños de letra desde el menú **▾**.

### Control desde el celular

Probado con la computadora hablándose a sí misma, **nunca desde un teléfono**.

- [ ] Botón **📱**. La primera vez, **Windows va a preguntar si permitís el
      acceso a la red**: aceptá para *redes privadas*. Anotá si el cartel
      aparece y qué dice.
- [ ] Escanear el **QR** con la cámara del celular, estando en la misma wifi.
- [ ] Poner el **PIN** de cuatro dígitos.
- [ ] Probar: **◀ ▶**, Clear, Black, Logo, Sin fondo, prender y apagar la
      salida, tocar una diapositiva suelta y un ítem de la playlist.
- [ ] Ver que la pantalla del celular **se actualiza sola** cuando alguien toca
      algo en la computadora.
- [ ] Probar con **dos celulares a la vez**.
- [ ] Alejarte hasta donde estaría quien predica: ¿llega la wifi? ¿cuánto tarda
      en responder?
- [ ] Cerrar la app y volver a abrirla: el control remoto tiene que **arrancar
      solo** (queda encendido de una vez para la otra).

### Video: barra de reproducción, desenfoque y apagado

- [ ] Con un video de fondo, usar **⏮ / ⏪10s / ▶⏸ / 10s⏩** y **arrastrar la
      barra de progreso**. Que el tiempo mostrado sea el real.
- [ ] Lo mismo **con un video de YouTube** proyectado (va por otro camino
      distinto por dentro).
- [ ] **Apagar la salida con un video andando**: el video tiene que **pausarse**
      (no seguir sonando) y **retomar donde estaba** al volver a prenderla.
      Esto era un bug tuyo y es lo que hay que confirmar.
- [ ] Mover el **desenfoque** de 0 a 100 con el video andando y ver que cambia
      en el momento, tanto en el proyector como en los recuadros del operador.
- [ ] Aplicar otro fondo y volver al primero: tiene que acordarse del
      desenfoque.

### Fondo al azar

- [ ] Botón **🎲** en la barra de medios: aplica un fondo distinto cada vez.
- [ ] Apretalo tantas veces como fondos tengas y **anotá si alguno se repite
      antes de que salgan todos** — no debería.
- [ ] Marcá **🎲 al azar por canción** estando en *Fondos*. Cambiá de pestaña:
      la casilla tiene que verse destildada ahí (marca de dónde sortea).
- [ ] Proyectá tres o cuatro canciones seguidas: cada una tiene que salir con
      un fondo distinto.
- [ ] Andá y volvé entre versos de la misma canción: **el fondo no tiene que
      cambiar** dentro de la canción.
- [ ] En medio de una canción, aplicá un fondo a mano: tiene que quedarse hasta
      la canción siguiente.
- [ ] Proyectá un pasaje de la Biblia: **no** tiene que sortear nada.
- [ ] Cerrá y abrí la app: la casilla tiene que seguir marcada en *Fondos*.

### Reproducción continua

- [ ] Poner tres o cuatro videos en una pestaña y tildar **▶▶ continua**.
- [ ] Proyectar el primero y dejarlo: tiene que pasar solo al siguiente, y al
      terminar el último **volver al primero**.
- [ ] Destildar la casilla y confirmar que un video vuelve a repetirse solo.

### Descargar de YouTube

Hoy **no tenés yt-dlp instalado**, así que primero:

```
winget install yt-dlp ffmpeg
```

- [ ] Sin instalarlo: clic derecho en un video de YouTube → *Descargar para uso
      local*. Tiene que explicarte cómo instalarlo, no romperse.
- [ ] Ya instalado: descargar un video corto. Ver la barra de progreso y que
      **Cancelar** funcione.
- [ ] El video queda en la pestaña **Descargados** y se proyecta bien.
- [ ] Anotá **qué calidad bajó** (clic derecho en el archivo → Propiedades →
      Detalles). Con ffmpeg debería llegar a 1080p; sin ffmpeg, 720p.

### Corrector ortográfico

Depende de que Windows tenga el corrector de español instalado. **Si no
subraya nada, avisame**: significa que hay que instalarlo o buscar otro camino.

- [ ] Editar una canción y escribir mal a propósito ("cancion", "adoracion").
      Tienen que quedar **subrayadas en rojo**.
- [ ] Clic derecho sobre una: aparecen las **sugerencias con la tilde**.
- [ ] Clic derecho sobre una palabra bíblica rara → **Agregar al diccionario**,
      y ver que deja de estar subrayada.
- [ ] Confirmá que **"Jehová", "Getsemaní", "Efesios"** ya vienen aceptadas.

### Editor y temas

- [ ] En el diseño de una canción: escribir el **tamaño de letra** a mano, y
      subirlo/bajarlo de a uno con **− / +** (con Shift, de a diez).
- [ ] Lo mismo con el **interlineado** y con el **% de la caja**.
- [ ] Los botones de posición de la caja: que se entienda cuál mueve
      horizontal y cuál vertical.
- [ ] En 🎨 Temas: **Uso de mayúsculas** → probá TODAS MAYÚSCULAS y "como
      oración" y mirá la vista previa.
- [ ] **Contorno de la letra**: poné letra blanca sobre un fondo claro y subí el
      grosor del contorno hasta que se lea. Esto era un pedido tuyo concreto.
- [ ] **Intensidad de sombra**: bajala a 0 y subila a 100.
- [ ] Revisá que **tus temas y canciones de antes se vean igual que siempre**
      (la actualización cambia la base de datos; los temas viejos tendrían que
      conservar su sombra).

### La ventana reacomodada

- [ ] El **texto rápido** ahora está abajo a la derecha, desde el Live hasta el
      pie. Que se use cómodo.
- [ ] La barra de **medios** está abajo a la izquierda, **se agranda
      arrastrando** el borde de arriba y los medios se recorren **para abajo**.
- [ ] Cerrar y abrir: tiene que acordarse del alto que le dejaste.
- [ ] **Sin fondo (F4)** arriba: con una canción proyectada sobre un video,
      apretalo. Tiene que sacar el video **y dejar la letra**.
- [ ] Pasar el mouse por Clear, Black, Logo: los globos de ayuda explican qué
      hace cada uno y **duran lo suficiente para leerlos**.

---

### Cuenta regresiva (nuevo)

- [ ] Botón **⏱**: poner **2 minutos** y **Proyectar**. Tiene que arrancar en
      2:00 (no en 1:59) y bajar parejo.
- [ ] Mirarla **desde el fondo del salón**: ¿se lee el número? ¿y el texto de
      arriba?
- [ ] Con un **fondo de video** puesto: la cuenta va encima y el video sigue.
- [ ] Dejarla llegar a **cero**: tiene que quedarse con «¡Bienvenidos!» hasta
      que la detengas, sin números en negativo.
- [ ] Con la **pantalla de escenario** prendida: los músicos ven el mismo
      número.
- [ ] **F2 (Black)** y volver: la cuenta sigue en hora, no se atrasa.
- [ ] Probar la opción **a las HH:MM** con la hora de tu reunión.
- [ ] Cerrar y abrir el programa: los textos que escribiste tienen que seguir
      ahí.
- [ ] Detenerla desde el mismo botón ⏱ → **Detener la que corre**.

### PDF y PowerPoint (nuevo)

- [ ] **📥 Agregar** → elegir un **PDF** de varias páginas. Anotá **cuánto
      tarda**: se dibujan todas las páginas de una vez.
- [ ] Las páginas entran numeradas y **en orden** en la pestaña.
- [ ] Proyectar una: tiene que verse **a pantalla completa** y nítida.
- [ ] Agregar las páginas a la **playlist** y pasarlas con las flechas.
- [ ] Probar con una **presentación .pptx** de la iglesia: se convierte sola
      si tenés PowerPoint (tarda más la primera vez).
- [ ] Probar en la PC del proyector, que **puede no tener PowerPoint**: ahí
      tiene que avisar que la exportes a PDF, sin romperse.
- [ ] Mirá el espacio en disco: las páginas se guardan como imágenes.

### Buscador único (nuevo)

- [ ] **Ctrl+K** (o el botón 🔍) y escribir una palabra que esté en una
      canción **y** en la Biblia: tienen que aparecer las dos cosas juntas.
- [ ] **↑↓** para moverte por los resultados **sin** sacar el cursor de la
      caja, y **Enter** para cargar.
- [ ] Elegir una **canción**: queda cargada, lista para proyectar.
- [ ] Elegir un **versículo**: se carga en la pestaña Biblia.
- [ ] Elegir un **medio**: se aplica de fondo.
- [ ] Buscar mientras hay algo **proyectado**: la salida no se toca hasta que
      elegís.

### Panel LIVE con el video en movimiento (nuevo)

- [ ] Proyectar un **video de fondo** y mirar el panel **LIVE** del operador:
      tiene que verse avanzar, igual que en el proyector.
- [ ] Pasar de diapositiva con el video puesto: el preview sigue andando.
- [ ] Apagar la salida (Esc) y volver a prenderla: el preview retoma.
- [ ] Con la reunión larga, mirá si la PC se pone lenta (es dibujar el video
      dos veces, una en cada pantalla).

### Salida para transmisión (nuevo)

- [ ] Botón **📱** → copiar la dirección **para la transmisión**.
- [ ] Pegarla en un **Browser Source de OBS** (o el Web Input de vMix) y
      confirmar que aparece la letra con **fondo transparente** sobre la
      cámara.
- [ ] Pasar de diapositiva: la transmisión sigue al proyector con menos de un
      segundo de retraso.
- [ ] **F1 Clear** y **F2 Black**: la letra sale de la transmisión también.
- [ ] El **aviso al pie** y la **cuenta regresiva** aparecen en la transmisión.
- [ ] Probar `&fondo=negro` al final de la dirección.
- [ ] Dejarlo andando **toda la reunión** y mirar si la PC aguanta las dos
      cosas a la vez (proyección + transmisión).

### Listas inteligentes (nuevo)

- [ ] Botón **✨** del panel PLAYLIST → *Canciones que hace rato no cantamos*,
      90 días. Tiene que aparecer la lista llena (al principio, **todas**: las
      canciones que ya tenías no traen fecha de uso).
- [ ] Proyectar una canción y volver a la lista: esa canción **ya no está**.
- [ ] Probar *Canciones de un artista* con uno de los tuyos.
- [ ] Probar *Canciones que dicen una palabra* (ej. «santo»): tiene que mirar
      también **dentro de la letra**.
- [ ] Intentar **agregarle una canción a mano**: tiene que avisar que esa lista
      se arma sola, sin romper nada.
- [ ] El botón **✏️** sobre una lista inteligente abre su regla para cambiarla.
- [ ] Las playlists **normales** siguen funcionando igual que siempre.

### Recorte y color de los medios (nuevo)

- [ ] En *Propiedades* de un video, **Recorte**: poner *desde 5* y *hasta 12*.
      Tiene que arrancar en el segundo 5 y, al llegar a 12, volver al 5 (no al
      cero).
- [ ] Usarlo de verdad con un video que tenga **cortina al principio**.
- [ ] **Brillo −40** en un fondo cargado y proyectar una canción encima:
      ¿ahora se lee la letra?
- [ ] **Tinte** con el color de la iglesia, al 30-40 %.
- [ ] Con **brillo y desenfoque juntos**, mirá si la PC sigue yendo fluida.

### Cuadros de texto por diapositiva (nuevo)

- [ ] Clic derecho en una canción → *Editar canción (diseño)* → **➕ Cuadro de
      texto**.
- [ ] Escribir en el cuadro nuevo con **doble clic**, moverlo y estirarlo.
- [ ] Cambiarle **tipografía, tamaño, color y alineación**; los ajustes que no
      le corresponden quedan apagados.
- [ ] **Guardar** y proyectar: el cuadro sale en el proyector donde lo dejaste.
- [ ] Cerrar y abrir el programa: el cuadro sigue ahí.
- [ ] El selector de arriba vuelve a **Letra de la canción** y la caja de la
      letra se sigue moviendo como siempre.
- [ ] La **papelera** quita el cuadro elegido.
- [ ] Canciones **sin** cuadros extra tienen que verse exactamente igual que
      antes.

### Encuadre de medios (nuevo)

- [ ] Clic derecho en un fondo → *Propiedades* → **Encuadre**.
- [ ] **Zoom 120 %** en un video: tiene que acercarse y recortar. Ideal para
      sacar de cuadro una marca de agua.
- [ ] **Zoom 70 %** con un **color** de relleno: el video se ve más chico y el
      color llena alrededor.
- [ ] **Correr** la imagen a los lados y arriba/abajo.
- [ ] **Tamaño fijo** 1280×720: el medio queda en un recuadro centrado.
- [ ] Relleno con **otra imagen o video** de la biblioteca: se ve detrás,
      llenando. Probá con dos videos a la vez y **mirá si la PC aguanta**
      (Administrador de tareas: CPU y memoria) — es lo que más me preocupa de
      esta función.
- [ ] Lo que ves en el **preview del operador** tiene que ser lo mismo que sale
      en el proyector.
- [ ] *Reencuadrar* vuelve todo a como estaba.

### Transiciones (nuevo)

- [ ] En **🎨 Temas**, probar cada una de las cinco opciones de *Cómo entra la
      diapositiva* proyectando una canción y pasando con las flechas.
- [ ] Con **280 ms** (lo de siempre) tiene que verse igual que antes.
- [ ] **Mirala desde lejos**: ¿ayuda o marea? Es la pregunta que importa.
- [ ] Con un **video de fondo** puesto: la transición es sólo de la letra, el
      video no salta.
- [ ] Poner la **Biblia en corte** y las **canciones en fundido**, y confirmar
      que cada una se comporta como su tema dice.

### Notas para la plataforma (nuevo)

- [ ] Pestaña **Notas** (columna derecha) → escribir el guion del domingo y
      **Mostrar**.
- [ ] Con el **escenario prendido**: aparecen abajo, y **no** salen en el
      proyector. Confirmalo mirando las dos pantallas.
- [ ] Pegar un guion **largo** (diez líneas o más): tiene que entrar entero,
      achicado, sin cortarse.
- [ ] **Quitar** las saca del escenario.
- [ ] Cerrar y abrir el programa: el texto sigue escrito.
- [ ] En la flecha **▾** del escenario, *Mostrar las notas* las apaga y
      prende.

### Copia de seguridad (nuevo)

- [ ] Botón **💾** → *Guardar una copia*. **Con el programa abierto y
      proyectando**, que es como se va a usar. Anotá cuánto tarda y cuánto pesa.
- [ ] Copiar ese archivo a un **pendrive**.
- [ ] Probar **restaurar**: elegí la copia, mirá que los números que muestra
      sean los tuyos, aceptá y dejá que el programa se cierre y abra solo.
- [ ] Después de restaurar: **tus canciones, Biblias, medios y temas siguen
      todos ahí**.
- [ ] Probar elegir **un archivo cualquiera** (una foto, un .txt): tiene que
      avisar que no es una copia, sin romperse.

### Límite de intentos del PIN (nuevo)

- [ ] Desde el celular, poner el **PIN equivocado cinco veces**: a la quinta
      tiene que avisar «Demasiados intentos» y no dejar entrar ni con el PIN
      bueno.
- [ ] Con **otro celular**, entrar con el PIN correcto: ese tiene que entrar
      igual.
- [ ] Apagar y prender el control remoto desde la computadora: el celular
      bloqueado puede volver a entrar (con el PIN nuevo).

## 3. Cosas que ya verifiqué yo, pero conviene confirmar en tu PC

Todo esto lo probé proyectando en un segundo monitor y anda. Lo repito acá
porque tu hardware y tu proyector son otros.

- [ ] **Fondo sin diapositiva**: aplicá un video de fondo *sin* nada proyectado.
      Tiene que verse el video.
- [ ] **Bucle**: dejá un video de fondo corriendo y mirá que dé la vuelta sin
      cortes ni parpadeo.
- [ ] **Miniaturas**: la primera vez que abras la app va a regenerar las
      miniaturas de los videos sola (tarda unos segundos). Verificá que se vean
      **fotogramas de los videos** y no el cono naranja de VLC.
- [ ] **Aviso al pie** sobre un video, y los botones Clear / Black / Logo (F1,
      F2, F3) con un video de fondo.

### Del Inspector de medios

- [ ] Clic derecho en un medio → *Propiedades (Inspector)*.
- [ ] Cambiar la **escala** (Rellenar / Ajustar / Estirar), guardar, aplicar el
      medio y ver que la salida cambia de verdad. Probá con un video que **no**
      sea 16:9 — los `01` a `08` tuyos son 3:2 y sirven.
- [ ] Cambiar **fin de video** a "Logo", proyectar un video corto y ver que al
      terminar pasa al logo.
- [ ] Cambiar **Fondo / Primer plano** y ver que Primer plano tapa la letra.
- [ ] Cambiar el **volumen** y el silencio de un video con audio (esto es lo
      único que no pude escuchar yo).

### Del control remoto

Probado con la computadora hablándose a sí misma: la página carga, el PIN
correcto entra, el equivocado da 403, un comando cambia el estado de verdad, y
un pedido mal formado no rompe nada. Falta el celular real (sección 2).

---

## 4. Ensayo general

Lo ideal: armar un servicio completo, como si fuera el domingo, y usarlo de
punta a punta.

- [ ] Armar una **playlist** con canciones, pasajes bíblicos y medios.
- [ ] Navegar toda la playlist con las **flechas ← →**, incluido el salto de un
      elemento al siguiente. Con un pasaje (ej. `Juan 3:16-18`): al entrar cae
      en el 16, al llegar al 18 la flecha sigue al próximo elemento, y volviendo
      desde el elemento siguiente aterriza en el 18. Si el pasaje es el último
      elemento, la flecha sigue de largo por el capítulo (a propósito).
- [ ] **Biblia**: tipear una referencia (ej. `Juan 3:16`) y proyectarla con
      Enter. Apretar Enter de nuevo tiene que dejar el mismo versículo.
- [ ] Biblia con **dos versiones a la vez**.
- [ ] **Resaltado en vivo** sobre un versículo proyectado.
- [ ] Cambiar el **tema** de una canción y ver que se aplica al proyectar.
- [ ] Hacer todo el ensayo **manejando desde el celular**, con la computadora
      lejos.
- [ ] Dejar el programa **abierto una o dos horas** con un video de fondo y la
      pantalla de escenario prendida, para confirmar que no se cierra solo ni se
      pone lento.

---

## 5. Lo que sé que todavía no está

Para que no lo busques:

- **Cuenta regresiva** antes del servicio ("empezamos en 5:00").
- **Notas del predicador** en la pantalla de escenario (hoy muestra la letra de
  la canción, no un guion aparte).
- **Respaldo y restauración** de la biblioteca desde el programa. Por ahora es
  copiar `%APPDATA%\EcclesiaCast\ecclesiacast.db` a mano.
- **Transiciones** entre diapositivas más allá del fundido actual.
- **Salida NDI** para el streaming.
