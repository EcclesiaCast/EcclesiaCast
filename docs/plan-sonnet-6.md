# Plan de trabajo para Sonnet — proyectar una página web

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2) y lo que agrega
[plan-sonnet-2.md](plan-sonnet-2.md) al principio:
- No abrir la aplicación. Para lo visual, dibujá a PNG desde un programa de
  consola STA en tu carpeta temporal.
- No escribir nunca en la biblioteca real
  (`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`).
- No ejecutar python: no está instalado, y llamarlo abre la Microsoft Store.
- Rama `beta6`, sin push. Un commit por punto, en inglés, con
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Castellano rioplatense para todo lo que se ve.
- CHANGELOG [Sin publicar] y `docs/pendientes.md` al día.

**Excepción sólo para este plan:** para probar WebView2 podés abrir **tu propia
ventana de prueba** (no la app) desde tu programa de consola. Tiene que estar
fuera de la pantalla (`Left = -4000`), con `ShowActivated = false` y
`ShowInTaskbar = false`, y cerrarse sola al terminar. Alexis está usando la
PC: nada puede aparecer en su pantalla ni robarle el foco.

## Qué pidió Alexis

> abrir una web en el cuadrado central de diapositivas y hacer las cosas que
> quieramos ahí y se ve proyectado, que podamos verlo en tiempo real los
> movimientos o mostrar como screenshot, hago algo y apreto un boton y muestra
> eso

Hoy el programa **no proyecta páginas web**. WebView2 se usa para YouTube
(`YouTubeSurface`, `YouTubeBrowserWindow`) y para la búsqueda de canciones
(`MusicaComBrowserSearch`).

## La pieza clave: `WebView2CompositionControl`

El paquete que ya usamos (`Microsoft.Web.WebView2` 1.0.4078.44) trae
`Microsoft.Web.WebView2.Wpf.WebView2CompositionControl`:
- Dibuja la página dentro de un `Image` de WPF; por dentro usa un
  `GraphicsCaptureSession`.
- No tiene el problema de «airspace» del `WebView2` común: se le puede poner
  WPF encima, transformarlo y meterlo en un `Viewbox`.
- Su imagen se actualiza sola en tiempo real, así que la misma imagen se puede
  mostrar en la ventana de salida y en el LIVE.

Limitaciones según su documentación: puede ir a menos cuadros por segundo que
el control común, y el contenido con DRM (Netflix, Spotify) no se ve. Las dos
están bien para esto.

**Primero comprobá** (con tu ventana de prueba) que el control carga una
página, que su `Image` interna se actualiza y que esa imagen se puede mostrar
en otro `Image` o con un `VisualBrush` en otra ventana del mismo hilo.
Medí cuántos cuadros por segundo da. Si algo de esto no anda, pará y
explicalo en el informe antes de seguir por otro camino.

## 1. El panel web en el cuadro central

- Un botón **«🌐 Web»** en la barra de medios, junto a «▶ YouTube», abre el
  panel web **en el lugar de la grilla de diapositivas** (`SlidesPanel` en
  `MainWindow.xaml`).
- Arriba del panel, la barra:
  - ◀ ▶ ⟳ y la caja de dirección.
  - **A− / A+**: zoom de la página, para que el texto se lea en la pantalla
    grande.
  - **«📷 Proyectar foto»**.
  - **«🔴 En vivo»** (prendido o apagado).
  - **«Guardar en medios»**.
  - **«✕ Cerrar»**: vuelve a las diapositivas.
- **Lo que se ve es lo que se proyecta.**
  - La página se dibuja siempre a **1920×1080** (el control con ese tamaño
    fijo) y un `Viewbox` la achica para que entre en el panel, con barras a
    los costados si sobra lugar.
  - Así el operador ve exactamente lo que va a ver la congregación, y la
    proyección sale nítida.
  - Comprobá que el mouse sigue apuntando bien con la escala (clics, ruedita,
    seleccionar texto).
- **La caja de dirección:**
  - `google.com` o `www.x.org` → se completa con `https://`.
  - Texto que no es una dirección (`juan 3 16 comentario`) → búsqueda en
    Google.
  - Esa lógica va en **Core** con pruebas, por ejemplo
    `Core/Web/AddressInput.Resolve(string) → Uri`.
- **Perfil del navegador:** usá el mismo de YouTube
  (`WebViewProfile.GetEnvironmentAsync()`), así la sesión de Google o Canva
  iniciada en un lado sirve en el otro.
- **Ventanas nuevas** (`NewWindowRequested`): se abren en el mismo panel.
- **Descargas:** se dejan como hace WebView2.
- **Atajos de teclado:** mientras el foco está **dentro de la página web**,
  ningún atajo de la ventana principal se dispara. El control manda las
  teclas por los eventos de WPF, así que hoy escribir en un buscador movería
  las diapositivas con las flechas o la barra espaciadora. Revisá todos los
  `KeyDown`/`PreviewKeyDown` y los `InputBindings` de la ventana. Si no se
  puede comprobar ejecutando, que quede demostrado leyendo el código.
- El control se crea **la primera vez** que se abre el panel, no al arrancar
  el programa, y se mantiene vivo después.

## 2. Modo foto

- «📷 Proyectar foto» toma lo que se ve en ese momento con
  `CoreWebView2.CapturePreviewAsync` en PNG. Va a salir a la resolución con
  que se dibuja la página: con el control a 1920×1080, más o menos esa.
- La foto se guarda en `%APPDATA%\EcclesiaCast\capturas\web-AAAAMMDD-HHMMSS.png`.
- Se proyecta como una imagen más, con todo lo que ya existe (salida, LIVE,
  PREVIEW, transiciones), en **«Ajustar»** y **en primer plano**: tapa la
  letra, igual que un medio en primer plano hoy.
  - Es un `MediaItem` **que no está en la biblioteca**.
  - Revisá todos los lugares que suponen que el fondo tiene un `Id` de la
    base (resaltar la tarjeta en vivo, `RefreshBackground`, el inspector,
    «Agregar a la playlist»…) y que un medio de afuera no los rompa.
- **«Guardar en medios»** agrega la última foto a la pestaña de medios
  elegida, como una imagen común.
- Las capturas de más de 7 días se borran solas al abrir el programa, salvo
  las que se guardaron en medios.

## 3. Modo en vivo

- «🔴 En vivo» proyecta la página **en movimiento**: la pantalla muestra la
  imagen del control mientras se va actualizando. Se ve todo: desplazarse,
  videos de la página y animaciones.
- **El puntero del mouse:** cuando está sobre la página, se dibuja una flecha
  en la proyección en esa posición, así la congregación ve qué se señala.
  La barra tiene una casilla **«Mostrar el puntero»**, prendida de entrada,
  que se recuerda en los ajustes.
- Se proyecta **en primer plano** y en «Ajustar», como la foto.
- El LIVE del operador muestra lo mismo. El PREVIEW muestra la página cuando
  es lo que sigue.
- **Para modelarlo**, sugiero:
  - un `MediaType.Web` nuevo, con `Path` = la dirección;
  - un servicio estático en App, por ejemplo `WebLiveFeed`, con el
    `ImageSource` vivo y un evento cuando cambia;
  - `ProjectedView` y la ventana de salida, cuando el fondo es `Web`, muestran
    esa imagen.
  - Revisá **todos** los `switch`/`if` sobre `MediaType` (hay unos 15 sobre
    `YouTube`) para que `Web` no arranque VLC ni YouTube, no pida miniatura de
    video y no caiga en el fondo negro.
  - Fijate cómo se guarda `MediaType` en la base: si hace falta, sumá una
    migración y probala sobre una **copia** de la biblioteca real (copiá sólo
    el `.db`).
- **Cuándo termina:** se apaga solo cuando se proyecta otra cosa de fondo, y
  el botón se despinta.
  - Clear, Black y Logo funcionan como con cualquier fondo.
  - Si el operador cierra el panel o vuelve a las diapositivas mientras está
    en vivo, la proyección sigue en vivo. El control queda vivo, oculto pero
    sin destruirse.
  - Comprobá que oculto se siga actualizando. Si no se puede, que la pantalla
    se quede con el último cuadro y el botón «🌐 Web» lo avise («en vivo
    pausado: abrí la página para seguir»).
- **Modo PC de bajo rendimiento** (`PerformanceMode`): en el LIVE y el
  PREVIEW del operador se muestra una foto que se renueva cada 2 segundos, no
  el movimiento. La salida siempre va en movimiento.

## 4. Páginas guardadas en los medios

- **«Guardar en medios»** sin una foto tomada guarda **la página** (no una
  foto): una tarjeta 🌐 con el título de la página, su dirección y una
  miniatura sacada de una captura.
- **Clic en una tarjeta 🌐:** abre esa página en el panel web, sin
  proyectarla. Se proyecta con los botones de la barra.
- Las tarjetas 🌐 se pueden agregar a la playlist. Al elegirlas en la
  playlist se abre el panel con esa página, y el doble clic la pone
  **en vivo**.
- En el menú de clic derecho de una tarjeta 🌐, sólo lo que tenga sentido:
  renombrar, mover de pestaña, agregar a la playlist, quitar. Nada de
  propiedades de encuadre ni «aliviar».

## 5. Documentación

- **`docs/privacidad.md`:** una fila nueva, «Abrís una página web en el
  programa | La página que abrís (y Google, si escribís algo que no es una
  dirección) | Lo mismo que si la abrieras en un navegador».
- **Guía de uso (`docs/guia-de-uso.md`):** una sección «Proyectar una página
  web» con la foto, el en vivo, el puntero y las páginas guardadas.
- **CHANGELOG** [Sin publicar].
- **`docs/pendientes.md`:**
  - En §1 «Para probar», paso a paso (ver abajo).
  - El pedido de §3 («Proyectar una página web») se marca hecho y se mueve.
- **Instalador:** el mensaje sobre WebView2 en `installer/EcclesiaCast.iss`
  sumá «y las páginas web».

## Pruebas

- **En Core:** `AddressInput.Resolve`, el nombre de las capturas y la regla de
  limpieza a los 7 días.
- **En tu ventana de prueba fuera de la pantalla:**
  - que la foto salga a 1920×1080 aproximadamente;
  - que la imagen en vivo cambie al desplazar la página (desplazala por
    código);
  - medir los cuadros por segundo;
  - que el control oculto se siga actualizando.
- **Dibujos a PNG** de `ProjectedView` con un fondo `Web` (con una imagen de
  prueba en el `WebLiveFeed`) y con la foto proyectada, para ver que se ven en
  «Ajustar» y que la flecha del puntero cae donde corresponde.
- Compilar sin advertencias y que pasen todas las pruebas (hoy 537).

## Al terminar

- Rearmá el instalador de la beta.7 como en `.github/workflows/release.yml`:
  borrá `publish`; corré `dotnet publish
  src/EcclesiaCast.App/EcclesiaCast.App.csproj --configuration Release
  --runtime win-x64 --self-contained true -p:PublishSingleFile=false
  -p:Version=1.0.0-beta.7 --output publish`; y después
  `"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.7
  installer\EcclesiaCast.iss`.
- Informe final en castellano, para Alexis:
  - qué quedó hecho (con commits);
  - cómo lo probaste;
  - qué no pudiste probar;
  - qué decidiste vos;
  - cuántos cuadros por segundo dio el en vivo.

### Para «Para probar» (§1 de pendientes)

1. «🌐 Web» → escribí `google.com` y buscá algo. Probá escribir en el
   buscador de la página: las flechas y la barra espaciadora **no** tienen que
   mover las diapositivas.
2. «📷 Proyectar foto»: la pantalla muestra esa foto. Seguí navegando: la
   pantalla no cambia hasta la próxima foto.
3. «🔴 En vivo»: la pantalla sigue lo que hacés (desplazarte, un video de la
   página) y muestra el puntero. Volvé a las diapositivas: tiene que seguir
   en vivo. Proyectá otro fondo: el en vivo se apaga.
4. A− / A+ agrandan la letra de la página en la pantalla.
5. «Guardar en medios» → aparece una tarjeta 🌐; clic la abre; agregala a la
   playlist y probala desde ahí.
