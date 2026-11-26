# Tanda 15 (Sonnet) — Diálogos simples

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (todas). Cómo se traduce
cada pantalla: [guia-wpf-a-avalonia.md](guia-wpf-a-avalonia.md) (**leela
entera antes de empezar**). Contexto: [plan-mac-fase-3.md](plan-mac-fase-3.md).

**Rama:** la de esta sesión, cortada de `mac` (con la tanda 14).
**Tamaño:** mediano. **Una sola sesión.** No arranques la tanda 16.

## Para qué

Que en la Mac funcionen los diálogos chicos que hoy responden «Esto todavía no
está en la versión de Mac» (`NotOnMacYet` en `Desktop/Composition.cs`), con el
mismo aspecto que en Windows.

## Qué hay que hacer

1. **Abrir y guardar archivos y elegir carpeta (`IDialogs`).** Hoy es
   `NotOnMacYet`. Hacelo con `StorageProvider` de Avalonia, esperando la
   respuesta como `Views/Modal.cs` (un `DispatcherFrame` que termina cuando
   llega el resultado), porque los ViewModels leen la respuesta en la línea
   siguiente. Los filtros de tipo de archivo y la carpeta de inicio, como en
   la versión de WPF (`App/Services`). Sin ventana para mirar: probá lo que se
   pueda sin abrir nada (por ejemplo, cómo se arman los filtros) y anotá en el
   informe lo que queda para la Mac.

2. **Mudar primero la lógica que vive en la ventana de WPF.** Estas cuatro
   deciden cosas en su `.xaml.cs`. Antes de traducirlas, esa lógica pasa a
   un ViewModel o servicio de `EcclesiaCast.Presentation`, con pruebas en
   `Presentation.Tests`, y la ventana de WPF lo usa **sin cambiar lo que hace**:
   - `FeedbackWindow` y `FeedbackCoordinator`: armar y mandar el comentario,
     cuándo se habilita «Enviar» (ahora **el texto es obligatorio**), cuándo
     se pregunta;
   - `CountdownWindow`: validar y arrancar la cuenta (`Start_Click`);
   - `LightCopyWindow` y `YouTubeDownloadWindow`: el trabajo con su avance y
     su cancelación (`RunAsync`).
   Un commit por ventana.

3. **Traducir las pantallas**, cada una con su servicio en `Desktop/Services`,
   su línea en `Composition.cs` en vez de `NotOnMacYet` y su prueba en el
   banco de pantallas (sección 5 de la guía, de los dos lados):
   - elegir (`ChoiceWindow`) y pedir un texto (`TextPromptWindow`, con su
     segundo campo): sirven a `ITextPrompt`. El ejemplo 3.3 de la guía es esta
     misma ventana;
   - aviso (`NoticeWindow`): la de Avalonia ya existe y es provisoria. Tiene
     que verse como la de WPF, con el botón rojo cuando la pregunta es
     destructiva y **el ícono de «va encima de todo»** donde el texto trae
     `Notice.OnTopIconMark` (hoy se vería escrito; WPF lo dibuja así en
     `NoticeWindow.xaml.cs`);
   - pantalla de carga (`SplashWindow`): también existe y es provisoria;
   - acerca de (`AboutWindow`) y transiciones (`TransitionsWindow`): en WPF
     las abre el código de la ventana principal. Hacé la ventana y una forma
     de abrirla; la conecta la tanda 19 (`MenuRequest.About` y `.Transitions`);
   - comentarios, texto rápido (`IQuickTextEditor`), cuenta regresiva
     (`ICountdownDialog`), copia liviana (`ILightCopyDialog`) y bajar de
     YouTube (`IYouTubeDownloadDialog`).
   Un commit por ventana o por par chico (elegir y texto juntas).

4. **Actualización (`UpdateWindow`): no se hace en esta tanda.** En la Mac las
   actualizaciones no se ofrecen todavía (`IUpdates` es `Silent`): cambiar el
   `.app` es de la fase 4. Anotalo en el informe.

## Terminado

- Compila sin advertencias. Pasan todas las pruebas: núcleo, parte común y
  Mac, más las nuevas.
- El banco de render de WPF da idéntico a la referencia y el de Avalonia
  sigue en 195 de 195.
- Cada pantalla tiene su imagen de WPF y de Avalonia lado a lado, y la
  miraste. Lo que queda distinto va en el informe, con el porqué.
- Windows funciona igual: la lógica que se mudó no cambia lo que hace (sus
  pruebas lo muestran) y ninguna pantalla de WPF cambia de aspecto (las
  imágenes de WPF del banco de pantallas antes y después).
- Ninguna pantalla nueva decide nada que no esté en su ViewModel o servicio.
- La compilación en una Mac queda sin comprobar hasta que se suba: decilo.
- `CHANGELOG.md`, «Sin publicar»: una línea por lo que se nota.

**Informe final**, como piden las reglas, más: qué de la guía no sirvió o
faltaba (para corregirla antes de la tanda 16).
