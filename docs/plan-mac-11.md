# Tanda 11 (Opus) — Ventanas de salida y de escenario, y el programa de prueba para la Mac

**Leé antes:**
- las reglas: [plan-mac-reglas.md](plan-mac-reglas.md);
- el contexto: [plan-mac-fase-2.md](plan-mac-fase-2.md);
- del plan general [plan-mac.md](plan-mac.md), sólo estas dos secciones:
  - la 13, todas las filas sobre ventanas, pantallas, Retina, sueño y App
    Nap;
  - la 4, los seis puntos obligatorios.

**Rama:** la de esta sesión, cortada de `mac` con las tandas 9, 12 y 10 ya
fusionadas. **Tamaño:** mediano.

## Para qué

La letra y el video ya están hechos. Falta ponerlos en pantalla:
- la ventana de salida, a pantalla completa en el proyector y sin quitarle el
  foco al operador;
- la ventana de escenario.

Además, para la segunda visita a la Mac hace falta algo que se pueda abrir y
manejar. La ventana del operador de verdad es de la fase 3. Por ahora alcanza
con un **programa de prueba** chico que use el `MainViewModel` de verdad.

## Qué hay que hacer (un commit por punto)

1. **`IOutputWindowNative`**, en `Presentation/Platform`. Pone una ventana a
   pantalla completa en la pantalla que se le diga, por encima de todo y sin
   tomar el foco.
   - **En Windows:**
     - hoy lo hacen `App/Views/OutputWindow.xaml.cs` y `StageWindow.xaml.cs`
       con `SetWindowPos` y `WS_EX_NOACTIVATE`;
     - ese código se muda a `Platform.Windows`;
     - las dos ventanas de WPF pasan a usarlo, **con las mismas llamadas y en
       el mismo orden**.
   - **En Mac:**
     - va en un proyecto nuevo, `src/EcclesiaCast.Platform.Mac` (net10.0);
     - se toma de `proto/EcclesiaCast.Proto/MacWindowNative.cs`, en la rama
       de la tanda 1;
     - pone la ventana por encima de la barra de menú, la deja en todos los
       Spaces y la estira al marco entero de la pantalla;
     - en otro sistema no hace nada.

2. **`IPowerGuard`**: evita que el sistema se duerma o frene el programa
   mientras se proyecta.
   - **En Windows:** lo que ya hacen `Platform.Windows/PowerThrottling.cs` y
     `App.xaml.cs`, sin cambiar cómo se comporta.
   - **En Mac:** `caffeinate` y la actividad que evita App Nap, tomados del
     prototipo (`KeepAwake.cs` y `MacWindowNative.cs`).

3. **`IDisplayProvider` de Avalonia**, en `Desktop/Platform/`.
   - Da la lista de pantallas con su **escala**. Agregale la escala a
     `DisplayInfo` sin romper lo que usa WPF.
   - Avisa cuando se enchufa o se desenchufa una pantalla.
   - Si desaparece la pantalla de la salida:
     - la salida se cierra de forma prolija y avisa;
     - cuando la pantalla vuelve, se puede proyectar otra vez.

     Fijate cómo lo hace hoy WPF y hacelo igual.

4. **`OutputWindow` y `StageWindow` en Avalonia**, en `Desktop/Views/`.
   - Sin borde y del tamaño de su pantalla.
   - Adentro van la `ProjectedView` y las cajas de escenario.
   - La diapositiva se dibuja siempre a 1920×1080 virtuales y después se
     escala.
   - Con una sola pantalla, la salida tapa al operador y Esc vuelve, igual
     que en Windows.
   - Llevan sus propios `IProjectionWindowService` e `IStageWindowService`
     de Avalonia, que copian lo que hacen `App/Services/ProjectionWindowService.cs`
     y `StageWindowService.cs`.

5. **El programa**: `EcclesiaCast.Desktop` pasa a ser un ejecutable
   (`WinExe`).
   - **Armado del programa** (`Desktop/App.axaml.cs`):
     - arma el `MainViewModel` de verdad, como lo hace `App/App.xaml.cs`, con
       los `PlatformServices` de Avalonia;
     - para los 40 diálogos que la fase 3 todavía no hizo, usa **un solo
       reemplazo** que muestra «Esto todavía no está en la versión de Mac».
   - **Dónde guarda los datos:**
     - en Mac, con un `IPlatformPaths` propio en `Platform.Mac`, en
       `~/Library/Application Support/EcclesiaCast`. En .NET esa carpeta es
       `LocalApplicationData`, lo comprobó la tanda 1;
     - en Windows, con `WindowsPlatformPaths`;
     - en los dos sistemas respeta `ECCLESIACAST_DATA_DIR`.
   - **Ventana de prueba del operador** (`Desktop/Views/TestOperatorWindow`):
     - lleva escrito que es provisoria y que la reemplaza la tanda 19;
     - lista de canciones con búsqueda;
     - libro, capítulo y versículo;
     - anterior y siguiente;
     - negro y logo;
     - elegir un video de fondo de la biblioteca;
     - elegir la pantalla de salida;
     - prender el control desde el celular, con la dirección y el QR.

     Todo usa comandos del `MainViewModel`, sin lógica propia. El aspecto no
     importa.
   - **Registro:** Serilog escribe en la carpeta de datos, como en Windows.

6. **El paquete para Mac en GitHub**: un flujo nuevo,
   `.github/workflows/desktop-mac.yml`, copiado de `proto-mac.yml` de la rama
   de la tanda 1.
   - Arranca con un push a ramas `prueba-mac/**` o a mano.
   - Arma `EcclesiaCast.app` sin firmar. Lleva LibVLC para chip Apple, las
     Biblias que vienen con el programa y las fuentes.
   - Lo deja como `.zip` adjunto de la corrida.
   - **No publica nada.**

7. **`docs/pruebas-mac-2.md`**: la hoja para la segunda visita a la Mac, con
   proyector. En español simple y en pasos cortos:
   - abrir con «Abrir igualmente»;
   - los seis puntos obligatorios de la sección 4, ahora con el programa de
     verdad;
   - una canción y un versículo con un tema de fábrica, para comparar con una
     foto de Windows;
   - una pantalla Retina con el proyector;
   - un culto de mentira de 20 minutos con video de fondo;
   - desenchufar el proyector.

   Al final, qué mandar: fotos, el registro y lo que dice «Acerca de esta
   Mac».

## Cómo se comprueba

- Build sin advertencias. Pruebas en verde en Windows y, en `test-mac`, las
  de `Desktop.Tests`.
- Pruebas sin ventana:
  - el armado del programa crea el `MainViewModel` completo;
  - el reemplazo de los diálogos contesta sin colgarse;
  - el `IPlatformPaths` de Mac da `~/Library/Application Support/EcclesiaCast`
    y respeta la variable;
  - si se quita la pantalla de la salida, la salida se cierra y avisa.
- **WPF:** después del punto 1, el banco de WPF tiene que seguir dando lo
  mismo. Lo que no se pueda probar en Windows sin abrir ventanas va en el
  informe, en una lista «para la próxima prueba en la iglesia».
- **No abras el programa nuevo en esta PC**, ni siquiera «para ver si
  arranca». Lo prueban GitHub, en Mac, y Alexis, en la Mac prestada.

## Terminado cuando

- GitHub arma el `.zip` para Mac.
- La hoja de la segunda visita está escrita.
- En Windows no cambió nada para quien usa el programa.
