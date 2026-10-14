# Plan de trabajo para Sonnet — costuras entre los tramos del 13/10

Valen las reglas de la sección 0 de [plan-sonnet-8.md](plan-sonnet-8.md)
(no abrir la app ni ventanas, no tocar la biblioteca real, Edit y nunca
sed/perl, un commit por punto con `Co-Authored-By: Claude Sonnet 5.5
<noreply@anthropic.com>`, build sin advertencias y pruebas en verde antes de
cada commit). Rama `beta6`, sin worktree, sin push.

Opus revisó cómo se cruzan los tramos A–G ya juntados y encontró esto.
Arreglalos en orden. Las líneas son aproximadas. Si al leer el código un
hallazgo resulta no ser real, no lo toques y explicalo en el informe. Lo que
sea lógica pura, con prueba en Core. En el CHANGELOG no hace falta entrada
para lo que corrige algo todavía no publicado (todo esto es de `[Sin
publicar]`); sí para el punto 15 si cambia el arranque.

1. **(Alta) La grilla de presentación guarda copias viejas de los medios.**
   `LoadMedia` reemplaza `_allMedia` con objetos nuevos, pero
   `RefreshOpenPresentation` no reconstruye si la firma
   (`PresentationPages.Signature`: Id, Name, Path, ThumbnailPath) no cambió, y
   `BuildPageCards` guardó `Page = pages[i]`. Resultado: se proyectan ajustes
   viejos (zoom, encuadre, volumen, silencio, categoría) y, peor, «Propiedades…»
   en una tarjeta-página después de renombrar la pestaña hace
   `_media.Update(paginaVieja)` y **reescribe la fila con la categoría vieja**.
   Arreglo: en `RefreshOpenPresentation`, aunque la firma sea igual, reasignar
   `Page` desde `_allMedia` por Id; en `GoLivePage`, `ShowPage` y el menú de la
   tarjeta-página (`InspectMedia`), resolver siempre
   `_allMedia.FirstOrDefault(m => m.Id == page.Id) ?? page`.
2. **Reabrir una presentación con una página al aire**
   (`OpenPresentation` con `!alreadyOpen`): `LiveSlideIndex = -1` y PREVIEW en
   la página 1, así que → proyecta la página 1. Arreglo: después de
   `BuildPageCards`, si hay una página de esa presentación al aire, fijar
   `LiveSlideIndex`/`IsLive` en su tarjeta, `RefreshSlideOnAir` y
   `PreviewPageMedia` en la siguiente.
3. **Página agregada a la playlist como medio suelto**: `ProjectMedia` pisa
   `LiveTitle` (se pierden el borde rojo y la marca del celular) y
   `CurrentPlaylistItemIsMedia` hace que → salte al ítem siguiente en vez de
   la página siguiente. Arreglo: en `ProjectMedia`, no tocar `LiveTitle` si el
   medio es de una pestaña de presentación; `CurrentPlaylistItemIsMedia` falso
   cuando la presentación abierta contiene ese medio.
4. **Doble clic (o flecha) a una entrada de presentación sin páginas** proyecta
   la primera diapositiva de lo que había en la grilla. Arreglo:
   `OpenPlaylistItemCore` devuelve bool y quien lo llama (doble clic,
   `TryAdvancePlaylist`) no proyecta si falló. Sirve también para la canción
   borrada, que tenía el mismo defecto.
5. **Silenciar una pestaña continua corta la continuidad**: `RefreshBackground`
   recibe `Playable(original).Clone()` sin la transformación de pestaña
   continua (Loop → Stop) que hace `ApplyBackgroundCore`, y al terminar el video
   `OnProjectedVideoEnded` ve Loop y no pasa al siguiente. Arreglo: extraer esa
   transformación a un `Resolved(item)` usado por `ApplyBackgroundCore` y por
   los dos `RefreshBackground` (el del silencio y el de `InspectMedia`).
6. **`VlcVideoSurface.Show` con el mismo Id** actualiza `player.Mute` pero no
   `_playing`, así que tras quitar el silencio un cambio de dispositivo de audio
   no reabre el sonido (`OnAudioOutputChanged` lee `_playing.Muted`). Arreglo:
   `_playing = media` en esa rama.
7. **Cambiar la versión bíblica o los temas vuelve a poner en pantalla una
   página que ya no estaba al aire** (`RebuildSlidesPreservingLive` →
   `GoLiveSlide` → `GoLivePage`). Arreglo: volver a proyectar sólo si
   `IsGridOnAir`.
8. **Falla al capturar otra ventana**: `WindowCapture.Start` llama a `Stop()`
   antes de validar la nueva; si falla, la salida queda en negro pero
   `IsWindowLive` y el título siguen diciendo que está en vivo. Arreglo: no
   parar la vigente hasta tener la nueva, o en la rama de error
   `ClearBackground()` si `IsWindowLive`.
9. **Pasar páginas rápido muestra un destello de una página anterior**
   (`ProjectedView.HoldPreviousImage`: si ya hay una retenida, deja la vieja A y
   descarta B). Arreglo: reemplazar la retenida por la que hoy domina en
   pantalla (B).
10. **Una entrada «Presentación» marca como presentación una pestaña
    equivocada** (deduce la pestaña de la categoría actual de la primera página
    y llama a `SetPresentationTab(tab, true)`). Arreglo: usar `item.Caption` si
    la categoría actual no es presentación, y no marcar pestañas solas.
11. **«Usar como logo» acepta un video convertido de GIF**, que se borra con su
    medio (`GifImporter.Forget`). Arreglo: excluir `GifImporter.IsConverted`
    en el comando y en el menú.
12. **Soltar un PDF desde el Explorador congela el Explorador** mientras está
    abierto el diálogo de importación (el `Drop` es sincrónico). Arreglo:
    `Dispatcher.BeginInvoke(() => vm.ImportMediaFiles(files))` en el handler.
13. **Después de una página, F4 y F2 encienden «Canciones/Biblia EN VIVO» con
    la pantalla vacía**. Arreglo: en `GoLivePage`, `_liveSlideOrigin = SlideOrigin.None`
    y `_textLiveTitle = null`.
14. **Volver a la letra repone una ventana capturada ya detenida**
    (`RemovePageForText` hace `SetBackground` directo). Arreglo: no restaurar
    ítems Window ni Web (o pasar por `ApplyBackgroundCore`).
15. **(Posible) La importación de Biblias al arrancar puede trabar la
    interfaz**: `Task.Run(BundledBibleImporter.Run)` inserta ~31.000
    versículos por versión en una transacción, con SQLite en journal DELETE,
    mientras el hilo de UI lee y escribe settings; además escribe `_settings`
    desde otro hilo. Medilo primero (con una base de prueba en una carpeta
    temporal, nunca la real): si el lock dura más de lo tolerable, la opción
    preferida es hacer la importación antes de mostrar la ventana, detrás de la
    pantalla de carga, o pasar las escrituras de settings al hilo de UI.
    **No** cambies el journal mode de la base sin medirlo y explicarlo: el
    respaldo depende de que la base sea un solo archivo (`DatabaseBackup` fuerza
    `journal_mode=DELETE`).
16. **Los temporizadores de retención de 2 s no siguen los fundidos
    configurables** (`VlcVideoSurface._holdTimer`, `ProjectedView._heldImageTimer`):
    con «Medios» en 3 s se suelta antes de tiempo, y el de imagen no se detiene
    al empezar otro fundido. Arreglo: `max(2 s, fundido + margen)` y detener
    `_heldImageTimer` al empezar un fundido de imagen.

Informe final en castellano: por punto, commit, qué hiciste y cómo lo
verificaste, o por qué no lo tocaste.
