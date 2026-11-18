# Tanda 12 (Sonnet) — Banco de render de Avalonia y comparador

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md). Contexto:
[plan-mac-fase-2.md](plan-mac-fase-2.md) y la sección 7.2 de
[plan-mac.md](plan-mac.md).

**Rama:** la de esta sesión, cortada de `mac` con la **tanda 9** fusionada. Va
antes que la 10. **Tamaño:** mediano.

## Para qué

La tanda 6 sacó la «foto» de cómo dibuja WPF 65 diapositivas en 3 temas.
Esta tanda saca la misma foto con la `SlideView` de Avalonia y arma el
comparador, que dice caso por caso si se ve igual. Es la medida de la
puerta 3.

## Qué hay que hacer (un commit por punto)

1. **El banco**, como pruebas en `tests/EcclesiaCast.Desktop.Tests`, con
   `Avalonia.Headless` y Skia de verdad, sin ventana. Mirá cómo lo hizo la
   tanda 9 con sus cinco casos y cómo lo hace el banco de WPF
   (`tests/EcclesiaCast.RenderBench.Wpf/Program.cs`).
   - Lee los **mismos casos**, con `RenderCase.LoadAll`.
   - Por cada caso y tema escribe `caso.png` y `caso.json` en
     `artifacts/render/avalonia/<sistema>/`, donde `<sistema>` es `windows`
     o `mac`. El JSON usa `RenderResult`, **el mismo formato** que WPF.
   - Para los renglones, recorré las líneas de `TextLayout` del bloque ya
     ubicado. La fuente que se anota es la que Avalonia usó de verdad, no la
     que se pidió.
   - Agregá un **cuarto tema, sólo en este banco:** `selawik`, que es el de
     canciones con `FontFamily = "Selawik"`. Sirve para comparar Windows con
     Mac usando una fuente que viene con el programa. No lleva referencia de
     WPF.
   - La prueba del banco falla sólo si un caso **no se pudo dibujar**. Las
     diferencias no la hacen fallar: de eso se ocupa el comparador.

2. **El comparador**, en `tests/EcclesiaCast.RenderCompare`: una biblioteca
   más una prueba que lo corre, sin consola.
   - Recibe dos carpetas y escribe `informe.html` y `resumen.txt`.
   - Usa los criterios de la sección 7.2:
     - renglones: **idénticos**, en cantidad y en el texto de cada uno;
     - tamaño de letra: hasta 2 % de diferencia;
     - rectángulo del bloque: hasta 19 px de diferencia.
   - El HTML muestra lado a lado las imágenes de los casos que no pasan, con
     la diferencia en palabras (por ejemplo, «corta en "amó" en vez de en
     "Dios"»). Las imágenes se enlazan, no se copian.
   - **WPF contra Avalonia en Windows:** se comparan los 3 temas contra
     `tests/casos-de-render/referencia-wpf/`.
     - Las imágenes de WPF se toman de `artifacts/render/wpf/` si existen.
     - Si no existen, el informe dice «correr el banco de WPF para ver la
       imagen».
   - **Avalonia en Windows contra Avalonia en Mac:** sólo el tema `selawik`.
     Ahí se exige que sea **idéntico** también en tamaño y rectángulo.

3. **GitHub**, en `.github/workflows/build.yml`:
   - En el trabajo de Windows y en `test-mac` se corre `Desktop.Tests`, que
     incluye el banco.
   - Se sube `artifacts/render/avalonia/` como adjunto de la corrida
     (`actions/upload-artifact`), para bajarlo y comparar.
   - No se agrega nada que publique.

4. **`docs/pendientes.md`:** en la parte de Mac, anotá cómo se corren el
   banco y el comparador, y dónde queda el informe.

## Cómo se comprueba

- Build sin advertencias y pruebas en verde.
- Correr el banco en Windows (`dotnet test tests/EcclesiaCast.Desktop.Tests`)
  y el comparador contra la referencia de WPF.
- **Pegar el `resumen.txt` en el informe final.** Tiene que decir:
  - cuántos casos pasan en renglones, en tamaño y en rectángulo;
  - cuáles no pasan, agrupados por motivo.
- **No arregles las diferencias:** eso le toca a la tanda 13. La única
  excepción es una diferencia que sea claramente un error del banco, por
  ejemplo que mida mal los renglones: ésa sí se arregla.
- Mirá con Read tres pares de imágenes de casos que no pasan y describí la
  diferencia.

## Terminado cuando

- Los 65 casos se dibujan en Avalonia con los 4 temas.
- El comparador escribe el informe.
- GitHub corre el banco en Windows y en Mac.
- El informe final dice qué tan lejos estamos de «igual».

## Qué no hacer

No tocar `SlideView` (ni la de WPF ni la de Avalonia) ni la cuenta de la
letra. Esta tanda mide; no corrige.
