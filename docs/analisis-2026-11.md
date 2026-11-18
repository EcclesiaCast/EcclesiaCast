# Análisis del programa — 10/11/2026

Revisión hecha sobre la versión 1.3.1. Cada punto dice **qué vi**, por qué
importa un domingo y qué haría. Lo que ya quedó arreglado en esta tanda está
marcado ✅; el resto está ordenado por lo que más conviene hacer primero.

No es una auditoría renglón por renglón de todo el código (son más de 16.000
líneas sólo en la ventana principal y sus partes): revisé a fondo el editor de
temas, el arranque, el manejo de errores, las copias de seguridad, el control
remoto y la forma en que está repartido el código, y miré el resto por
patrones de riesgo.

## 1. Editor de temas ✅ (hecho en esta tanda)

Fotos: [antes](analisis/editor-temas-antes.png) ·
[después](analisis/editor-temas-despues.png) ·
[tema de Biblia](analisis/editor-temas-biblia.png)

| Qué pasaba | Qué quedó |
|---|---|
| **Los cambios se perdían sin aviso** al hacer clic en otro tema de la lista, al crear uno nuevo o al cerrar. | Pregunta «¿Querés guardar los cambios?» (Sí / No / Cancelar), avisa «Cambios sin guardar» y hay «Deshacer cambios». |
| Cuarenta campos en dos columnas largas; la tipografía ni se veía sin bajar, porque los 15 botones de plantillas ocupaban media ventana. | Campos a la derecha, en seis secciones: Letra · Sombra y franjas · Fondo · Leyenda · Transición · Biblia. Nombre y tipo siempre a la vista. |
| Las plantillas eran botones con el nombre: había que probarlas una por una para saber cómo eran. | Cada plantilla es un dibujo de cómo queda. |
| La lista de temas mostraba sólo nombres. | Cada tema con su dibujo, si es de canciones o Biblia, y «En uso: canciones / Biblia». |
| Vista previa fija de 280 px, siempre sobre el tablero: no se sabía si un tema transparente se leía sobre un video claro. | Vista previa al medio y más grande. «Probar sobre» foto oscura o clara, «Texto largo» y «Ver la caja». |
| Elegir una tipografía no cambiaba nada hasta hacer clic en otro lado. | Cambia al elegirla, y la lista muestra cada letra con su forma. |
| Colores: un código (#FFFFFF) y un botón «Paleta…». | El color se ve al lado del código; se toca el color para cambiarlo. |
| Alineación y lugar de la leyenda en listas desplegables. | Botones (Izquierda/Centro/Derecha) y una pantallita donde se toca el lugar. |
| La paleta de colores se abría centrada en la ventana principal, no sobre el editor. | Se abre sobre la ventana que la pidió. |
| Guardar un tema releía cada canción de la biblioteca una por una (con 271 canciones, 272 lecturas). | Una sola lectura. |

**Falta probarlo con el programa abierto** (lo dibujé a imagen y pasa la
compilación y las 1.232 pruebas, pero no lo usé con el mouse). Qué mirar:

1. Cambiar algo, tocar otro tema → pregunta. Probar las tres respuestas.
2. Cambiar algo y cerrar con la X → pregunta.
3. Aplicar una plantilla, «Deshacer cambios» → vuelve a como estaba.
4. Arrastrar la caja, «Caja completa», «Centrar».
5. Elegir tipografía de la lista: cambia en el acto. Con muchas letras
   instaladas, que la lista no tarde en abrir.
6. Tema de Biblia: aparece la sección «Biblia»; al pasarlo a Canciones
   desaparece.
7. «Usar en canciones» / «Usar en Biblia»: cambia la marca «En uso» de la lista.

## 2. Posibles fallas (lo que puede salir mal un domingo)

### 2.1 Un error inesperado frena al operador con un cartel — prioridad alta

`App.xaml.cs` atrapa los errores de la ventana y muestra un cartel «Ocurrió
un error inesperado» que **hay que cerrar para seguir**. En pleno culto eso
significa que las flechas no pasan la letra hasta que alguien toque Aceptar. Y
si el error viene de algo que se repite (un reloj que corre cuatro veces por
segundo, como la cuenta regresiva), los carteles se apilan.

**Qué haría:** el error va al registro y a un aviso chico en la barra de abajo
(«Hubo un error; el programa sigue. Ver detalle»), sin cartel. Y como mucho un
aviso cada 30 segundos.

### 2.2 Los errores fuera de la ventana no quedan anotados — prioridad alta

Sólo se atrapan los errores del hilo de la ventana. Uno en segundo plano
(descarga, miniaturas, el servidor del celular, el video) cierra el programa
**sin dejar nada en el registro**: después no hay forma de saber qué pasó.

**Qué haría:** sumar los dos manejadores que faltan
(`AppDomain.UnhandledException` y `TaskScheduler.UnobservedTaskException`) que
escriban al registro antes de caer. Es chico y es lo que más ayuda a
diagnosticar una caída que te cuente otra iglesia.

### 2.3 No hay copia de seguridad automática — prioridad alta

Hay copia a mano (💾) y una automática antes de actualizar. Pero si el disco
falla o la base se daña entre una actualización y otra, la iglesia pierde
canciones, temas y playlists. Ahora que el programa sale a otras iglesias,
nadie va a hacer copias a mano.

**Qué haría:** una copia sola por semana al abrir el programa, en la carpeta
`respaldos`, guardando las últimas cinco. La función de copiar ya existe; es
sólo llamarla con un calendario. Y un renglón en «Acerca de» que diga cuándo
fue la última.

### 2.4 El PIN del celular viaja en la dirección — prioridad media

El control desde el celular y la salida para OBS llevan el PIN en la dirección
(`…/salida?pin=1234`) y el PIN son 4 cifras. Dentro de la wifi de la iglesia
alcanza, y ya hay límite de intentos. El riesgo real es otro: **una wifi
abierta al público**. Cualquiera conectado puede probar; con el límite de 5
intentos cada 5 minutos tardaría mucho, pero el PIN no cambia solo.

**Qué haría:** nada urgente. Para más adelante: PIN de 6 cifras opcional y un
aviso en la ventana del celular si la red es pública.

### 2.5 Sólo se prueba el «corazón», no las ventanas — prioridad media

Las 1.232 pruebas son todas de `EcclesiaCast.Core` (la lógica). Lo que maneja
la ventana principal (playlist con flechas, qué queda en vivo al cambiar de
tema, medios) no tiene ninguna, y es justo donde aparecieron casi todos los
errores de las betas.

**Qué haría:** un proyecto de pruebas para `EcclesiaCast.App` que arme el
modelo de la ventana con una base vacía y camine los casos que ya fallaron una
vez (flechas en la playlist, doble Enter en la cita, cambio de tema con algo
en vivo). No hace falta abrir ventanas para eso.

### 2.6 Detalles chicos

- Una advertencia del compilador que quedó (`_songMaxLines` en
  `MainViewModel.Songs.cs:57`): no rompe nada, conviene limpiarla.
- Mi banco de pruebas de hoy abrió el programa sin querer (por eso lo viste
  abrirse y cerrarse, y el aviso de «se cerró inesperadamente» la próxima vez
  que lo abras es por eso, no por una falla). Ya quedó corregido y anotado
  para que no vuelva a pasar.

## 3. Mejoras de diseño (cómo está armado)

### 3.1 La ventana principal es un archivo de 2.551 líneas

`MainWindow.xaml` tiene todo: canciones, Biblia, playlist, medios, vista en
vivo y las seis pestañas de la derecha. Su código suma otras 1.616. Cada
cambio obliga a leer y tocar ese archivo, y es fácil romper otra parte.

**Qué haría:** partirlo en piezas por panel (`PanelCanciones`, `PanelBiblia`,
`PanelPlaylist`, `PanelMedios`, `PanelEnVivo`, `PanelDerecho`), igual que se
hizo con `MainViewModel`. No cambia nada a la vista; abarata todo lo que venga
después. Es trabajo para una tanda propia, con el programa abierto para
comparar.

### 3.2 Los estilos de los formularios están copiados en cada ventana

El editor de temas, el diseñador de canciones y el de escenario definen cada
uno sus propios «campo», «rótulo» y «botón apretado». Hoy agregué los del
editor de temas en su ventana para no tocar el resto.

**Qué haría:** pasarlos a `App.xaml` una vez y que las tres ventanas usen los
mismos. Así un cambio de aspecto se hace en un lugar.

### 3.3 El diseñador de canciones tiene dos pestañas con el aspecto de Windows

En «Diseño de la canción», las pestañas **Texto | Caja** son blancas, del
estilo viejo de Windows, en medio de una ventana oscura. Se ve en la captura
de la página web (`sitio/img/diseno.webp`).

**Qué haría:** las mismas pastillas que ahora usa el editor de temas.

## 4. Experiencia de uso

Ordenado por cuánto ayuda al que está en la compu.

1. **Botones de arriba que son sólo un dibujo.** El de diseño de la ventana,
   el del celular y la lupa no dicen qué hacen hasta dejar el mouse encima. A
   una iglesia nueva le conviene el nombre al lado («Celular», «Buscar») o al
   menos en el recorrido de bienvenida.
2. **El diseñador de canciones y el editor de temas ahora se parecen menos.**
   El de temas quedó con vista previa de fondos, «Texto largo» y colores a la
   vista; al de canciones le vendría bien lo mismo (sobre todo «Probar sobre
   foto oscura / clara»).
3. **Paleta de colores.** Sigue siendo tres barras de rojo, verde y azul, que
   para alguien no técnico es difícil. Una paleta más grande, «usados hace
   poco» y los colores del tema actual resolverían el 95 % de los casos sin
   barras.
4. **Avisos que se pierden.** Los mensajes van a la barra de abajo en letra
   chica («Temas actualizados.»). Los importantes (algo falló, se guardó) se
   notan poco. Un aviso que aparece dos segundos cerca de donde se hizo clic
   se ve más.
5. **Deshacer.** No hay Ctrl+Z para lo que más duele: borrar una canción, un
   tema o un ítem de la playlist. Una papelera de 30 días para canciones y
   temas sale barata y salva domingos.
6. **Tipografías en la ventana principal.** Las listas de «Texto rápido» y
   «Aviso al pie» muestran sólo el nombre; podrían mostrar la letra con su
   forma como hace ahora el editor de temas.

### 4.1 Segunda pasada por todo el programa (10/11, tarde)

Recorrí menús, atajos, carteles y listas de la ventana principal. Lo que
encontré, de más a menos importante:

1. **No hay una ventana de «Ajustes».** Las preferencias están repartidas en
   cuatro menús: «Modo PC de bajo rendimiento» y «Reiniciar el sonido» en Ver,
   «Transiciones» y «Celular» en Proyección, «Buscar actualizaciones al abrir»
   en Ayuda, la pantalla de salida arriba a la derecha. Quien busca «dónde
   cambio tal cosa» no tiene un lugar. Una sola ventana Ajustes (Pantallas ·
   Sonido · Transiciones · Celular · Actualizaciones · Rendimiento) lo ordena,
   y es donde va a vivir la entrada de micrófono del seguimiento por audio.
2. **29 botones son sólo un dibujo.** Además de los de arriba, están los +, ✎,
   🗑 de canciones y playlist y los de medios. Los de borrar son los que más
   preocupan: un 🗑 chico al lado de un ✎ se aprieta sin querer. Separarlos y
   sumar la papelera (punto 5 de arriba).
3. **La Biblia se importa desde seis lugares** con nombres parecidos (Importar
   una Biblia, Importar paquete, Exportar paquete, Bajar de internet, Cómo
   preparar…, repetidos en Archivo y en la pestaña). Para una iglesia nueva
   alcanza «Bajar Biblias de internet…»; lo demás puede ir en un submenú
   «Más formas de traer una Biblia».
4. **47 carteles que frenan** (hay que tocar Aceptar). Varios son avisos que
   no piden decidir nada («Se restauró la copia», errores de importación):
   esos pueden ser un aviso que se va solo. Dejar cartel sólo donde hay que
   elegir.
5. **Listas vacías sin ayuda.** El historial de versículos y los medios dicen
   qué hacer cuando están vacíos; canciones y playlist no. Para el que instala
   por primera vez, «Todavía no hay canciones: buscá una en internet o
   importalas de otro programa» con los dos botones ahí mismo.
6. **Atajos de teclado.** Hay F1–F4, Esc, Ctrl+K y Ctrl+Enter, y nada más: no
   hay atajo para canción nueva, para la pestaña Biblia ni para la playlist.
   Y Esc **apaga la salida**, que es la tecla que todo el mundo aprieta para
   «cancelar»: conviene pedir doble Esc o que sólo valga con el foco en la
   grilla.
7. **Dos atajos para lo mismo** (Ctrl+K y Ctrl+F buscan en todo): está bien,
   pero el botón de la lupa no muestra ninguno.

## 5. El sonido que se quedó mudo — investigado y corregido ✅

Miré los registros reales del programa de los últimos días. El **31/10 a las
21:37** un dispositivo de audio se desconectó y volvió **diez veces en dos
segundos** (lo típico de un cable HDMI o USB flojo, o un proyector que
renegocia la señal) y terminó otra vez activo. El 3/11 pasó algo parecido.

El programa ya reabría el sonido cuando Windows **cambiaba** de salida. Pero
cuando la **misma** salida se cae y vuelve, Windows no avisa ningún cambio de
«salida predeterminada»: avisa sólo que el dispositivo volvió. Ese aviso el
programa lo anotaba y no hacía nada, así que lo que estaba sonando se quedaba
con una salida muerta hasta cerrar y abrir (o hasta Ver → Reiniciar el
sonido). En esos registros no aparece ni una sola vez «Se reabre la salida».

**Corregido:** cuando una salida vuelve a estar activa, el programa reabre el
sonido de lo que esté sonando, igual que si hubiera cambiado. Los micrófonos
no lo disparan. El registro ahora dice si el dispositivo era salida o entrada.

**Lo que no pude comprobar:** no tengo forma de desconectar un cable desde
acá. Para probarlo: poné un video con sonido, desenchufá y enchufá el HDMI (o
apagá y prendé el parlante Bluetooth) y tiene que volver solo en un segundo.
Si el problema que tuviste fue **otro** (por ejemplo: sonaba por los parlantes
de la notebook en vez de la consola, o un video puntual sin sonido), contame
qué viste y cuándo, y lo busco en el registro de ese día.

Aparte: ese cable o adaptador que se cae diez veces en dos segundos conviene
cambiarlo; el programa ahora lo aguanta, pero el corte se va a oír igual.

## 6. Orden que propongo

| # | Qué | Tamaño |
|---|---|---|
| 1 | Probar el editor de temas nuevo con el programa abierto | 15 min tuyos |
| 2 | 2.1 + 2.2: errores sin cartel y registro de caídas | chico |
| 3 | 2.3: copia de seguridad semanal | chico |
| 4 | 3.3 + 3.2: pestañas del diseñador y estilos compartidos | chico |
| 5 | 4.3 + 4.5: paleta mejor y papelera | mediano |
| 6 | 2.5: pruebas de la ventana principal | mediano |
| 7 | 3.1: partir la ventana principal | grande, tanda propia |

El seguimiento por audio tiene su plan aparte:
[plan-seguimiento-por-audio.md](plan-seguimiento-por-audio.md).
