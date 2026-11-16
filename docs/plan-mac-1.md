# Tanda 1 (Opus) — Prototipo de EcclesiaCast en Avalonia para Mac

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). Contexto:
[plan-mac.md](plan-mac.md), secciones 4 (criterios), 13 (cosas de macOS) y 16
(planes B). No leas el resto.

**Rama:** la de esta sesión, cortada de `main`. **No toques nada de `src/` ni
de `tests/`**: todo va en una carpeta nueva `proto/` y en un flujo nuevo de
GitHub. El prototipo se tira cuando termine la fase 2; no lo pulas.

## Para qué

Saber, con poco gasto, si Avalonia sirve para llevar EcclesiaCast a Mac.
Tiene que demostrar seis cosas en una Mac real con chip Apple y macOS 15 o
más nuevo (tabla «Obligatorios» de la sección 4 del plan). Alexis lo prueba en
una Mac prestada; vos no tenés Mac: lo tuyo se comprueba compilando, con
pruebas en memoria y con la Mac de GitHub.

## Qué hay que hacer

1. **Proyecto `proto/EcclesiaCast.Proto`** (Avalonia 12, `net10.0`,
   `WinExe`). Referencia `src/EcclesiaCast.Core` y `src/EcclesiaCast.Data` tal
   como están. Si `Core` o `Data` no compilan como referencia de un proyecto
   `net10.0` (hoy son `net8.0`: deberían), anotalo y no los toques.
   No lo agregues a `EcclesiaCast.slnx` (rompería el build de Windows con
   .NET 8): que tenga su propio `proto/Proto.slnx`.

2. **Datos.** Carpeta `EcclesiaCast-proto` dentro de
   `Environment.SpecialFolder.ApplicationData` (en Mac tiene que dar
   `~/Library/Application Support/EcclesiaCast-proto`; anotá en el registro la
   ruta real). Crea la base con las migraciones de `Data` (mirá cómo lo hace
   `src/EcclesiaCast.App/App.xaml.cs`, líneas 95 a 135). En el primer arranque
   importa `installer/bibles/RV1909.ecbiblias` (va copiada dentro del paquete)
   con el importador de `Core`, y crea dos canciones de prueba con el
   repositorio de `Data`. Registro con Serilog en `logs/` de esa carpeta.

3. **Ventana del operador** (fea está bien): lista de canciones, selector de
   libro, capítulo y versículo, lista de pantallas, botones «Proyectar»,
   «Apagar», «Anterior», «Siguiente», «Video de fondo» (elige un archivo) y
   una vista previa de la diapositiva. Un botón «Abrir carpeta de registros».

4. **Diapositiva.** Un control que dibuje a 1920×1080 virtual dentro de un
   `Viewbox`, como `src/EcclesiaCast.App/Controls/SlideView.xaml` (leé sólo las
   primeras 60 líneas del XAML y los métodos `BuildShadow` y `BuildOutline`
   del `.cs`). Texto centrado con ajuste de renglón, **contorno y sombra con
   `DropShadowEffect`** (uno en el contenedor y otro en el texto, igual que
   hoy). Fuente: incluí **Selawik** (OFL, de Microsoft) como recurso y usala
   donde el tema diga «Segoe UI». No hace falta el ajuste automático de tamaño.

5. **Video de fondo con la letra encima.** El mismo método que
   `src/EcclesiaCast.App/Controls/VlcVideoSurface.xaml.cs` (leé el comentario
   inicial y buscá `SetVideoFormatCallbacks`, `SetVideoCallbacks` y el
   callback de display): LibVLC entrega cada cuadro en memoria y se copia a un
   `WriteableBitmap` de Avalonia en el hilo de la pantalla; la letra es una
   capa normal encima. **Un solo reproductor para toda la vida del control y
   los delegados guardados en campos** (el comentario explica por qué). Bucle
   sin cuadro negro al volver a empezar. El registro anota cada 10 segundos:
   cuadros pintados por segundo, cuadros descartados y memoria del proceso.

   **Lo primero que tenés que averiguar:** si el paquete NuGet
   `VideoLAN.LibVLC.Mac` (3.1.3.2) trae librerías para `osx-arm64`. Si no las
   trae, plan B: en el flujo de GitHub, bajar el VLC oficial 3.0.x para chip
   Apple desde videolan.org, y copiar `libvlc.dylib`, `libvlccore.dylib` y la
   carpeta `plugins` dentro del `.app`, con `VLC_PLUGIN_PATH` puesto antes de
   `Core.Initialize`. Anotá cuál quedó y por qué. En Windows usá
   `VideoLAN.LibVLC.Windows` para poder compilar y probar en memoria.

6. **Ventana de salida.** Sin borde, del tamaño exacto de la pantalla elegida,
   sin robar el foco (`ShowActivated="False"`), cursor escondido, por encima
   de la barra de menú y del Dock. En Mac hace falta una llamada nativa: con
   el manejador de la ventana (`TryGetPlatformHandle`) y `objc_msgSend`,
   `setLevel:` por encima del menú principal y `setCollectionBehavior:` para
   que esté en todos los Spaces y no se mueva. **No uses la pantalla completa
   propia de macOS.** Con una sola pantalla, Esc y doble clic la cierran.
   Aislá lo nativo en una clase `MacWindowNative`, con un equivalente vacío
   para Windows.

7. **Pantallas en caliente.** Escuchá el cambio de pantallas de Avalonia. Si
   desaparece la pantalla donde está la salida: cerrar la salida, avisar en la
   ventana del operador y no colgarse. Si vuelve: se puede proyectar de nuevo
   sin reiniciar. Anotá en el registro cada cambio (cantidad de pantallas,
   tamaño y escala de cada una).

8. **Control desde el celular.** Un servidor mínimo propio sobre
   `TcpListener` en el puerto 8765 (el mismo método que
   `src/EcclesiaCast.App/Remote/RemoteControlServer.cs`; **no lo copies
   entero**, usa WPF en una línea). Una página con «Anterior» y «Siguiente».
   La ventana del operador muestra la dirección (`http://IP:8765`) y un QR
   (QRCoder). El registro anota la hora de cada pedido y la del cambio de
   diapositiva.

9. **Que la Mac no se duerma** mientras la salida está prendida: lanzar
   `/usr/bin/caffeinate -d -i -w <pid>` y terminarlo al apagar. En Windows,
   nada.

10. **Pruebas en memoria** (`proto/EcclesiaCast.Proto.Tests`,
    `Avalonia.Headless.XUnit` con Skia): dibujar una diapositiva con texto,
    contorno y sombra, guardar el PNG en la salida de la prueba y comprobar
    que no está vacío (hay pixeles claros en el centro). Una segunda prueba
    que crea la base en una carpeta temporal e importa la Biblia.

11. **Flujo `.github/workflows/proto-mac.yml`**, que corre sólo a mano
    (`workflow_dispatch`) y en los push a ramas `proto/**`:
    - en `windows-latest`: build y pruebas;
    - en `macos-15`: build, pruebas, `dotnet publish -r osx-arm64
      --self-contained`, armar `EcclesiaCast Proto.app` (`Contents/MacOS`,
      `Contents/Resources`, `Info.plist` con identificador
      `org.ecclesiacast.proto`, `LSMinimumSystemVersion` 15.0 y
      `NSHighResolutionCapable`), firma local
      (`codesign --force --deep --sign -`), y comprimir **con `ditto -c -k
      --keepParent`** (el artefacto de GitHub pierde los permisos de ejecución
      si se sube la carpeta suelta: se sube el `.zip` ya hecho).
    - El PNG de la prueba se sube como artefacto en los dos sistemas.

12. **Hoja de prueba `docs/pruebas-mac-1.md`** para Alexis, en español simple,
    con pasos numerados y los nombres exactos de tus botones: qué llevar
    (adaptador HDMI, TV o proyector, celular), cómo pasar el archivo a la Mac,
    el «Abrir igualmente» (Configuración del Sistema → Privacidad y seguridad;
    pedile captura de cada cartel), conectar la TV como **pantalla separada**
    (no en espejo), y un paso por cada punto P1 a P6 y por cada informativo de
    la sección 4 del plan, con qué mirar y qué foto sacar. Al final: «mandame
    las fotos y la carpeta de registros».

## Terminado cuando

- `dotnet build proto/Proto.slnx` sin advertencias y `dotnet test` en verde en
  esta computadora, **sin abrir ninguna ventana**.
- `dotnet build EcclesiaCast.slnx` sigue igual que antes (no se tocó).
- El flujo está escrito y revisado a mano (no se puede correr sin push; eso lo
  pide Alexis después de la revisión).
- La hoja de prueba cubre P1 a P6.

## Qué no hacer

Nada de `src/`. Sin WebView, sin captura de ventanas, sin PDF, sin editor de
temas, sin actualización. Sin firma de Apple. Sin versión para Intel.

## Informe

Además de lo de las reglas: de dónde sale LibVLC para chip Apple; qué llamadas
nativas hicieron falta; qué te hace dudar de que algún punto P1–P6 se cumpla
en la Mac real y qué probarías primero si falla; tamaño del `.zip` si se pudo
estimar.
