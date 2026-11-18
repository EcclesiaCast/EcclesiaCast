# Tanda 13 (Opus) — Llegar a «igual» y revisión cruzada de la fase 2

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md). Contexto:
[plan-mac-fase-2.md](plan-mac-fase-2.md). Del plan general
[plan-mac.md](plan-mac.md), la sección 7.2 (criterios y plan B de la letra) y
la 16.

**Rama:** la de esta sesión, cortada de `mac` con las tandas 9 a 12
fusionadas. **Tamaño:** mediano.

## Para qué

El comparador de la tanda 12 mide qué tan lejos está la letra de Avalonia de
la de WPF. Esta tanda achica esa distancia. Además, igual que la tanda 8,
revisa con otros ojos toda la fase antes de que Alexis lleve el programa a
la Mac con proyector.

## Qué hay que hacer

1. **Correr el banco y el comparador** como dice `docs/pendientes.md`. Empezá
   por el `resumen.txt`.

2. **Cerrar las diferencias**, de la más común a la más rara. Primero hay que
   entender cada una. Estas son causas típicas:
   - medir el renglón con el espacio del final o sin él;
   - el redondeo de `pixelsPerDip`;
   - el interlineado que cada motor usa por defecto;
   - cómo corta cada motor una palabra larga sin espacios;
   - el ancho de un signo de apertura.

   Cada arreglo va en su propio commit, con su prueba, y del lado de Avalonia
   (`AvaloniaTextMetrics` y la `SlideView` de Avalonia). La cuenta común sólo
   se toca si el banco de WPF sigue dando idéntico.

3. **Si los renglones no llegan a ser idénticos** con arreglos razonables, no
   insistas más de lo que vale la pena. El plan B es medir con el motor de
   Avalonia en las dos interfaces, pero eso cambia Windows y **lo decide
   Alexis**. En el informe escribí:
   - cuántos casos no llegan y cuáles son;
   - un ejemplo de cada clase de diferencia, con las dos imágenes;
   - cuánto costaría el plan B.

4. **Contorno, sombra y franjas:** compará las imágenes lado a lado. Si el
   contorno de Avalonia no se parece, probá el plan B de la sección 13 del
   plan: dibujar el contorno como trazo, sólo en Avalonia.

5. **Revisión cruzada de la fase**, sobre `git diff 1ba42d1..HEAD`. Revisá:
   - las reglas del video: los delegados guardados en campos, un solo
     reproductor y nada que espere al hilo de la pantalla;
   - que `Desktop` no use nada de WPF ni de Windows sin pasar por una
     interfaz;
   - que `Platform.Mac` no haga nada en otro sistema;
   - que en Windows (`App`) **nada cambie**:
     - el banco de WPF tiene que dar idéntico;
     - las ventanas de salida y de escenario tienen que hacer las mismas
       llamadas nativas, en el mismo orden;
   - lo que las tandas 9 a 12 dejaron «dudoso» en sus informes (te lo pasa
     Fable).

6. **`docs/pruebas-mac-2.md`:** revisala con lo que aprendiste y agregá lo que
   falte.

## Terminado cuando

- El build no tiene advertencias y las pruebas pasan en Windows y en Mac
  (GitHub).
- El banco de WPF da las 195 medidas idénticas a la referencia.
- **El informe incluye el `resumen.txt` final**, con estas dos comparaciones:
  - WPF contra Avalonia en Windows, 3 temas: renglones idénticos en todos,
    o la lista de los que no lo son;
  - Avalonia en Windows contra Avalonia en Mac, tema `selawik`: idéntico.
- El informe ordena todo por gravedad (rompe / cambia algo / prolijidad) y
  dice qué se arregló y qué tiene que decidir Alexis.

**Puerta 3:** la cierra Alexis en la segunda visita. La letra tiene que verse
igual y la salida tiene que funcionar en la Mac real con proyector.
