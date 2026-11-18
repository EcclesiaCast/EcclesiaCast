# Fase 2: la salida en Avalonia (tandas 9 a 13)

Lo escribió Fable el 18/11/2026, con la fase 1 cerrada en `mac` (1ba42d1).

- Plan general: [plan-mac.md](plan-mac.md), secciones 6 (fase 2), 7.2, 11 y 13.
- Reglas de siempre: [plan-mac-reglas.md](plan-mac-reglas.md).

## Cuándo se lanza

Estas tandas ponen mucho trabajo en Avalonia, así que **se lanzan cuando
pase la puerta 1**: el prototipo tiene que andar en la Mac real
([pruebas-mac-1.md](https://github.com/EcclesiaCast/EcclesiaCast/blob/proto/tanda-1/docs/pruebas-mac-1.md)).
Alexis puede decidir arrancar antes, como hizo con la fase 1. El riesgo es
que, si el prototipo falla en la Mac, todo lo de esta fase se tira.

La puerta 2 (el instalador de prueba en la iglesia) no frena la fase 2,
porque nada de lo que hacen estas tandas se ve en Windows. La tanda 9 toca la
cuenta de la letra de WPF, pero el banco de la tanda 6 la protege.

## Orden

Las tandas van de a una. Cada una sale de `mac` y vuelve a `mac`.

| Orden | Tanda | Qué hace | Modelo | Tamaño |
|---|---|---|---|---|
| 1 | [9](plan-mac-9.md) | Pasa la cuenta de la letra a la parte común; crea el proyecto `Desktop`; hace `SlideView` y `StageBoxView` en Avalonia; agrega las fuentes que vienen con el programa | Opus | Grande |
| 2 | [12](plan-mac-12.md) | Banco de render de Avalonia y comparador con el de WPF | Sonnet | Mediano |
| 3 | [10](plan-mac-10.md) | Video, `ProjectedView`, fundidos, logo en video y relleno | Opus | Mediano a grande |
| 4 | [11](plan-mac-11.md) | Ventanas de salida y de escenario, `Platform.Mac` y el programa de prueba para la Mac | Opus | Mediano |
| 5 | [13](plan-mac-13.md) | Ajustes hasta llegar a la igualdad, y revisión cruzada | Opus | Mediano |

La 12 va segunda aunque tenga un número más alto. Así la letra se compara
apenas existe la `SlideView` nueva, y la 10 y la 11 trabajan sabiendo cuánto
se parece.

## Decisiones que valen para toda la fase

- **Versión de Avalonia:** la 12.1.6, la misma del prototipo. Las pruebas
  que dibujan en memoria usan `Avalonia.Headless.XUnit` de la misma versión.
- **Proyecto nuevo:** `src/EcclesiaCast.Desktop` (net10.0).
  - Usa `Presentation`, `Core` y `Data`, y **no conoce WPF**.
  - `App` no lo referencia. Las dos interfaces viven una al lado de la otra.
- **La regla de la fase 1 sigue:** nada que decida algo va en una pantalla.
  Si una pantalla de Avalonia necesita una cuenta que hoy está en una de WPF,
  la cuenta se muda a `Presentation` y las dos la usan.
- **Windows no cambia.** Todo lo que toca a `App` se comprueba con el banco
  de WPF de la tanda 6. Las 195 medidas tienen que salir **idénticas** a las
  de `tests/casos-de-render/referencia-wpf/`.
- **Para lo propio de Mac, la referencia es el prototipo.** Está en la rama
  `claude/mac-tanda-1-prototipo-ddaace`, carpeta `proto/`:
  - `MacWindowNative.cs`: nivel de ventana, Spaces y App Nap.
  - `KeepAwake.cs`: `caffeinate`.
  - `VideoBackground.cs`: el video en una imagen, con sus reglas.
  - `Assets/Fonts`: Selawik y su licencia.

  Se copia lo que sirve; no se inventa de nuevo.
- **Lo que no existe en Mac** (panel web, captura de ventanas, YouTube en
  vivo) no se dibuja en la salida de Avalonia. Esas capas quedan apagadas,
  según lo que diga `IPlatformInfo`. No se simulan.
