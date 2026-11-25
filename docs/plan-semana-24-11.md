# Plan de la semana del 24/11/2026: tokens, 1.5.0, redes y Mac

Escrito el 24/11. Cuenta **Pro**; el límite semanal se reinicia el **1/12 a las 20:59 (hora de Uruguay)**.
Hoy: 1 % usado en las 5 horas, 0 % en la semana.

## 1. Dónde se fueron los tokens de la semana pasada

**Cómo se midió.** Claude no puede ver el consumo por sesión. Se recorrieron los archivos de
conversación de esta PC (`~/.claude/projects`) desde el 16/11 y se sumó, por archivo, lo escrito en
caché (×1,25) más lo que el modelo contestó (×5). **Es una medida de comparación, no los tokens
reales**: sirve para ordenar quién gastó más, no para decir cuánto. Cuenta también los agentes
auxiliares, y los archivos modificados desde el 16/11 aunque la sesión sea más vieja.

| Grupo | Parte del consumo |
|---|---|
| EcclesiaCast (sin Mac) | **41 %** |
| GE (Bonos 2027, `geuy`) | **38 %** |
| Otros proyectos (sheep, la-guita-bo, iglesia-admin, Rifa) | **17 %** |
| EcclesiaCast Mac (tandas 1 a 12 y el plan) | **4 %** |

**Lo que sale de esto**

1. **La cuenta es una sola.** Más de la mitad (55 %) se fue en proyectos que no son EcclesiaCast,
   y GE solo gastó casi lo mismo que todo EcclesiaCast. Si el límite se comparte, nadie lo veía.
2. **Las sesiones maratón son las caras.** Las más pesadas de EcclesiaCast fueron sesiones de 36 a 57 MB de
   conversación («Sistema de proyección de letras», «Arreglos del formulario» y otras dos). En una
   sesión larga cada mensaje vuelve a escribir todo el historial: el costo no crece en línea recta,
   crece más rápido. **Las 10 sesiones más pesadas llevan cerca de un tercio (36 %) del total.**
3. **La Mac no fue lo que se comió la semana** (4 %). Las tandas eran cortas y con un encargo
   claro: ese formato funciona. Quedan abajo de la lista.
4. **Los encargos con muchas respuestas del modelo salen caros** (la salida pesa ×5). Redactar
   documentos largos, volver a leer archivos enteros y repetir pruebas pesan más que programar.

## 2. Reglas de uso (para que no se repita)

**Presupuesto semanal** (porcentaje del límite de la semana; se mira con `get_usage` al empezar):

| Proyecto | Tope |
|---|---|
| EcclesiaCast | 45 % |
| GE | 30 % |
| Resto | 10 % |
| **Reserva para publicar y urgencias** | **15 %** |

**Semáforo:** al llegar al **60 % el miércoles** se frena lo que no sea urgente. Al **80 %** sólo
queda publicar y responder. La reserva del 15 % **no se toca** para nada nuevo.

**Hábitos**
- **Una sesión, una tarea.** Terminada, se cierra y se abre otra. Pasadas las **150 vueltas** o el
  **50 % del contexto**, se cierra con un resumen de 10 líneas y se sigue en una nueva.
- **Sonnet por defecto**, esfuerzo bajo o medio. **Opus** sólo para diseñar algo difícil y para la
  revisión final antes de publicar. (Ya estaba en la memoria «Ahorrar tokens»: se cumple.)
- **Nada de varios agentes en paralelo** salvo que lo pidas. Un agente en fila.
- **Al empezar una sesión nueva:** decir el archivo y el objetivo. No pedir «analizá todo».
- **Probar con pruebas y capturas ya hechas**, no repetir builds completos sin necesidad.
- **Documentos largos, sólo si se van a usar.** Cada plan de 900 líneas cuesta como una tanda de programación.
- **Antes de un trabajo grande**, Claude te dice el % usado de la semana.

## 3. En qué quedamos con la 1.5.0

- **«Arreglos del programa pedidos en el formulario»:** **terminada y fusionada** (PR 36). No queda
  nada abierto ahí.
- **«YouTube sólo audio»:** fusionada (PR 43).
- **Abierta, sin fusionar: «Importar letras de Holyrics automáticamente» (PR 45).** Hay que decidir si
  entra a la 1.5.0.
- **Sin probar:** los **46 puntos** de `docs/pendientes.md` §1 (10 de la última tanda, 6 de la
  primera, 30 de versiones anteriores). Para cerrar la 1.5.0 no hace falta probar los 46: se propone
  probar **los 16 de la 1.5.0** y dejar los de versiones anteriores como «visto en uso».
- **Al publicar:** contador de uso (`worker.js`) y el cambio «➕ Agregar» en `cargar-canciones.html`.

### Arreglo nuevo: la dirección de OBS / vMix se pierde al reiniciar la PC

**Lo que se vio en el código** (`RemoteControlServer.cs`, `MainViewModel.Remote.cs`):
- La dirección que se muestra es `http://<IP de la red>:<puerto>`. La **IP de la red cambia** cuando la
  PC se reinicia (el router le da otra), y por eso hay que volver a pegarla en OBS o vMix.
- El **puerto** se guarda, pero si a las 8080 las ocupa otro programa al arrancar, el programa toma la
  siguiente libre y **guarda ésa**: la dirección cambia aunque la IP no. (A verificar con el usuario
  si le pasa.)

**Propuesta (una tanda chica de Sonnet):**
1. Para OBS o vMix **en la misma PC**, mostrar y copiar la dirección con **`localhost`**
   (`http://localhost:8080/…`), que **no cambia nunca** al reiniciar. Es lo que más probablemente pidió
   con «máscara».
2. Para otra PC de la red, mostrar el **nombre de la PC** (`http://NOMBRE-PC:8080/…`), que tampoco cambia.
   Sólo la IP queda como última opción, con el aviso «esta puede cambiar».
3. **No pisar el puerto guardado** cuando se usa uno de reemplazo; avisar «el puerto 8080 está ocupado».
4. Un botón **«Copiar todas las direcciones»** y un cartel «Dirección que no cambia» en la ventana.

Aviso: en OBS y vMix los nombres de PC funcionan si las dos PC están en la misma red; en un
Windows con IPv6 apagado puede hacer falta usar `localhost`. Se prueba con una captura.

## 4. Programas para mirar

### RW Presenter (Recursos Worship) — leído de su página oficial
- **Qué es:** programa **en el navegador** (también se instala en Windows 11, 105 MB, versión 0.0.4).
  **Sin Mac.** Suscripción: Gratis $0, Básico $14,97, Profesional $19, Director $37 por mes.
- **Lo que tiene y EcclesiaCast no tiene (o no lo encontré):**
  - Señal **Fill + Key** para mezcladores como ATEM, y salida con fondo transparente.
  - **Capas y escenas** guardadas (imágenes, videos y plantillas encima o debajo del texto).
  - **Macros, automatización** (avance automático, programador, línea de tiempo) y **variables**.
  - **Control con Stream Deck** (Bitfocus Companion) y perfiles de operador con permisos.
  - **Cambio de tono** de las letras, arreglo editable que reutiliza el coro.
  - **Generador de fondos animados** y una biblioteca propia de fondos y kits.
  - Importa **SongSelect, Proclaim, Holyrics, OpenLP, FreeShow** además de los que ya importamos.
- **Lo que EcclesiaCast tiene mejor:** es **gratis y sin cuenta**, **funciona sin internet** de verdad
  (instalado), para Windows con salida a proyector real, y tiene seguimiento de la canción por audio,
  placas, escenario y control desde el celular sin pagar.
- **Mi lectura:** su fuerza es el **ecosistema de recursos** (fondos, kits, plantillas) y la
  **integración con transmisión**. No hay que copiarlo; sí conviene mirar **Stream Deck / Companion**
  (barato de hacer y lo pide el público de OBS) y **cambio de tono**.
- **No tienen tutoriales** en la página (sólo marketing): ahí hay un hueco que podemos ocupar.

### Pendar Visual — **no se pudo leer**
La página (`lynk.id`) devuelve «403» y una verificación anti robots que no se puede saltar, y una
búsqueda no encontró nada. **Pegame el texto de la página** (o capturas) y lo comparo.

## 5. Tutoriales dentro del programa

Idea tuya: un apartado **Ayuda → Tutoriales** que use la misma pieza del recorrido de bienvenida.
Es buena: **el público lo pide, nadie más lo tiene (RW Presenter no), y baja las consultas por mensaje.**

**Propuesta**
- Una ventana **Ayuda → Tutoriales** con tarjetas; cada una arranca un recorrido corto (5 a 8
  pasos) **sobre el programa real**, con la flecha señalando el botón.
- **Primera tanda, 8 tutoriales** (los más pedidos): 1) Crear una canción nueva, 2) Armar la lista
  del domingo, 3) Proyectar la Biblia, 4) Poner fondos y videos, 5) Conectar con OBS o vMix, 6) Control
  desde el celular, 7) Placas de nombre, 8) Importar canciones de otro programa.
- **Segunda tanda:** escenario, seguimiento por audio, música de YouTube, avisos, temas.
- Cada tutorial es un archivo de texto (pasos + qué señalar), así se agregan sin tocar el código.
- **Tamaño:** 1 tanda de Sonnet para la ventana y el motor (reutiliza el recorrido) + 1 para los textos.
- De paso, cada tutorial puede salir como **video corto para redes** con la misma guía: se graba una vez.

## 6. Redes sociales

**Lo que no puedo hacer sin vos:** no veo tus mensajes ni comentarios. Para contestarlos hace falta
que me **pegues** los pendientes (o una captura por cuenta); con eso armo una **hoja de
respuestas** (las 8 o 10 preguntas que se repiten, con respuesta lista y tono de la marca) y vos
las mandás.

**Contestar (hoy, en 1 hora):**
1. Ordenar por tipo: *¿cómo lo descargo?*, *¿es gratis?*, *¿hay para Mac / celular?*, *tutorial*, *error*.
2. Las de «Mac» y «tutorial» se contestan con la misma frase: «Estamos en eso, dejamos aviso acá».
3. Los que hayan dejado un error se pasan a GitHub como pedido.

**Plan de publicaciones después de la 1.5.0** (3 por semana, sin quedarse en silencio):

| Día | Formato | Contenido |
|---|---|---|
| Lun | Historia | Lo que viene esta semana / pregunta al público |
| Mié | Reel o TikTok corto (15–30 s) | Un tutorial de los 8 de arriba, de a uno por semana |
| Vie | Publicación fija | Novedad de la versión, o «caso de iglesia» (con permiso) |
| Cada 2 semanas | Respuesta en video | Contestar la pregunta más repetida |

- **Semana de la 1.5.0:** 1) historia «sale la 1.5.0» con lo nuevo (placas, YouTube sólo audio, ventana
  flotante), 2) un reel de placas de nombre (lo más visual), 3) una publicación con el enlace.
- **Lo que más rindió** (según las métricas del 15 y 19/11): los reels cortos que muestran el programa
  funcionando. Seguir por ahí, y **sumar una llamada a la acción** («probalo gratis: ecclesiacast.org»).
- **La mayoría del público vino de Centroamérica:** conviene un reel pensado para ellos (el «español»
  neutro, sin voseo).
- **Después de publicar:** pedir a las iglesias que ya lo usan una estrella en GitHub (ayuda a volver a
  pedir la firma a SignPath en diciembre).

## 7. Mac: cómo seguir (sólo la lista)

Estado: plan de 6 fases en `docs/plan-mac.md` (970 líneas, rama `mac`); hechas las tandas 1 a 12
(prototipo, pruebas de Core, .NET 10, interfaces, ViewModels, bancos de render, ventanas, letra,
comparador). Lo que sigue, en orden:

1. **Fusionar y estabilizar:** pasar a `main` lo de la fase 1 (Puerta 1) y dejar la rama `mac` al día.
2. **Terminar la fase 2 («la salida»):** proyector, video con LibVLC y audio en Avalonia (tandas 13 a 18).
3. **Fase 3: la ventana del operador y los diálogos** (muchas pantallas: es lo más largo).
4. **Probar en una Mac real prestada** antes de seguir: apenas se vea la salida, antes de
   invertir en la fase 3.
5. **Fase 4: empaquetar** (`.app` / `.dmg`) y publicar como **«beta» sin cuenta de Apple** (ya decidido).
6. **Documentación y avisos de «qué falta en Mac»**, y avisar en redes que está en camino.
7. **Después:** igualar a Windows y retirar WPF (fase 5), sólo si rinde bien.
- **Cuidado de tokens:** una tanda por semana, de a una, de Sonnet; Opus sólo para las puertas. La Mac
  **no corre mientras la 1.5.0 no esté publicada** y la reserva del 15 % esté a salvo.

## 8. Orden propuesto de esta semana

1. **Hoy:** decidir PR 45 (Holyrics), pegar mensajes de redes y el texto de Pendar Visual.
2. **Hoy–mañana:** tanda chica «dirección fija para OBS» (Sonnet).
3. **Vos probás los 16 puntos de la 1.5.0** y nos decís los números.
4. **Publicar la 1.5.0** (con el contador y el cambio de `cargar-canciones.html`) y empezar la
   vuelta de redes de la semana de arriba.
5. **Siguiente semana:** tutoriales dentro del programa (tanda 1) y el listado de Mac a una tanda por semana.
