# Tanda 2 (Sonnet) — Las pruebas de Core también en Mac

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). No hace
falta leer el plan completo.

**Rama:** la de esta sesión. **Tamaño:** chico. No depende de ninguna otra
tanda.

## Para qué

`EcclesiaCast.Core` y `EcclesiaCast.Data` ya son .NET común, pero nunca se
probaron fuera de Windows. Antes de construir encima hay que saber que sus
pruebas pasan en Mac, y que sigan pasando en cada cambio.

## Qué hay que hacer

1. **`.github/workflows/build.yml`:** agregar un trabajo `test-mac` en
   `macos-15` (chip Apple) que haga `dotnet test
   tests/EcclesiaCast.Core.Tests --configuration Release`. **No** tiene que
   compilar `src/EcclesiaCast.App` (es WPF, sólo Windows): por eso se apunta
   al proyecto de pruebas y no a la solución. El trabajo de Windows queda como
   está.

2. **Buscar lo que supone Windows** en `src/EcclesiaCast.Core`,
   `src/EcclesiaCast.Data` y `tests/EcclesiaCast.Core.Tests` (sin contar
   `Migrations`), con Grep:
   - rutas escritas con `\\` o `C:\`;
   - `Path.Combine` mezclado con separadores a mano;
   - comparaciones de rutas o nombres de archivo que distinguen mayúsculas
     donde no deberían;
   - `Environment.NewLine` o `\r\n` en textos que una prueba compara;
   - `Registry`, `DllImport`, `OperatingSystem.IsWindows`;
   - codificaciones de Windows (`Encoding.GetEncoding(1252)` y parecidas:
     en Mac hace falta registrar `CodePagesEncodingProvider`).
   Ya sé de estos: `Performance/LowPerformance.cs` (clave del registro),
   `Updates/CodeSignature.cs` (ya se saltea si no es Windows),
   `Songs/EasyWorshipImporter.cs`, `Songs/ProPresenterImporter.cs`,
   `Songs/Rtf.cs`, `Updates/ReleaseNotesText.cs`.

3. **Arreglar sólo lo que haga fallar una prueba en Mac o lo que
   evidentemente fallaría** (una ruta con `\\` que se usa para abrir un
   archivo). Cada arreglo, con su prueba. **No cambies nada que modifique el
   comportamiento en Windows.** Lo dudoso va al informe, no al código.

4. **Ayudante de rutas** `EcclesiaCast.Core/IO/PortablePath.cs`, con pruebas:
   - `Normalize(string)`: convierte `\` y `/` al separador del sistema;
   - `FileName(string)`: el nombre del archivo de una ruta escrita en
     cualquiera de los dos sistemas (`C:\Videos\a.mp4` y `/Users/x/a.mp4`);
   - `SamePath(string, string)`: compara sin distinguir mayúsculas ni
     separador.
   Todavía no lo usa nadie: es para la pantalla de «medios que no se
   encuentran» (plan, sección 11). No lo conectes.

5. **Una prueba nueva** que arme un paquete `.ecbiblias` en una carpeta
   temporal y lo vuelva a leer (si ya existe una así, alcanza con comprobar
   que no depende de rutas de Windows).

## Cómo se comprueba sin Mac

No se puede hacer push, así que el trabajo de Mac no va a correr todavía.
Comprobá: `dotnet test` en verde en Windows; el YAML revisado a mano; y la
lista de la búsqueda del punto 2 en el informe, cada hallazgo con «arreglado»,
«no hace falta» o «dudoso». Cuando Alexis haga push, lo que falle en Mac se
arregla en una sesión corta.

## Terminado cuando

Pruebas en verde en Windows, flujo escrito, ayudante con sus pruebas, y el
informe con la lista completa de hallazgos.
