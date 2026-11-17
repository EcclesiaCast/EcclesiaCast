# Qué probar en la 1.4.0 de prueba (11/11/2026, al día el 14/11)

Instalador: `dist\EcclesiaCast-1.4.0-prueba.5-setup.exe` (171 MB). No está
publicado en ningún lado. Se instala encima de las anteriores.

**Lo nuevo de la prueba.5 (para el domingo 15/11):**

- **«Seguir la canción» rehecho** (sección 1): usa la placa de video,
  escucha una vez por segundo y sigue el orden de la canción. Con las dos
  canciones del 13/11 pasó de acertar 6 de 18 cambios a 16 de 18, y de ir
  11 s atrás a unos 4 s. **Es lo principal para probar en el culto.**
- **Menú Archivo dividido:** ahora hay menús **Canciones**, **Biblias** y
  **Fondos**. Mirá que cada opción abra lo mismo que antes.
- **Ajustes → «Cerrar»** ya cierra la ventana.
- **Bajar Biblias:** si elegís una que ya tenés, lo dice en una ventana.
- **Fondos de Pixabay** en vez de Pexels (sección 6). Hasta que publiques el
  servicio dice «todavía no está disponible».
- **Encuadre del logo** (sección 7): zoom, correr, tamaño fijo y con qué se
  rellenan los bordes, igual que en los fondos.

- La prueba.2 arregla que «Seguir la canción» no abría ninguna entrada y que
  «Bajar Biblias» repetía una que ya estaba.
- La prueba.3 suma la cuenta anónima de uso. **Mirá** Ajustes →
  Actualizaciones: está el interruptor «Contar en forma anónima que se usó»
  con su explicación. Hasta que publiques el servicio en Cloudflare
  (`servicios/api/LEEME.md`) no cuenta nada, y no tiene que dar ningún
  error ni aviso por eso.
- La prueba.4 suma **los avisos con el aspecto del programa** y **los fondos
  de Pexels** (ver las secciones 5 y 6, al final).

**Nada de esto se probó con el programa abierto.** Compila, pasan las 1.327
pruebas y el reconocedor de voz se probó contra el audio de tres cultos, pero
ninguna ventana nueva se usó con el mouse. Lo esperable es encontrar detalles.

## Antes de instalar

1. **Hacé una copia de la biblioteca:** Archivo → «Guardar una copia de la
   biblioteca…». Esta versión le agrega a la base lo necesario para la
   papelera y el encuadre de los logos; si algo sale mal, con la copia se
   vuelve atrás.
2. Cerrá el programa e instalá encima. Windows va a mostrar el aviso de
   siempre («Más información» → «Ejecutar de todas formas»).

## 1. Seguir la canción (lo principal)

**En casa, sin micrófono** (es la forma más cómoda de empezar):

1. Proyección → «Seguir la canción por lo que se escucha (beta)…».
2. «Bajar el reconocedor de voz»: unos 550 MB, una sola vez. Tiene que mostrar
   los MB que va bajando y terminar en «Reconocedor de voz listo».
3. En «De dónde escucha» elegí **«Lo que suena en esta PC: …»**.
4. Abrí una canción que tengas cargada y poné su primera diapositiva.
5. Poné a sonar esa misma canción (YouTube, Spotify, un video del culto).
6. «Empezar a escuchar». Qué mirar:
   - La barra verde se mueve con la música.
   - A los pocos segundos, abajo aparece en cursiva lo que va entendiendo.
   - Arriba dice «Voy por la diapositiva N».
   - En **Sólo sugerir**: la diapositiva que sigue queda marcada con borde
     azul y Enter la pone. No tiene que proyectar nada solo.
7. Probá después **Adelantarse**: tiene que pasar sola cuando la diapositiva
   está terminando. Si pasa tarde, subí «Pasar cuando falten … palabras»; si
   pasa antes de tiempo, bajalo.
8. Con el modo automático andando, **pasá una diapositiva con la flecha**: el
   programa tiene que respetarla y seguir desde ahí.
9. Cerrá la ventanita: abajo a la derecha de la ventana principal queda un
   botón verde con lo que está haciendo; un clic la vuelve a abrir.

**En la iglesia:** elegí la entrada por donde llega el sonido de la consola,
dejalo en **Sólo sugerir** y pasá las diapositivas a mano como siempre. No hay
que hacer nada más: el programa anota en el registro del día lo que entendió
y lo que hubiera hecho. Después del culto avisame y lo leo para ver en cuántas
acertó.

Qué esperar, para no llevarse una sorpresa:

- Anda con canciones a ritmo medio y la voz adelante. Con canciones rápidas y
  la banda fuerte se pierde, y ahí va a decir «No ubico la letra todavía».
- Con la placa de video entiende cada tramo en medio segundo y va unos 4
  segundos atrás de lo que se canta en «Seguir». En **Adelantarse** (ahora
  viene en 2 palabras) llegó justo a tiempo o un poco antes: **probá ese
  modo primero**.
- Para saltar a otra parte de la canción lo tiene que escuchar dos veces
  seguidas: si alguien repite un coro fuera de orden, tarda un poco más.
- Ahora trabaja la placa de video mientras escucha. **Mirá si el video de
  fondo se traba**; si se traba, es un dato importante.
- **Con el micrófono:** sube el volumen solo si llega bajito, y avisa si por
  esa entrada no llega nada. Probalo también con el micrófono de la
  notebook, aunque sea cerca del parlante.
- Sólo actúa con una canción que ya tiene una diapositiva en pantalla. Nunca
  pone la primera, nunca cambia de canción y no toca la Biblia.

## 2. Editor de temas (rehecho)

1. Cambiar algo y tocar otro tema de la lista → pregunta si guardar. Probar
   Sí, No y Cancelar.
2. Cambiar algo y cerrar con la X → pregunta.
3. Aplicar una plantilla y «Deshacer cambios» → vuelve a como estaba.
4. «Probar sobre» foto oscura y clara, «Texto largo», «Ver la caja».
5. Elegir tipografía: cambia en el acto. Que la lista no tarde en abrir.
6. Tema de Biblia: aparece la sección «Biblia».
7. «Usar en canciones» / «Usar en Biblia»: cambia la marca «En uso».

## 3. Sonido que vuelve solo

Con un video con sonido en pantalla, sacá y poné el cable HDMI (o apagá y
prendé el parlante). Tiene que volver a sonar solo en un segundo.

## 4. Lo que hizo Sonnet

- **Esc dos veces.** Con la salida prendida, un Esc sólo avisa abajo; el
  segundo, dentro de 2 segundos, apaga. El botón «Apagar salida» sigue
  apagando con un clic.
- **Listas vacías.** Con una biblioteca sin canciones aparecen los botones
  para buscar, traer o escribir una; con el buscador sin resultados dice
  «Ninguna canción con eso». (Para verlo sin borrar nada: escribí cualquier
  cosa rara en el buscador.)
- **Diseñador de canciones.** «Texto» y «Caja» ahora son pastillas oscuras.
  Que al pasar de una a otra se vean los campos correctos y que todo siga
  funcionando (color, tamaño, caja).
- **Papelera.** Borrar una canción o un tema ya no es definitivo: Edición →
  «Papelera…». Probar borrar, restaurar, «Borrar del todo» y que una canción
  borrada desaparezca de la lista, del buscador y de la playlist.
- **Paleta de colores.** Grilla grande, «Usados hace poco» (que queden al
  cerrar y abrir el programa), y «Ajuste fino» con Tono, Intensidad y
  Claridad. Probarla desde Temas y desde el diseñador.
- **Ajustes.** Archivo → «Ajustes…» (o Ctrl+,). Recorrer las secciones:
  cambiar la pantalla de salida, reiniciar el sonido, transiciones, modo de
  bajo rendimiento, aspecto. Cada cosa tiene que hacer lo mismo que desde su
  menú de siempre.
- **Errores sin cartel.** No se puede provocar a mano; si alguna vez aparece
  abajo «Hubo un error; el programa sigue», avisame y miro el registro.

## 5. Avisos con el aspecto del programa

Todos los carteles grises de Windows ahora son una ventana oscura del
programa, con botones que dicen lo que hacen. Para verlos sin romper nada:

1. **Temas:** cambiá algo en un tema y cerrá con la X → «Guardar / No
   guardar / Seguir editando». Probá las tres.
2. **Temas:** creá un tema nuevo y eliminalo → aviso amarillo con «Mandar a
   la papelera» en rojo. Después restauralo desde Edición → Papelera.
3. **Papelera:** «Vaciar la papelera» pregunta antes.
4. **Fondos → «Bajar más fondos de internet…»** → «Bajar los fondos / Ahora
   no» (podés decir «Ahora no»).
5. Qué mirar en cada uno: que se lea entero, que **Enter** elija el botón
   azul o rojo, que **Esc** cancele, y que aparezca encima de la ventana
   desde donde lo abriste (no detrás).

## 6. Fondos de Pixabay

**Antes de publicar el servicio:** tocá **🎞 Fondos** en la barra de medios
(o menú Fondos → «Buscar fondos en Pixabay…»), buscá «cruz» → tiene que
decir «La búsqueda de fondos todavía no está disponible», sin errores raros.

**Después de publicarlo** (`servicios/api/LEEME.md`: creás una cuenta gratis
en pixabay.com, copiás la clave de pixabay.com/api/docs y hacés los pasos en
Cloudflare):

1. 🎞 Fondos → buscá «cruz atardecer». Aparecen tarjetas con la imagen, la
   duración y quién lo hizo.
2. Cambiá a **Fotos** → busca fotos con la misma palabra.
3. «Ver más resultados» trae más.
4. Elegí dos o tres con un clic (se marcan en azul), elegí la pestaña abajo y
   tocá **Bajar 3**. Mientras baja dice el porcentaje.
5. Aparecen en esa pestaña de medios, con miniatura, y se proyectan como
   cualquier fondo. **Mirá que el video ande en bucle y se vea nítido en el
   proyector.**
6. Buscá lo mismo otra vez: los que ya bajaste dicen «Ya está en tus medios».
7. Abajo a la izquierda, «Fotos y videos de Pixabay» abre la página de Pixabay.

## 7. Encuadre del logo

1. Proyección → Logos…, elegí un logo de imagen (mejor uno cuadrado).
2. Tiene que aparecer el recuadro **Encuadre**. En «Los bordes se rellenan
   con» elegí **«La misma imagen, desenfocada»**: los costados se llenan con
   el logo borroso.
3. En la vista previa de arriba: **arrastrá** para correrlo, la **ruedita**
   hace zoom, las **flechas** lo corren de a poquito. «Reencuadrar» lo vuelve
   a dejar como estaba.
4. Probá **Tamaño fijo en pantalla** con «Cuadrado».
5. Mostrá el logo en el proyector: **se tiene que ver igual que en la vista
   previa.**
6. Si tenés un logo de **video**, mirá que las barras de los costados salgan
   del color de fondo del logo (antes podía verse el video de fondo pausado).
7. Tus otros logos tienen que verse como antes.

## 8. Vista en grilla de la Biblia (prueba.9)

Instalador: `dist\EcclesiaCast-1.4.0-prueba.9-setup.exe`. Es lo único nuevo
respecto de la prueba.8. Si la grilla da problemas, se saca y la 1.4.0 sale
sin ella (está en commits propios).

1. Pestaña **Biblia**: al lado del 🕘 hay un botón **«▦ Grilla»**. Tocalo.
2. Aparecen los **libros como fichas**, con la abreviatura grande y el nombre
   chico. Cada grupo tiene su color (Ley, Históricos, Poéticos, Profetas,
   Evangelios, Hechos, Cartas de Pablo, Cartas generales, Apocalipsis). El
   botón pasó a decir **«☰ Lista»**.
3. Tocá **Juan**: la ficha queda marcada y abajo aparecen sus **capítulos** y,
   al lado, los **versículos**. Tocá el capítulo 3: se carga en el centro.
4. Tocá el versículo **16**: **se proyecta** (igual que un clic en su
   diapositiva).
5. Con la grilla abierta, escribí **`jn 3 16`** y Enter, y después
   **`Juan 3:16`**: tiene que saltar al versículo, marcar las fichas y
   proyectarlo. Probá también un libro con número («1co 13»).
6. Con una **Biblia sólo del Nuevo Testamento** (o cualquier otra con menos
   libros) tienen que aparecer sólo los libros que tiene.
7. **Cambiá el ancho del panel** (o achicá la ventana): las fichas se
   reacomodan y nada se corta ni sale una barra horizontal.
8. Cerrá el programa y abrilo: tiene que volver a la **vista que dejaste**.
9. Volvé a **«☰ Lista»**: la lista de siempre, igual que antes. La búsqueda
   con `?` y el historial 🕘 tienen que funcionar en las dos vistas.
10. Mirá que, al cambiar de Biblia con la grilla abierta, las fichas
    se actualicen.

Lo que **no** tiene a propósito: favoritos de versículos.

## Si algo falla

El registro del día está en
`%APPDATA%\EcclesiaCast\logs\ecclesiacast-AAAAMMDD.log`. Con decirme qué
hiciste y a qué hora, lo busco ahí.

## Lo que quedó afuera a propósito

- **Partir la ventana principal en piezas:** después de esta prueba. Toca
  todo el programa y no conviene mezclarlo con lo nuevo.
- **Copia de seguridad automática:** descartada.
- **Campaña de redes:** en pausa.
