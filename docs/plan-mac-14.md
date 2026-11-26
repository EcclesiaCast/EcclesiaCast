# Tanda 14 (Opus) — Estilos generales, menú y guía WPF → Avalonia

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md). Contexto: del plan
[plan-mac.md](plan-mac.md), la sección 6 (fase 3), y
[plan-mac-fase-3.md](plan-mac-fase-3.md).

**Rama:** la de esta sesión, cortada de `mac` (con el 94664ac, la 1.5.0).
**Tamaño:** mediano a grande. Es la base de las tandas 15 a 21 (Sonnet).

## Para qué

Las tandas 15 a 21 traducen unas 45 pantallas. Si cada una inventa sus
colores y sus botones, el programa de Mac queda desparejo y cada traducción
cuesta el doble. Esta tanda deja hecho lo que todas comparten: los estilos,
el menú, una forma de comparar cada pantalla con WPF y una guía con ejemplos.

## Qué hay que hacer

1. **Estilos generales.** Lo de `App/App.xaml` pasa a
   `Desktop/Styles/EcclesiaCastTheme.axaml`: la paleta oscura, el logo, el
   cuadriculado, los botones (`DarkButton`, `AccentButton`, `FoldButton`,
   `VersionChip`, `VersionRow`, el ▸/▾ de las versiones, `TabButton`), los
   títulos y recuadros de panel, el texto de relleno, las agarraderas, y los
   menús, listas desplegables, avisos al pasar el mouse y barras de
   desplazamiento oscuras. La letra de la interfaz es Segoe UI, o Selawik
   donde no está (en Mac), como resuelve `AvaloniaFontCatalog`. También
   `ShortcutHint` y el diseño de aprendizaje o avanzado (recursos que la
   ventana cambia). La aplicación y la prueba en memoria cargan el mismo
   tema.
2. **Convertidores** que usan las pantallas de WPF y no tienen un equivalente
   directo en Avalonia, con sus pruebas: color escrito como texto a pincel, el
   número de un botón de opción, el primero que no es nulo, «no es cero» y la
   marca de las pestañas de medios.
3. **Menú** de la ventana principal (`Desktop/Controls/MainMenu.axaml`), con
   los mismos menús, textos, atajos y ayudas que `MainWindow.xaml`. Lo que es
   un comando del `MainViewModel` va enlazado. Lo que en WPF resuelve la
   ventana (Ajustes, Salir, Pixabay, el menú Ver, tutoriales, Colaborar…) se
   avisa con un evento `Requested`, para que la ventana de verdad (tanda 19 en
   adelante) lo conecte. El menú global de la Mac es de la tanda 22.
4. **Banco de pantallas.** Un banco nuevo de WPF
   (`tests/EcclesiaCast.ScreenBench.Wpf`, un `WinExe`, sin ventanas) dibuja en
   memoria las pantallas de WPF con los estilos reales. El menú se lee del
   propio `MainWindow.xaml`. Del lado de Avalonia, una prueba dibuja lo mismo.
   Las dos imágenes quedan en `artifacts/pantallas/` y además una al lado de la
   otra, para mirarlas. Por ahora: la barra del menú, cada menú abierto y un
   muestrario de los estilos. El banco de render de la letra no se toca.
5. **Guía** (`docs/guia-wpf-a-avalonia.md`) para quien traduce: cómo se
   escribe cada cosa de WPF en Avalonia (disparadores, visibilidad,
   convertidores, recursos, ventanas que esperan respuesta, enlaces, atajos,
   ayudas, menús), con ejemplos sacados de esta tanda y de las ventanas que
   ya existen. Cómo sumar una pantalla al banco y comparar. Y lo que no se
   hace (lógica en la pantalla, WPF o Windows en `Desktop`).

## Terminado

- Compila sin advertencias. Pasan todas las pruebas: núcleo, parte común y
  Mac, más las nuevas.
- El banco de render de WPF da igual que antes y el de Avalonia sigue en
  195 de 195.
- Cada imagen nueva está comparada a ojo con la de WPF, y las diferencias que
  quedan están anotadas en la guía.
- Windows no cambia: `App` no se toca.
