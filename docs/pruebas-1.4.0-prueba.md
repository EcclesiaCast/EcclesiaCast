# Qué probar en la 1.4.0 de prueba (11/11/2026)

Instalador: `dist\EcclesiaCast-1.4.0-prueba.2-setup.exe` (158 MB). No está
publicado en ningún lado. La prueba.2 arregla que «Seguir la canción» no
abría ninguna entrada y que «Bajar Biblias» repetía una que ya estaba; se
instala encima de la prueba.1.

**Nada de esto se probó con el programa abierto.** Compila, pasan las 1.301
pruebas y el reconocedor de voz se probó contra el audio de tres cultos, pero
ninguna ventana nueva se usó con el mouse. Lo esperable es encontrar detalles.

## Antes de instalar

1. **Hacé una copia de la biblioteca:** Archivo → «Guardar una copia de la
   biblioteca…». Esta versión le agrega a la base lo necesario para la
   papelera; si algo sale mal, con la copia se vuelve atrás.
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
- Tarda unos 3 segundos en entender cada tramo.
- Usa bastante procesador mientras escucha. **Mirá si el video de fondo se
  traba**; si se traba, es un dato importante.
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

## Si algo falla

El registro del día está en
`%APPDATA%\EcclesiaCast\logs\ecclesiacast-AAAAMMDD.log`. Con decirme qué
hiciste y a qué hora, lo busco ahí.

## Lo que quedó afuera a propósito

- **Partir la ventana principal en piezas:** después de esta prueba. Toca
  todo el programa y no conviene mezclarlo con lo nuevo.
- **Copia de seguridad automática:** descartada.
- **Campaña de redes:** en pausa.
