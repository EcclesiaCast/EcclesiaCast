# Tanda 6 (Sonnet) — Banco de render de WPF y casos de referencia

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). Contexto:
[plan-mac.md](plan-mac.md), sección 7.2 solamente.

**Rama:** la de esta sesión, cortada de `mac` con la tanda 4 fusionada. Puede
ir antes o después de la tanda 5, **nunca al mismo tiempo**. **Tamaño:**
mediano.

## Para qué

En la fase 2 la letra proyectada se va a dibujar con Avalonia. Tiene que
verse **igual** que hoy con WPF: mismos cortes de renglón, mismo tamaño. Para
comparar hace falta una foto exacta de cómo dibuja hoy WPF un juego fijo de
diapositivas. Esta tanda saca esa foto.

## Qué hay que hacer

1. **Casos** en `tests/casos-de-render/`, un archivo JSON por caso, con el
   contenido de la diapositiva y el tema completo. Mirá con Grep cómo se arma
   una diapositiva (`SlideContent`, `SlideTheme` en `Core`) y qué propiedades
   lee `Controls/SlideView.xaml.cs` (son propiedades de control: buscá
   `DependencyProperty.Register`). Unos **60 casos**:
   - canción: 1, 2, 4 y 6 renglones; renglones cortos, largos y uno larguísimo
     sin espacios; todo mayúsculas; acentos, eñes, signos de apertura (¿ ¡);
   - Biblia: versículo corto, versículo muy largo, dos versiones a la vez,
     con y sin la cita;
   - ajuste al ancho prendido y apagado; interlineado chico y grande;
   - contorno solo, sombra sola, los dos, ninguno;
   - franjas detrás del texto (los estilos de `SlideView.Bands.cs`);
   - alineación izquierda, centro, derecha; márgenes distintos;
   - anuncio en el tercio inferior, cuenta regresiva, texto rápido, logo de
     texto;
   - una vista de escenario (`StageBoxView`).
   Cada uno con **tres temas**: el de fábrica de canciones, el de fábrica de
   Biblia (los crea `ThemeSeeder` en `Data`) y uno con una fuente angosta y
   pesada (Impact).

   Los JSON tienen que poder leerse sin WPF: el lector va en
   `Presentation` (`Rendering/RenderCase.cs`), porque el banco de Avalonia va
   a leer los mismos.

2. **Proyecto `tests/EcclesiaCast.RenderBench.Wpf`**
   (`net10.0-windows10.0.19041.0`, `UseWPF`, **`OutputType` `WinExe`**: nunca
   de consola). Referencia `App`. Sin ventana a la vista: crea los controles
   en memoria, les da medida y posición (`Measure`, `Arrange`,
   `UpdateLayout`) a 1920×1080 y los dibuja con `RenderTargetBitmap` a 96
   puntos por pulgada. Necesita un hilo STA y los recursos de `App.xaml`
   cargados a mano (sin `Application.Run`).

   Para cada caso escribe en `artifacts/render/wpf/` (carpeta ignorada por
   git):
   - `caso.png`;
   - `caso.json` con: tamaño de letra final de cada texto, **el texto de cada
     renglón tal como quedó cortado**, el rectángulo del bloque de texto
     (x, y, ancho, alto) y el nombre de la fuente que de verdad se usó.

   Los renglones: después del `UpdateLayout`, recorrer el texto con
   `TextPointer` y `GetCharacterRect`, y agrupar por la coordenada vertical.
   Si hay una forma más directa, usala.

   Sale con código 0 si dibujó todo y distinto de 0 si no. Como no tiene
   consola, escribe su resultado en `artifacts/render/wpf/resultado.txt`.

3. **Referencia guardada.** Copiá los `caso.json` (no los PNG: pesan
   demasiado) a `tests/casos-de-render/referencia-wpf/` y commitealos. Son la
   medida de «cómo era» contra la que se va a comparar.

4. **Prueba que la protege:** en `tests/EcclesiaCast.Presentation.Tests` (o
   en `Core.Tests` si la tanda 5 no llegó), una prueba que lea todos los casos
   y todas las referencias y compruebe que cada caso tiene su referencia y que
   los archivos están bien formados. No dibuja nada.

5. **`.gitignore`:** `artifacts/`.

6. **`docs/pendientes.md`:** una línea en la parte de Mac: cómo se corre el
   banco y dónde quedan las imágenes.

## Cómo se comprueba

- Build sin advertencias, pruebas en verde.
- Correr el banco (es un `.exe` que no muestra nada) y leer `resultado.txt`.
- Mirar con Read **cinco** de los PNG (uno de canción, uno de Biblia con dos
  versiones, uno con franjas, uno con contorno y sombra, la cuenta regresiva)
  y decir en el informe si se ven como una diapositiva de verdad. Si el
  contorno o la sombra no salen en la imagen (pasa cuando los efectos no se
  dibujan fuera de pantalla), **decilo**: hay que saberlo antes de comparar.

## Terminado cuando

Los 60 casos por 3 temas se dibujan, las referencias están commiteadas, y el
informe dice cuánto tarda el banco y si los efectos salen bien en la imagen.

## Qué no hacer

No tocar `SlideView` ni nada de cómo se dibuja. Si para medir hace falta
exponer algo interno, que sea con `InternalsVisibleTo` hacia el banco.
