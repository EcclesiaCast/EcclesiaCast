# Tanda 10 (Opus) — Video y la salida completa en Avalonia

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md). Contexto:
[plan-mac-fase-2.md](plan-mac-fase-2.md). Del plan general, la sección 16
(«Video») de [plan-mac.md](plan-mac.md).

**Rama:** la de esta sesión, cortada de `mac` con las tandas 9 y 12
fusionadas. **Tamaño:** mediano a grande.

## Para qué

Lo que se proyecta no es sólo la letra: es una pila de capas. De abajo hacia
arriba:
1. color o imagen de relleno;
2. imagen de fondo;
3. video;
4. diapositiva;
5. logo;
6. anuncio.

Al cambiar de diapositiva, la anterior se funde con la nueva. Hoy todo esto
lo arma `App/Controls/ProjectedView.xaml(.cs)`, y el video lo pinta
`App/Controls/VlcVideoSurface.xaml.cs`. Esta tanda los hace en Avalonia.

## Antes de escribir

`VlcVideoSurface.xaml.cs` y `ProjectedView.xaml.cs` son grandes: buscá con
Grep y leé de a tramos. Leé completo `proto/EcclesiaCast.Proto/VideoBackground.cs`
de la rama `claude/mac-tanda-1-prototipo-ddaace`: es una versión chica de lo
mismo, ya hecha en Avalonia, con las reglas aprendidas.

**Reglas del video que no se negocian** (están en los comentarios de los dos
archivos):
- Hay **un solo reproductor** por superficie, durante toda su vida.
- Los delegados de LibVLC se **guardan en campos**. Si el recolector los
  libera, el programa se cae sin aviso.
- La función de formato **nunca espera** al hilo de la pantalla.
- El cuadro se copia a la imagen **en el hilo de la pantalla**.

## Qué hay que hacer (un commit por punto)

1. **`VlcVideoSurface` en Avalonia** (`Desktop/Controls/`), con
   `WriteableBitmap` de Avalonia. Tiene que poder lo mismo que la de WPF:
   - **El bucle:** no puede quedar un cuadro negro al volver a empezar. El
     «hold» con fundido (`HoldFade`, `ReleaseHold`) se copia tal cual.
   - **El final del video:** respeta `VideoEndBehavior`.
   - **Pausa, buscar y saltar:** como pide `IProjectionWindowService`.
   - **La vista previa del operador:** la copia chica del cuadro.
   - **El fondo desenfocado:** `_backdropBitmap`.
   - **El sonido:** sale por la salida elegida.
   - **Lo que es propio de WPF** (`D3DImage`, si lo usa, o el modo de bajo
     rendimiento) se copia sólo si tiene sentido en Avalonia. Si no lo tiene,
     se anota.
   - **Las cuentas:** si una se puede separar del dibujo (tamaños, cuándo
     soltar el «hold», el encuadre), va a `Presentation` y también la usa
     WPF. Vale la regla de la tanda 9: el banco de WPF tiene que seguir dando
     lo mismo, y las pruebas de esa cuenta van en `Presentation.Tests`.

2. **`ProjectedView` en Avalonia**, con todas las capas de la de WPF:
   - **Capas:** la anterior (para el fundido entre diapositivas), el relleno
     de color o imagen, la imagen de fondo, el video, `SlideView` y el logo,
     incluido el **logo en video**.
   - **Encuadre:** el zoom y el desplazamiento salen de `FillFraming` (Core),
     que ya es común. Lo que en WPF hace `App/Services/MediaFraming.cs`
     (transformaciones, desenfoque, recorte) se hace en Avalonia en
     `Desktop/Services/MediaFraming.cs`.
   - **Capas que en Mac no existen** (puntero del panel web, panel web,
     captura de ventanas): no se dibujan. Donde `IPlatformInfo` diga que la
     función existe (en Windows), dejá un comentario que diga dónde iría. Las
     hace la fase 5.

3. **`FramedMediaPreview` en Avalonia.** La usan logos y propiedades del
   medio en la fase 3, y comparte el encuadre. Si es grande, hacé sólo lo que
   necesita la salida y anotá lo que falta.

4. **Pruebas** en `Desktop.Tests`:
   - El fundido entre dos diapositivas termina mostrando sólo la nueva.
   - El relleno con zoom y desplazamiento pone la imagen donde dice
     `FillFraming`.
   - **El video con LibVLC de verdad**, si se puede sin ventana:
     - Se usa un video chico de prueba, de 3 segundos, en `tests/medios/`.
       Lo armás vos con lo que haya; si no hay con qué, la prueba va sin
       video.
     - Se reproducen 2 segundos.
     - Se comprueba que llegaron cuadros y que el bucle no dejó un cuadro
       negro.
     - LibVLC para Windows ya viene con el programa. Para Mac, el paquete
       `VideoLAN.LibVLC.Mac` 3.1.3.2 trae chip Apple: lo comprobó la tanda 1.
     - Si en `test-mac` no se puede cargar, la prueba se saltea ahí con un
       mensaje y se anota.

5. **`CHANGELOG.md`:** ampliá la línea de la tanda 9; no agregues otra.

## Terminado cuando

- El build sale sin advertencias y las pruebas pasan.
- El banco de WPF sigue idéntico a la referencia.
- La salida de Avalonia arma todas las capas que existen en Mac.
- El informe dice qué capacidades del video de WPF quedaron afuera y por qué.

## Qué no hacer

- Ninguna ventana: ni la de salida ni la de escenario. Son de la tanda 11.
- No cambiar nada del video en WPF, salvo mudar una cuenta, y comprobado con
  el banco y con pruebas.
