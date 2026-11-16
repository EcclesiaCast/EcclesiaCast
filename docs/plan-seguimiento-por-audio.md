# Plan: que el programa siga la canción por lo que escucha

10/11/2026. Idea de Alexis: lo que sale de la consola entra a la PC por la
entrada de micrófono (toda la banda). Elegida la canción, el programa escucha
y pasa las diapositivas solo. Dos formas:

- **Seguimiento:** cuando el cantante canta una frase, se pone esa diapositiva.
- **Karaoke (anticipado):** el programa se adelanta y pone la diapositiva que
  viene antes de que el cantante la empiece.

## 1. ¿Se puede? Sí, con tres verdades por delante

**1. No hace falta «entender» todo lo que se canta.** Reconocer canto con una
banda encima es difícil y los programas se equivocan mucho. Pero acá el
programa **ya sabe la letra**: sólo tiene que decidir en cuál de 6 o 10
diapositivas está. Eso es muchísimo más fácil: alcanza con pescar algunas
palabras de cada renglón.

**2. El modo «seguimiento» puro llega tarde, siempre.** Para saber que el
cantante empezó una frase hay que escucharla: entre 1 y 3 segundos. La
congregación necesita la letra **antes** de cantarla. Así que seguimiento solo
sirve para corregir («saltaron al coro, voy al coro»), no para manejar el
culto. **El modo que de verdad sirve es el anticipado**: el programa escucha
el *final* de la diapositiva actual y cambia a la siguiente justo antes de que
termine. El seguimiento queda como red: si lo que escucha después no coincide
con donde está, salta a donde sí.

**3. Con toda la banda mezclada rinde peor que con la voz sola.** Si la
consola tiene un auxiliar libre, mandar a la PC **sólo las voces** (o las
voces bien arriba) cambia todo. No es obligatorio, pero es la mejora más
grande y es gratis. Hay que probarlo de las dos maneras antes de decidir.

Lo que **no** va a hacer bien, y hay que saberlo de entrada: partes
instrumentales largas (no hay voz que seguir), canto espontáneo que no está
en la letra, y dos diapositivas con el mismo texto (las distingue sólo por el
orden).

## 2. Cómo funcionaría por dentro

```
Entrada de micrófono
      │  (audio, 16.000 muestras por segundo)
      ▼
Reconocedor de voz  ──►  palabras sueltas, con errores
(en la PC, sin internet)        «santo santo señor potente»
      │
      ▼
Seguidor de letra  ──►  «diapositiva 2, renglón 2, confianza 85 %»
(compara con la letra de la canción elegida)
      │
      ▼
Piloto  ──►  pasa la diapositiva (o sólo la sugiere)
(reglas de seguridad: cuándo se anima a cambiar)
```

**Reconocedor de voz.** Tiene que andar sin internet (el programa lo promete)
y ser libre. Tres candidatos, todos gratuitos y que funcionan dentro del
programa:

| Motor | A favor | En contra |
|---|---|---|
| **sherpa-onnx** con modelo en español que transmite palabra por palabra | Poca demora; se le puede decir «prestá atención a estas palabras» (las de la canción) | Hay que comprobar cuánto aguanta la música de fondo |
| **Vosk** (modelo chico de español, ~40 MB) | Liviano; se le puede dar la lista exacta de palabras de la canción, y con eso acierta mucho más y más rápido | Es el que peor tolera música de fondo |
| **Whisper** (whisper.cpp) | El que mejor aguanta música y ruido | No transmite: trabaja de a tramos de 2–5 segundos; más demora y más procesador |

No hay forma de elegir en el papel: **lo decide una prueba con grabaciones
reales de tu iglesia** (fase 0). Mi apuesta es sherpa-onnx o Vosk con el
vocabulario de la canción para el anticipado, y Whisper como comparación.

**Seguidor de letra.** Es nuestro, va en `EcclesiaCast.Core` y se prueba sin
audio. Recibe palabras con errores y la letra, y responde dónde está:

- Compara por sonido, no por letra: sin tildes, `b=v`, `s=z=c`, `ll=y`, sin
  `h`. «Vendita» y «bendita» son lo mismo.
- Prefiere quedarse o avanzar al siguiente renglón; para saltar lejos (volver
  al coro) pide más pruebas.
- Dice cuánta confianza tiene. Sin confianza, no hace nada.

**Piloto.** Las reglas que lo hacen confiable:

- **El operador siempre manda.** Una flecha o un clic a mano pausa el piloto
  unos segundos y se reengancha desde donde quedó el operador.
- Sólo se mueve **dentro de la canción elegida**. Nunca cambia de canción,
  nunca borra la pantalla, nunca toca la Biblia ni los medios.
- No cambia dos veces en menos de un segundo y medio.
- Si hace rato que no entiende nada (silencio, el pastor hablando, un solo de
  guitarra), se queda quieto y lo muestra.
- Se enciende y se apaga con una tecla, y en pantalla se ve qué escucha y
  dónde cree que está.

**«Que aprenda».** Lo que el programa puede aprender de verdad, sin magia:

- **Tu forma de tocar cada canción:** cada vez que se proyecta, anota cuánto
  duró cada diapositiva y en qué orden fueron (verso, coro, coro, puente…).
  La vez siguiente ya sabe que «después del verso 2 casi siempre repiten el
  coro» y cuánto falta más o menos para el cambio. Sirve aunque el
  reconocedor esté dudando, y sirve para las partes instrumentales.
- **Cuánto adelantarse:** si el operador corrige siempre medio segundo antes,
  el piloto se ajusta.
- No entrena un modelo de voz propio: eso necesita miles de horas y no vale
  la pena. Usa uno hecho y aprende *de tu iglesia* lo de arriba.

## 3. Las dos formas, como las vería el operador

**Seguimiento.** El programa marca con borde la diapositiva que escucha. En
«sugerir» sólo la marca (el operador aprieta Enter); en «automático» la pone
sola. Uso real: red de seguridad y canciones donde la banda salta de parte.

**Anticipado (karaoke).** Cuando escucha que va por las últimas palabras de la
diapositiva actual, pasa a la que sigue en el orden de la canción. Se elige
cuánto adelantarse: «al empezar el último renglón», «faltando 3 palabras» o
«al terminar». Si después escucha que no era por ahí, el seguimiento corrige.

No incluyo iluminar palabra por palabra como un karaoke de televisión: es otra
cosa (letra animada) y ya dijiste que no te interesa. Si era eso lo que
tenías en mente, decime y lo sumo.

## 4. Fases

Cada fase termina en algo que se puede probar, y si una sale mal se frena ahí
sin haber gastado en las siguientes.

### Fase 0 — Probar si alcanza (antes de escribir nada en el programa)

**Lo que necesito de vos:** dos o tres cultos grabados tal como entra el
sonido a la PC (con Audacity u OBS, en WAV o MP3), con 3 o 4 canciones cada
uno. Si podés, una vez con toda la banda y otra con sólo las voces por un
auxiliar. Y la letra de esas canciones como está en el programa.

**Lo que hago:** un banco aparte (no toca el programa) que pasa esas
grabaciones por los tres motores y mide, contra los cambios que hiciste a
mano: cuántas diapositivas acierta, con cuánta demora y cuánto procesador usa
mientras hay un video de fondo andando.

**Se sigue sólo si** algún motor acierta 9 de cada 10 cambios con la voz que
realmente tenés. Si no llega, el resultado igual dice qué falta (por ejemplo:
«con auxiliar de voces sí, con todo mezclado no»).

### Fase 1 — Escuchar

Elegir la entrada de sonido en Ajustes, un medidor de nivel para ver que
llega señal, y aviso si satura o no entra nada. El modelo de voz se baja la
primera vez desde el programa (como las Biblias), no va en el instalador.
Nada se graba ni sale de la PC; se agrega a la página de privacidad.

### Fase 2 — El seguidor de letra (en Core, con pruebas)

La comparación por sonido, el cálculo de «dónde estoy» y la confianza. Se
prueba con textos armados a propósito con errores, y con lo que devolvieron
los motores en la fase 0.

### Fase 3 — Seguimiento en «sugerir»

Botón 🎤 al lado de la canción. Marca la diapositiva que escucha, sin
proyectar. Se usa en dos o tres cultos **al lado del operador**, que sigue
pasando a mano. El programa anota cuántas veces coincidió con él. Es la
prueba de fuego y no puede arruinar un culto.

### Fase 4 — Automático y anticipado

Con buenos números en la fase 3: el piloto con sus reglas, el modo anticipado
y el ajuste de cuánto adelantarse. Tecla para encender y apagar, y el estado
bien a la vista («Siguiendo · confianza alta» / «No escucho la voz»).

### Fase 5 — Aprender de cada culto

Guardar duración y orden por canción, y usarlos para anticipar mejor y para
las partes sin voz. Pantalla chica por canción: «las últimas 5 veces fue así».

### Fase 6 — Pulido

Otros idiomas (portugués, inglés), que no le saque procesador al video (el
reconocedor corre con prioridad baja y con un tope de núcleos), guía de uso y
el consejo del auxiliar de voces.

## 4.1 Estado al 10/11/2026

Respuestas de Alexis: **no hay salida sólo de voces** (entra toda la banda),
**karaoke = adelantarse a la diapositiva, sin resaltar palabras**, y las
grabaciones están en Facebook (pasa los enlaces).

**Hecho — fase 2, el seguidor de letra** (`src/EcclesiaCast.Core/Listening/`,
25 pruebas en `tests/.../Listening/FollowPilotTests.cs`):

- `LyricSounds`: pasa las palabras a cómo suenan («vendita» = «bendita»,
  «yeno» = «lleno», «sielo» = «cielo») y tolera una letra de diferencia en
  palabras largas.
- `LyricFollower`: con las últimas 7 palabras escuchadas dice en qué
  diapositiva está el canto, cuántas palabras le faltan y con cuánta
  confianza. Si un renglón se repite en varias, gana la que está en pantalla.
- `FollowPilot`: decide cuándo cambiar. Modo anticipado (pasa cuando faltan 2
  palabras o menos) y modo seguimiento. No cambia dos veces en menos de 1,5 s,
  espera 3 s después de que el operador mueve a mano, no se mueve cuando
  alguien habla, y sigue un salto atrás sólo si es claro.

Probado con «Santo, santo, santo» cantado «con errores» (palabras comidas y
mal escritas): recorre las cuatro diapositivas en orden. **Todavía no escucha
nada**: falta el oído (fases 0 y 1).

**Sigue — fase 0 con las grabaciones de Facebook.** Como entra toda la banda,
el motor a probar primero es Whisper (el que mejor aguanta música); después
Vosk y sherpa-onnx con el vocabulario de la canción. Para cada grabación
necesito: el enlace, qué canciones son y que estén cargadas en el programa
con esa letra. El audio de Facebook viene comprimido y con el sonido de la
transmisión, que suele ser mejor mezcla que la entrada de la PC: sirve para
saber si la idea funciona, y después hay que repetir con una grabación hecha
en la PC del culto.

## 4.2 Resultado de la fase 0 (11/11/2026)

Probado con el audio de los cultos del 25/10, 1/11 y 8/11 bajado de Facebook
(la transmisión, con toda la banda), en la notebook del culto, sin placa de
video dedicada.

| Reconocedor | Qué entendió del canto | Cuánto tarda por cada 8 s de audio |
|---|---|---|
| Whisper «base» | Nada: escribe «[Música]» | 0,3 s |
| Whisper «small» | Palabras sueltas y frases inventadas | 2 a 5 s |
| Whisper «large-v3-turbo» entero | Casi toda la letra de las canciones claras | 11 s (no sirve en vivo) |
| **«large-v3-turbo» compacto (q5) con contexto corto** | **Casi toda la letra de las canciones claras** | **2,9 s** |

- **Lo que anda:** canciones a ritmo medio con la voz adelante. De «La
  alegría» (8/11) entendió «la alegría del Señor para siempre es la fuerza
  de mi vida», «mi refugio, la esperanza que da vida», renglón por renglón.
- **Lo que no anda todavía:** canciones rápidas con la banda fuerte. De «En
  el nombre de Jesús» (1/11) casi no sacó nada útil («viviré, no moriré»
  salió como frases sin sentido). También se pierde en las entradas
  instrumentales, que es esperable.
- **Cuando habla alguien** lo entiende perfecto; por eso el piloto sólo
  actúa si las palabras coinciden con la canción abierta.
- **Darle la letra como pista empeora:** el reconocedor empieza a «escuchar»
  la letra aunque no la estén cantando. Descartado.
- **Inventa cosas sobre la música** («Música», «¡Suscríbete al canal!», un
  renglón repetido): se filtran antes de llegar al piloto (`HeardText`).
- **Demora total:** unos 3 segundos de reconocedor más lo que tarda en
  juntarse la frase. Por eso «adelantarse» viene con 5 palabras de margen en
  vez de 2, y se ajusta en la ventana.
- De las 15 canciones de esos tres cultos sólo 3 estaban en la biblioteca de
  esta PC, así que **no hay todavía una medición de aciertos sobre un culto
  entero**: eso sale de usarlo en modo «sugerir» y leer el registro.

**Decisión:** alcanza para salir en prueba, no para confiarle un culto.
Arranca en «sólo sugerir».

**Hecho — fases 1, 3 y 4 en versión de prueba** (`Services/Listening/`,
`MainViewModel.Listening.cs`, `Views/ListeningWindow`): entrada de sonido
con medidor, descarga del reconocedor (unos 550 MB, una vez), y los tres modos.
Cada cosa que escucha y cada decisión quedan en el registro del día
(`Seguir la canción: …`), que es lo que hay que mirar después de cada
domingo para ajustar.

**Falta:** medir con cultos reales en «sugerir»; aprender el orden y la
duración de cada canción (fase 5); probar la placa de video integrada para
bajar la demora; otros idiomas.

## 4.3 Segunda vuelta: más rápido y con la forma de la canción (13/11/2026)

Alexis probó la beta el 12/11 con lo que suena en la PC: la letra se entendía,
pero iba unos 6 segundos atrás, saltaba mal (de la 1 a la 6, de la 10 a la 14)
y con el micrófono de la notebook no escuchaba nada.

**Cómo se midió.** Se grabó lo que suena en la PC (16 kHz, como lo escucha el
programa) con «Bautizados en Fuego» (20 diapositivas, 5 min 20 s, en vivo, con
una parte hablada en el medio) y «Más Fuerte que Nunca» (6 diapositivas). A
mano se anotó en qué segundo se empieza a cantar cada diapositiva (con un
margen de unos 2 segundos). Un banco aparte (sin abrir el programa) hace de
cuenta que escucha en vivo: cada pasada tarda lo que de verdad tarda el
reconocedor y la siguiente empieza cuando termina. «Retraso» es cuánto después
de empezar a cantarse la diapositiva aparece en pantalla (negativo = antes).
«Equivocado» es poner una que no es ni la que se canta ni la que sigue.

| | Cambios acertados | Retraso (mediana) | Peor | Equivocados |
|---|---|---|---|---|
| **Bautizados · seguir** — antes | 6 de 18 | +11,0 s | +13,9 s | 3 |
| después, con placa de video | 16 de 18 | +4,4 s | +8,1 s | 1 |
| después, sólo procesador | 15 de 18 | +6,4 s | +11,4 s | 1 |
| **Bautizados · adelantarse** — antes (5 palabras) | 14 de 18 | +1,6 s | +12,6 s | 1 |
| después, con placa de video (2 palabras) | 17 de 18 | −1,7 s | +7,6 s | 1 |
| después, sólo procesador (2 palabras) | 16 de 18 | 0,0 s | +8,2 s | 0 |
| **Más Fuerte · seguir** — antes | 2 de 5 | +13,6 s | +13,6 s | 4 |
| después, con placa de video | 5 de 5 | +3,6 s | +7,6 s | 0 |
| después, sólo procesador | 4 de 5 | +8,9 s | +16,5 s | 1 |
| **Más Fuerte · adelantarse** — antes (5 palabras) | 5 de 5 | +2,1 s | +26,2 s | 2 |
| después, con placa de video (2 palabras) | 5 de 5 | −2,4 s | +1,6 s | 0 |
| después, sólo procesador (2 palabras) | 5 de 5 | −0,3 s | +1,2 s | 0 |

En «seguir» siempre hay 2 o 3 segundos que no se pueden sacar: hay que oír
las primeras palabras de la diapositiva para saber que empezó. Por eso el modo
que sirve para el culto es «adelantarse». Los cambios que faltan en
«Bautizados» son las diapositivas de «Fuego, fuego» gritado, que el
reconocedor no entiende.

**Ojo:** las reglas se ajustaron mirando estas mismas dos canciones, así que
con otras los números van a ser algo peores. Falta medir con canciones que no
se usaron para ajustar, y con el sonido de la consola en un culto.

**Qué cambió.**

1. **Placa de video.** El reconocedor corre en la placa (Vulkan: sirve para
   AMD, Intel y Nvidia) y cae al procesador si no puede. En la notebook
   (Radeon 860M integrada) una pasada de 6 s de sonido bajó de 2,7 s a 0,45 s.
   La primera pasada prepara la placa y tarda unos 10 s: se hace al encender,
   no en la primera frase. El instalador crece unos 58 MB.
2. **Pasadas cortas y seguidas.** Ventana de 6 s (antes 8) y una pasada por
   segundo (antes cada 3). Además se le corta el paso cuando entra en bucle
   repitiendo una frase, que era lo que causaba las pasadas de 5 a 10 s.
3. **La forma de la canción** (`LyricFollower`, `FollowPilot`). Lo escuchado
   se compara con la canción entera en orden, así que una frase que va del
   final de una diapositiva al principio de la otra se reconoce igual. El
   piloto lleva la cuenta de por dónde va el canto y sabe que avanza a ritmo
   de voz: entre copias de la misma letra (un coro repetido, dos diapositivas
   iguales seguidas) elige la que el canto pudo haber alcanzado. Ir a la
   siguiente es lo esperado; saltar a otra parte pide palabras claras y
   escucharlo dos pasadas seguidas; salir de una diapositiva que nadie cantó
   todavía (alguien hablando antes de empezar) también pide más. Las palabras
   cortas («de», «la», «y») valen la mitad. Ya no se tira lo escuchado después
   de cada cambio, que dejaba al programa sordo unos segundos.
4. **Adelantarse cuenta lo que tardó el reconocedor** (a 1,5 palabras por
   segundo), así que el margen viene en 2 palabras en vez de 5.
5. **Micrófono.** Lo del 12/11 no era el reconocedor: el programa descartaba
   por «silencio» todo lo que no llegara al 1 % del volumen, y el micrófono
   de la notebook no llegaba. Ahora el corte está en 0,2 %, el sonido bajo se
   sube antes de reconocerlo (hasta 40 veces) y el nivel de cada pasada queda
   en el registro. Probado bajando 10 veces la grabación: se entiende igual.
   **No está probado con un micrófono de verdad** captando un parlante.

**No se probó:** sherpa-onnx ni Vosk (reconocedores que
transmiten palabra por palabra). Con la placa de video Whisper ya responde en
medio segundo y es el único que entendió canto con banda en la fase 0; no
valía la pena sumar otro motor y otro modelo para bajar.

**Banco:** queda fuera del repositorio (tiene rutas de esta PC), con el audio
y las referencias, en la carpeta de notas de la sesión. Comandos: `rec`
(graba lo que suena), `probe`, `cost`, `sim` y `replay` (repite una
simulación con el piloto de ahora sin volver a reconocer).

## 5. Riesgos

| Riesgo | Qué hacemos |
|---|---|
| No acierta con toda la banda mezclada | Fase 0 lo dice antes de construir. Salida: auxiliar de voces. |
| Le saca procesador al video de fondo en la notebook | Se mide en fase 0 con video andando; prioridad baja y tope de núcleos. |
| Un cambio equivocado delante de la congregación | Fase 3 en «sugerir» durante varios cultos; el operador siempre manda; confianza mínima. |
| El modelo pesa (40 MB a 500 MB según el motor) | Se baja aparte, sólo quien use la función. |
| Canto espontáneo, repeticiones libres | El piloto se queda quieto y avisa; el operador sigue a mano. |

## 6. Lo que tenés que decidir

1. ¿Podés grabar esos cultos para la fase 0? Sin eso, todo lo demás es
   adivinar.
2. ¿La consola tiene un auxiliar libre para mandar sólo voces a la PC?
3. ¿«Karaoke» es adelantarse a la diapositiva (lo de este plan) o también
   iluminar palabra por palabra?

## Fuentes consultadas

- Alineación de letra en tiempo real (el problema es distinto de transcribir
  y la melodía ayuda): <https://arxiv.org/abs/2401.09200>
- sherpa-onnx, palabras clave para modelos que transmiten:
  <https://k2-fsa.github.io/sherpa/onnx/hotwords/index.html>
- Vosk, modelos y vocabulario propio: <https://alphacephei.com/vosk/models> ·
  <https://alphacephei.com/vosk/lm>
- Whisper para .NET: <https://github.com/sandrohanea/whisper.net>
