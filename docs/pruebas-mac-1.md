# Prueba en la Mac — prototipo (tanda 1)

**Qué es esto.** Un programa chiquito y aparte, «EcclesiaCast Proto», que sirve
para saber si EcclesiaCast puede andar en Mac antes de tocar el programa de
verdad. Es feo a propósito: sólo tiene los botones para las pruebas. Tiene su
propia biblioteca (una Biblia y dos canciones de prueba) y no toca nada más de
la Mac.

Hay seis pruebas que **tienen** que salir bien (P1 a P6) y algunas más que sólo
se anotan. En cada paso dice qué mirar y qué foto sacar. Las fotos de la TV,
con el celular; las de la pantalla de la Mac, con **⇧⌘3** (Mayúsculas +
Comando + 3: quedan en el Escritorio).

Calculá una hora y media.

---

## Qué llevar

1. La **Mac prestada**. Tiene que tener chip Apple y macOS 15 o más nuevo.
   Para verlo: menú  (arriba a la izquierda) → **Acerca de esta Mac**. Tiene
   que decir «Chip Apple M…» (M1, M2, M3, M4…) y «macOS Sequoia 15» o un
   número más alto (por ejemplo «macOS Tahoe 26»). **Sacale foto a ese
   cartel.** Si dice «Procesador Intel», esta Mac no sirve para la prueba.
2. Un **adaptador** de la Mac a HDMI (casi todas las Mac nuevas tienen sólo
   USB-C; algunas tienen HDMI directo).
3. La **TV o el proyector** de la iglesia, con su cable HDMI.
4. Tu **celular**, conectado a la **misma wifi** que la Mac.
5. Un **pendrive**.
6. Dos videos de fondo de la iglesia, copiados en el pendrive: uno **1080p**
   (Full HD) y, si tenés, uno **4K**.
7. El cargador de la Mac (la prueba de video dura 10 minutos).

## Bajar el programa (en tu PC, antes de ir)

1. Entrá a GitHub → el repositorio de EcclesiaCast → pestaña **Actions** →
   a la izquierda **proto-mac** → la última ejecución con tilde verde.
2. Abajo, en **Artifacts**, bajá **EcclesiaCast-Proto-mac**.
3. Lo que baja es un `.zip` que adentro tiene **otro** `.zip`
   (`EcclesiaCast-Proto-mac.zip`). Abrí el de afuera y copiá **el de adentro
   tal cual** al pendrive. **No lo descomprimas en Windows**: se rompe el
   programa para Mac.
4. **Anotá el tamaño** del `EcclesiaCast-Proto-mac.zip` (clic derecho →
   Propiedades). Es un dato que se anota.

## Pasarlo a la Mac y abrirlo

1. Enchufá el pendrive en la Mac y arrastrá `EcclesiaCast-Proto-mac.zip` al
   **Escritorio**.
2. Doble clic en el `.zip`: aparece **EcclesiaCast Proto** (con el ícono
   genérico de programa).
3. Arrastrá **EcclesiaCast Proto** a la carpeta **Aplicaciones** (en el
   Finder, a la izquierda).
4. Abrí **Aplicaciones** y hacé doble clic en **EcclesiaCast Proto**.
   Contá los segundos hasta que aparece la ventana (dato que se anota).
5. Lo más probable es que salga un cartel que dice que **no se abrió** porque
   Apple no pudo verificarlo. **Sacale captura (⇧⌘3)** y tocá **Listo** (no
   toques «Trasladar a la papelera»).
   - Si en cambio se abre directo, mejor: seguí en P1.
6. Andá a menú  → **Configuración del Sistema** → **Privacidad y
   seguridad**. Bajá hasta el final, a la parte **Seguridad**. Tiene que
   decir que se bloqueó «EcclesiaCast Proto». Tocá **Abrir igualmente**.
   **Captura.**
7. Te pide la contraseña de la Mac (la tiene que poner el dueño). Después
   sale otro cartel con **Abrir igualmente**: tocalo. **Captura** de cada
   cartel.
8. **Si dice que el programa «está dañado»:** abrí **Terminal**
   (Aplicaciones → Utilidades → Terminal), escribí exactamente esto y
   apretá Enter:

   ```
   xattr -cr "/Applications/EcclesiaCast Proto.app"
   ```

   Cerrá la Terminal y volvé al paso 4. **Captura** del cartel de «dañado».
9. Si en algún momento aparece un cartel que pregunta si el programa puede
   **aceptar conexiones entrantes** o **buscar dispositivos en la red
   local**: **captura** y tocá **Permitir**.

Para cerrar el programa en cualquier momento: **⌘Q**, o cerrar la ventana
«EcclesiaCast Proto — operador».

---

## P1 — Abre y carga la Biblia

1. Con el programa abierto, en la **Vista previa** (a la derecha) tiene que
   verse **Juan 3:16** con el texto del versículo y, abajo a la derecha,
   «Juan 3:16 (RV1909)».
2. A la izquierda, en **Canciones**, tienen que estar las dos canciones de
   prueba («Prueba: Sublime gracia» y «Prueba: renglones largos ñandú»).
3. Tocá **Abrir carpeta de registros**: se abre una carpeta del Finder con un
   archivo `proto-…log`. Arriba de la ventana del Finder, mantené apretado
   el nombre de la carpeta: tiene que mostrar que está dentro de
   **Application Support → EcclesiaCast-proto**.
4. **Captura** de la ventana del programa y **captura** del Finder.

**Sale bien si:** se ve el versículo y la carpeta existe.

## Conectar la TV como pantalla separada

1. Enchufá la TV a la Mac con el adaptador.
2. Andá a menú  → **Configuración del Sistema** → **Pantallas**. Elegí la
   TV y, en **Usar como**, poné **Pantalla extendida** (no la opción que
   dice «Duplicar»). **Captura.**
3. Volvé al programa: en **Pantallas** tienen que aparecer **dos**.

## P2 — Proyecta a pantalla completa sin robar el teclado

1. En **Pantallas**, elegí la TV (la que **no** dice «principal»).
2. Tocá **Proyectar**.
3. Mirá la TV: tiene que verse **sólo** la diapositiva, de punta a punta:
   **sin** la barra de menú de arriba, **sin** el Dock, **sin** bordes.
   Llevá el mouse a la TV: el puntero tiene que desaparecer.
   **Foto de la TV.**
4. Volvé a la Mac: hacé clic en la caja **Escribí acá mientras proyecta** y
   escribí cualquier cosa. Tiene que escribirse normal, y la TV tiene que
   seguir igual.
5. Abrí otro programa (por ejemplo **Notas** o **Safari**) y ponelo adelante.
   La TV tiene que seguir tapando todo (sin que aparezca la barra de menú en
   la TV). **Foto de la TV.**
6. Si la Mac tiene varios escritorios, pasá de uno a otro deslizando tres
   dedos en el trackpad: la TV tiene que quedar igual.

**Sale bien si:** la TV muestra sólo la diapositiva todo el tiempo y en la Mac
se puede seguir escribiendo.

## P3 — Canción y versículo con contorno y sombra

1. En **Canciones**, elegí **Prueba: Sublime gracia**. Mirá que la vista
   previa y la TV muestren lo mismo. **Foto de la TV** y **captura** de la
   Mac.
2. Tocá **Siguiente** y **Anterior**: cambian las estrofas.
3. En **Biblia** elegí Juan, 3, 16 y tocá **Mostrar versículo**. **Foto de
   la TV.**
4. Elegí **Prueba: renglones largos ñandú**: mirá que se vean bien los
   acentos, la ñ, ¿ y ¡. **Foto de la TV.**
5. **Para comparar con Windows** (otro día, con la PC de la iglesia y la
   misma TV): en EcclesiaCast cargá una canción con este texto, poné el
   **contorno en 6** en el tema de Canciones, proyectala y sacale foto a la
   TV desde el mismo lugar:

   > Sublime gracia del Señor
   > que a un pecador salvó;
   > fui ciego mas hoy veo yo,
   > perdido y Él me halló.

La letra no va a ser idéntica: la Mac no tiene la fuente de Windows (Segoe
UI) y usa una muy parecida (Selawik), un poco más gruesa. Lo que hay que
mirar es que se vean el **contorno** y la **sombra**.

## P4 — Video de fondo con la letra encima, 10 minutos

1. Proyectando en la TV, con una canción elegida, tocá **Video de fondo** y
   elegí el video **1080p** del pendrive.
2. La letra tiene que verse **encima** del video, en la TV y en la vista
   previa.
3. Dejalo andar **10 minutos** sin tocar nada (el registro anota solo cómo
   va, cada 10 segundos).
4. Mientras tanto, mirá la TV cada vez que el video **vuelve a empezar**: no
   tiene que verse un **cuadro negro** ni un salto feo. Si lo ves, anotá la
   hora.
5. Durante esos 10 minutos, dejá **Safari** (u otro programa) adelante en la
   Mac: el video en la TV no se tiene que frenar (dato que se anota).
6. **Foto de la TV** con el video y la letra.

**Sale bien si:** el video se ve fluido, la letra encima y nunca negro al
volver a empezar. Los números (cuadros por segundo, memoria) los saco yo del
registro.

## P5 — Control desde el celular

1. En la ventana del programa, abajo, hay un **código QR** y una dirección
   tipo `http://192.168.…:8765`.
2. Con el celular **en la misma wifi**, apuntá la cámara al QR y abrí el
   enlace (o escribí la dirección en el navegador).
3. Aparecen dos botones: **Siguiente ▶** y **◀ Anterior**. Tocalos varias
   veces: la diapositiva de la TV tiene que cambiar enseguida (menos de medio
   segundo). La página muestra **«Tardó … ms»**.
4. **Captura del celular** con la página y el «Tardó».
5. Si la Mac mostró algún cartel al tocar los botones del celular,
   **captura** y anotá qué tocaste.

**Sale bien si:** cambia enseguida, cada vez.

## P6 — Desenchufar y enchufar la TV mientras proyecta

1. Proyectando, con el video de fondo andando, **desenchufá el cable HDMI**
   de la Mac.
2. El programa **no** se tiene que cerrar ni quedar colgado. Arriba, en rojo,
   tiene que aparecer: «Se desconectó la pantalla de salida…». **Captura.**
3. **Volvé a enchufar** el cable. Esperá que la TV se prenda. En el programa
   aparece «Volvió una pantalla. Ya podés tocar «Proyectar».».
4. En **Pantallas** elegí la TV y tocá **Proyectar**. Tiene que volver a
   proyectar **sin cerrar el programa**. **Foto de la TV.**
5. Repetilo una vez más.

**Sale bien si:** nunca se colgó y volvió a proyectar sin reiniciar.

---

## Pruebas que sólo se anotan

1. **Sonido por HDMI:** tocá **Apagar**, marcá **Video con sonido (para
   probar el HDMI)**, tocá **Video de fondo**, elegí un video con sonido y
   **Proyectar**. ¿Se escucha por la TV? Anotalo.
2. **Video 4K:** **Video de fondo** con el video 4K, 2 minutos. ¿Se ve
   fluido? Anotalo.
3. **Una sola pantalla:** desenchufá la TV, tocá **Proyectar**: la
   diapositiva tapa toda la pantalla de la Mac. Apretá **Esc**: tiene que
   volver al programa. Probá también **doble clic** sobre la diapositiva.
4. **Espejo:** con la TV enchufada, en **Pantallas** de la Configuración
   poné **Duplicar**. Tocá **Proyectar**, mirá qué pasa en la TV y en la Mac,
   y volvé con **Esc**. **Foto.** Después dejalo otra vez en «Pantalla
   extendida».
5. **Pantalla Retina:** nada que hacer; mirá si la vista previa se ve nítida.
6. **Cuánto tarda en abrir:** cerrá el programa (⌘Q), volvé a abrirlo y
   contá los segundos hasta que aparece la ventana.
7. **Tamaño de la descarga:** el que anotaste al bajarlo.

---

## Al final

1. Tocá **Abrir carpeta de registros**, seleccioná todos los archivos (⌘A),
   clic derecho → **Comprimir**. Queda un `Archivo.zip`.
2. **Mandame las fotos y la carpeta de registros** (ese `Archivo.zip`), y lo
   que anotaste (segundos para abrir, tamaño, carteles que aparecieron, horas
   de algún cuadro negro).

**Para dejar la Mac como estaba:** arrastrá **EcclesiaCast Proto** de
Aplicaciones a la Papelera. En el Finder, menú **Ir** → **Ir a la carpeta…**,
escribí `~/Library/Application Support` y borrá la carpeta
**EcclesiaCast-proto**.
