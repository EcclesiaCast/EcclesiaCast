# Tanda 7 (Sonnet) — Sacar la lógica del código de las ventanas

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). No leas
el plan completo.

**Rama:** la de esta sesión, cortada de `mac` con la tanda 5 fusionada.
**Tamaño:** mediano a grande: 2 sesiones en fila.

## Para qué

Varias ventanas tienen en su código (`.xaml.cs`) cosas que **deciden o
guardan**: leen y escriben ajustes, llaman a los repositorios, validan,
importan archivos. Cuando se haga la misma ventana en Avalonia, esa lógica
habría que escribirla dos veces. Si antes se muda a un ViewModel o a un
servicio en `Presentation`, la ventana nueva sólo tiene que ubicar controles.

**El programa tiene que hacer exactamente lo mismo.** Esta tanda es la más
delicada de la fase 1, porque no se puede abrir la ventana para mirar.

## La regla para decidir qué se muda

**Se muda** (a `Presentation`): lo que lee o guarda en la base o en los
ajustes; lo que valida lo que escribió la persona; lo que arma o transforma
datos (una lista filtrada, un texto, un tema); lo que baja o importa
archivos; lo que decide qué se habilita.

**Se queda** en la ventana: foco, selección de texto, posición y tamaño de la
ventana, arrastrar con el mouse, animaciones, medir controles, abrir menús,
todo lo que nombre un control por su nombre para dibujar.

**Si dudás, no lo mudes** y anotalo en el informe.

## En este orden (un commit por ventana)

De la más simple a la más cargada. Entre paréntesis, las líneas de su código.

1. `TrashWindow` (104): listar, restaurar y borrar definitivo.
2. `PixabayWindow` (217): buscar, paginar, bajar.
3. `LogoManagerWindow`: alta, edición y borrado de logos.
4. `WebSongSearchWindow` (345): la búsqueda y la elección del resultado. Lo
   que maneja el navegador escondido se queda.
5. `MediaInspectorWindow` (405): leer y guardar las propiedades del medio.
6. `SongImportWindow` (477): encontrar bibliotecas, leer, elegir qué importar.
   Las carpetas donde busca (`SoftouchFolder`, las de ProPresenter) pasan a
   `IPlatformPaths`.
7. `BroadcastStyleWindow` (487): leer y guardar los estilos. La vista previa
   (WebView2) se queda.
8. `StageDesignerWindow` (626): las cajas, sus propiedades y el guardado. El
   arrastre con el mouse se queda.
9. `SongDesignerWindow` (1.056): lo mismo.
10. `MainWindow.xaml.cs` (1.616) y `MainWindow.Layout.cs` (468): **sólo** lo
    que guarda o lee ajustes y lo que decide qué proyectar. Buscá con Grep
    `_settings`, `Repository`, `vm.` seguido de una asignación. No lo leas
    entero. Lo dudoso, al informe.

Para cada una:

- Si ya tiene ViewModel, la lógica va ahí. Si no, creá uno
  (`AlgoViewModel` con `CommunityToolkit.Mvvm`, como `ThemeManagerViewModel`)
  y que la ventana lo reciba.
- **Cada cosa mudada lleva su prueba** en
  `tests/EcclesiaCast.Presentation.Tests`, con una base en carpeta temporal.
  Ésa es la red, ya que la ventana no se puede abrir.
- El código que queda en la ventana tiene que leerse como «ubicar y
  conectar».

## Terminado cuando

- Build sin advertencias, pruebas en verde.
- Ninguna de las diez ventanas llama a un repositorio ni a `ISettingsStore`
  directamente (comprobalo con Grep y pegá el resultado en el informe).
- El informe trae, por ventana: qué se mudó, qué se quedó y por qué, y **una
  lista de lo que Alexis tiene que probar a mano** en esa ventana (tres o
  cuatro acciones concretas cada una).

## Cuidado con

- **El momento en que se guarda.** Si hoy se guarda al cerrar la ventana, o al
  soltar el mouse, que siga así.
- **Lo que pasa al cancelar.** Muchas ventanas trabajan sobre una copia y
  recién la aplican con Aceptar.
- **Los avisos** (`Notice.Show`): que salgan en el mismo momento y con el
  mismo texto.
