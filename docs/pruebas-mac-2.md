# Prueba en la Mac — segunda visita, con proyector (tanda 11)

**Qué es esto.** La primera visita probó un programa aparte, el prototipo.
Esta vez se prueba **EcclesiaCast de verdad**: la misma biblioteca, las mismas
canciones, la misma cuenta de la letra y el mismo video que en Windows. Lo
único provisorio es la **ventana del operador**: es una ventana de prueba, fea
a propósito, con los botones justos. La de verdad llega más adelante (tanda
19). Lo que no tiene todavía (editar canciones, temas, copias de seguridad…)
dice **«Esto todavía no está en la versión de Mac»**: eso está bien, no es una
falla.

Hay seis pruebas que **tienen** que salir bien (P1 a P6) y otras que sólo se
anotan. En cada paso dice qué mirar y qué foto sacar. Las fotos de la TV o del
proyector, con el celular; las de la pantalla de la Mac, con **⇧⌘3**
(Mayúsculas + Comando + 3: quedan en el Escritorio).

Calculá dos horas.

---

## Qué llevar

1. La **Mac prestada** (chip Apple y macOS 15 o más nuevo). Menú  →
   **Acerca de esta Mac**: tiene que decir «Chip Apple M…» y «macOS Sequoia
   15» o más. **Sacale foto a ese cartel.**
2. El **adaptador** de la Mac a HDMI.
3. El **proyector** o la TV de la iglesia, con su cable HDMI.
4. Tu **celular**, en la **misma wifi** que la Mac.
5. Un **pendrive** con:
   - el programa (ver «Bajar el programa»);
   - la **biblioteca de Windows** (ver «Llevar la biblioteca de Windows»).
6. El **cargador** de la Mac: el culto de prueba dura 20 minutos.
7. Las **fotos de Windows** para comparar (ver P3). Si no las tenés, se
   sacan otro día con la PC de la iglesia.

## Bajar el programa (en tu PC, antes de ir)

1. Para que GitHub lo arme, la rama tiene que estar subida como
   **`prueba-mac/visita-2`**. Pedíselo a Claude («subí la rama de la Mac para
   la segunda visita»). GitHub tarda unos 15 minutos.
2. Entrá a GitHub → el repositorio de EcclesiaCast → pestaña **Actions** → a
   la izquierda **desktop-mac** → la última ejecución con tilde verde.
3. Abajo, en **Artifacts**, bajá **EcclesiaCast-mac-prueba**. (Al lado
   aparece también **render-avalonia-mac**: son las fotos de prueba de la
   letra para Claude, no hace falta llevarlas.)
4. Lo que baja es un `.zip` que adentro tiene **otro** `.zip`
   (`EcclesiaCast-mac-prueba.zip`). Abrí el de afuera y copiá **el de adentro
   tal cual** al pendrive. **No lo descomprimas en Windows**: se rompe el
   programa para Mac.
5. **Anotá el tamaño** de `EcclesiaCast-mac-prueba.zip`.

## Llevar la biblioteca de Windows

Así en la Mac están tus canciones de siempre.

1. En Windows, **cerrá EcclesiaCast**.
2. Tocá **Windows + R**, escribí `%APPDATA%\EcclesiaCast` y Enter.
3. Copiá el archivo **`ecclesiacast.db`** al pendrive. (Los videos e
   imágenes no viajan en este archivo; en la Mac se usan los fondos que
   vienen con el programa.)

---

## Pasarlo a la Mac y abrirlo

1. Arrastrá `EcclesiaCast-mac-prueba.zip` del pendrive al **Escritorio**.
2. Doble clic en el `.zip`: aparece **EcclesiaCast**.
3. Arrastrá **EcclesiaCast** a la carpeta **Aplicaciones**.
4. Abrí **Aplicaciones** y hacé doble clic en **EcclesiaCast**. Contá los
   segundos hasta que aparece la ventana (se anota).
5. Seguramente sale un cartel de que **no se abrió** porque Apple no pudo
   verificarlo. **Captura (⇧⌘3)** y tocá **Listo** (nunca «Trasladar a la
   papelera»).
6. Menú  → **Configuración del Sistema** → **Privacidad y seguridad**. Bajá
   hasta **Seguridad**: dice que se bloqueó «EcclesiaCast». Tocá **Abrir
   igualmente**. Pide la contraseña de la Mac (la pone el dueño). En el cartel
   que sigue, otra vez **Abrir igualmente**. **Captura** de cada cartel.
7. **Si dice que el programa «está dañado»:** abrí **Terminal**
   (Aplicaciones → Utilidades → Terminal), escribí esto y Enter:

   ```
   xattr -cr /Applications/EcclesiaCast.app
   ```

   Cerrá la Terminal y volvé al paso 4. **Captura** del cartel.
8. Si pregunta si el programa puede **aceptar conexiones entrantes**:
   **captura** y **Permitir**.

Arriba de la ventana tiene que haber una franja que dice **«Ventana de
prueba, provisoria»**.

Para cerrar el programa: **⌘Q**, o cerrar esa ventana. Cerrar la ventana
**cierra el programa entero**, también la salida.

---

## P1 — Abre, crea la biblioteca y trae las Biblias

Esta primera vez, sin tu biblioteca de Windows.

1. En **Biblia**, arriba, abrí la lista de versiones: tienen que estar las
   que vienen con el programa (RV1909 y las otras).
2. Elegí **RV1909**, después **Juan**, capítulo **3** y versículo **16**. En
   **Diapositivas** aparece el capítulo, y en **En vivo** (a la derecha) se ve
   el versículo.
3. En el Finder: menú **Ir → Ir a la carpeta…** (⇧⌘G), escribí
   `~/Library/Application Support/EcclesiaCast` y Enter. Tiene que haber un
   archivo `ecclesiacast.db` y una carpeta `logs`.
4. **Captura** de la ventana del programa y **captura** del Finder.

**Sale bien si:** se ve el versículo y la carpeta existe.

### Poner tu biblioteca de Windows

1. Cerrá el programa (**⌘Q**).
2. En esa misma carpeta del Finder, **borrá** `ecclesiacast.db` (y, si
   están, `ecclesiacast.db-wal` y `ecclesiacast.db-shm`).
3. Copiá ahí el `ecclesiacast.db` del pendrive.
4. Abrí EcclesiaCast otra vez. En **Canciones** tienen que estar tus
   canciones. **Captura.**

Si algo falla acá, anotalo y seguí con la biblioteca nueva: la Biblia
alcanza para casi todas las pruebas.

## Conectar el proyector como pantalla separada

1. Enchufá el proyector a la Mac con el adaptador.
2. Menú  → **Configuración del Sistema** → **Pantallas**. Elegí el
   proyector y en **Usar como** poné **Pantalla extendida** (no «Duplicar»).
   **Captura.**
3. En el programa, a la derecha, en **Pantalla de salida** tienen que estar
   las dos pantallas. Si no, tocá **Volver a buscar las pantallas**.

## P2 — Pantalla completa en el proyector, sin robar el teclado

1. En **Pantalla de salida** elegí el proyector (el que **no** dice
   «principal»).
2. Elegí un versículo o una canción y tocá **Prender / apagar la salida**.
3. Mirá el proyector: tiene que verse **sólo** la diapositiva, de punta a
   punta: **sin** barra de menú, **sin** Dock, **sin** bordes. Llevá el mouse
   al proyector: el puntero desaparece. **Foto del proyector.**
4. En la Mac, hacé clic en **Buscar canción…** y escribí algo. Tiene que
   escribirse normal y el proyector seguir igual.
5. Poné otro programa adelante (**Notas** o **Safari**). El proyector sigue
   tapando todo. **Foto del proyector.**
6. Si la Mac tiene varios escritorios, pasá de uno a otro deslizando tres
   dedos en el trackpad: el proyector queda igual.

**Sale bien si:** el proyector muestra sólo la diapositiva todo el tiempo y en
la Mac se puede seguir escribiendo.

## P3 — Una canción y un versículo, para comparar con Windows

Con el **tema de fábrica** (el que viene con el programa, sin cambios).

1. En **Canciones** buscá una canción que use el tema de fábrica de
   canciones. Doble clic en la primera diapositiva (o elegila y Enter).
2. **Foto del proyector** desde un lugar fijo (por ejemplo, el fondo del
   salón, al medio). Mirá que **En vivo**, en la Mac, muestre lo mismo.
   **Captura** de la Mac.
3. **Siguiente ▶** y **◀ Anterior** cambian la diapositiva (también las
   flechas del teclado).
4. En **Biblia**: RV1909, **Juan 3:16**. **Foto del proyector** desde el
   mismo lugar.
5. **Para comparar:** la misma canción y Juan 3:16 RV1909 proyectados desde
   Windows, en el mismo proyector y desde el mismo lugar. Si todavía no
   tenés esas fotos, sacalas otro día.

Lo que hay que mirar: que los renglones se corten **en las mismas palabras**
y que la letra tenga **el mismo tamaño**. La forma de las letras puede variar
un poquito: la Mac no tiene la fuente de Windows (Segoe UI) y usa una
casi igual (Selawik).

## Pantalla Retina con el proyector

La pantalla de la Mac tiene el doble de puntos (Retina); el proyector no.

1. Con el proyector prendido, mirá que la diapositiva lo llene **entero**,
   sin bordes negros de más ni partes cortadas. **Foto.**
2. Apagá la salida. En **Pantalla de salida** elegí la pantalla de **la
   Mac** (la «principal») y prendé la salida: tapa toda la Mac, también la
   barra de menú. La letra se tiene que ver **nítida**, no borrosa.
   **Captura (⇧⌘3).**
3. Arriba aparece un cartel: «Para volver al programa: tecla **Esc** o doble
   clic en esta pantalla». Apretá **Esc**: vuelve la ventana del programa.
4. (Se anota) Si en **Pantallas** ponés **Duplicar**, tiene que funcionar
   igual que con una sola pantalla: la salida tapa la Mac y Esc vuelve.
   Después dejalo otra vez en **Pantalla extendida**.

## P5 — Control desde el celular

1. Tocá **📱 Control desde el celular…**. Aparece un cartel con un código QR,
   una dirección y un PIN. **Captura.**
2. Con el celular, escaneá el código (o escribí la dirección en el
   navegador) y poné el PIN.
3. Con la salida prendida, tocá **siguiente** y **anterior** en el celular:
   el proyector tiene que cambiar enseguida (menos de medio segundo).
4. Anotá **qué preguntó la Mac** (si preguntó algo) al conectar el celular.
   **Captura** del cartel.

## P4 — Culto de prueba de 20 minutos con video de fondo

Hacé de cuenta que es un culto. Anotá la hora al empezar.

1. A la derecha, en **Video de fondo**, elegí la pestaña **Fondos de
   ejemplo**, elegí un video y tocá **Usar de fondo**.
2. Proyectá una canción encima. El video se mueve detrás de la letra.
   **Foto.**
3. Pasá las diapositivas de a una, más o menos una por minuto, durante 5
   minutos.
4. Pasá a la **Biblia** y proyectá unos versículos, con el video atrás.
5. Probá **Negro** (todo negro) y otra vez **Negro** (vuelve). Probá
   **Logo** y otra vez **Logo**.
6. Poné **Safari** adelante y **no toques la Mac durante 10 minutos**.
   Mirá el proyector de vez en cuando:
   - el video **no se frena** ni va a los saltos;
   - **nunca** se ve un cuadro negro cuando el video vuelve a empezar;
   - la pantalla de la Mac **no se apaga** ni se oscurece.
7. Volvé al programa y apagá la salida.

**Sale bien si:** el video fue parejo los 20 minutos, sin cuadros negros, y la
Mac no se durmió. Los números (cuadros por segundo, cuadros perdidos,
memoria) quedan en el registro; los miramos después.

## P6 — Desenchufar el proyector

1. Con la salida prendida y el video de fondo en marcha, **desenchufá** el
   cable del proyector.
2. El programa **no se tiene que cerrar ni colgar**. Tiene que salir un
   cartel: «Se desconectó la pantalla de la salida». **Captura.** Tocá
   **Aceptar**.
3. **Volvé a enchufar** el proyector. Esperá unos segundos: en **Pantalla de
   salida** tiene que volver a aparecer el proyector, ya elegido.
4. Tocá **Prender / apagar la salida**: vuelve a proyectar. **Foto.**

**Sale bien si:** no se cerró, avisó, y se pudo volver a proyectar sin cerrar
el programa.

## Otras cosas para anotar

- ¿El sonido del video sale por el proyector (HDMI)?
- ¿Cuántos segundos tardó en abrir?
- ¿Qué botones dijeron «Esto todavía no está en la versión de Mac»? (Es lo
  esperado; sólo anotalo si algo **se colgó** o **se cerró**.)
- **Carteles** (el del celular, o cualquier aviso). Mientras un cartel
  está abierto, el programa espera la respuesta de una forma que en la Mac
  nunca se probó. Con la salida prendida y un **video de fondo**:
  1. Abrí **📱 Control desde el celular…** y dejalo abierto 1 minuto: el
     video del proyector tiene que **seguir moviéndose** y el celular tiene
     que poder pasar de diapositiva.
  2. Cerrá el cartel con su botón y volvé a abrirlo; cerralo esta vez con
     el **botón rojo** de la esquina. El programa tiene que seguir funcionando.
  3. Último, con el cartel abierto, apretá **⌘Q**. Anotá qué pasa: se
     cierra, pregunta, o queda colgado (si se cuelga: **⌥⌘Esc** → Forzar
     salida, y mandá el registro).

---

## Qué mandar después

1. Todas las **fotos** y **capturas**, con el número de la prueba (P1, P2…)
   en el mensaje.
2. El **registro**: en el Finder, **Ir → Ir a la carpeta…** (⇧⌘G),
   `~/Library/Application Support/EcclesiaCast/logs`. Mandá los archivos
   `ecclesiacast-…log` de ese día.
3. La foto de **Acerca de esta Mac** (chip y versión de macOS).
4. El **tamaño** del `.zip` y los segundos que tardó en abrir.
5. Una línea por prueba: **P1 a P6, salió bien o no**, y qué pasó cuando no.

Al terminar podés borrar el programa (arrastrá **EcclesiaCast** de
Aplicaciones a la papelera) y la carpeta
`~/Library/Application Support/EcclesiaCast`. Nada más de la Mac se tocó.
