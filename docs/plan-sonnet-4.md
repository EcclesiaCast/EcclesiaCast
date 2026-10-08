# Plan de trabajo para Sonnet — medios: vista previa fiel, selección múltiple y calidad de los PDF

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2) y lo que agrega
[plan-sonnet-2.md](plan-sonnet-2.md) al principio: no abrir la aplicación ni
ninguna ventana visible (para lo visual, dibujar a PNG desde un programa de
consola STA en tu carpeta temporal), no escribir nunca en la biblioteca real
(`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`; para datos reales
copiá sólo `ecclesiacast.db` a una carpeta temporal), rama `beta6`, sin push,
un commit por punto en inglés con `Co-Authored-By: Claude Sonnet 5.5
<noreply@anthropic.com>`, castellano rioplatense para lo que se ve, CHANGELOG
[Sin publicar] y `docs/pendientes.md` al día. Mirá los tres puntos antes de
empezar: tocan los mismos archivos (la plantilla de la barra de medios en
`MainWindow.xaml`, `MediaInspectorWindow`, `MediaFraming`).

## 1. La miniatura muestra cómo se va a ver en pantalla

Hoy la miniatura de Propiedades y las tarjetas de la barra de medios muestran
la imagen o el póster tal cual, no como sale en pantalla. Alexis quiere ver el
encuadre real (escala, zoom, corrimiento) y poder mover lo que tiene zoom.

- Un control nuevo (por ejemplo `FramedMediaPreview`) que dibuja un medio
  dentro de un cuadro 16:9 **con la misma regla que la salida**
  (`Services/MediaFraming.cs`: escala, zoom, corrimiento, tamaño fijo, color
  de relleno de los bordes). Para videos y YouTube usa el póster
  (`ThumbnailPath`); para imágenes, el archivo. También el brillo y el tinte
  (mirá cómo los dibuja `Controls/ProjectedView`: `BrightnessLayer`,
  `TintLayer`). Si el relleno es otro medio, se dibuja su imagen quieta. Sin
  desenfoque en las tarjetas (cuesta y no se nota); en Propiedades sí.
- **En Propiedades** (`Views/MediaInspectorWindow`): la miniatura de 120×68
  de arriba se reemplaza por ese cuadro, más grande (que ocupe bien la parte de
  arriba de la ventana, que ya tiene dos columnas), y **se actualiza en vivo**
  al cambiar cualquier campo, antes de guardar. **Arrastrar** sobre el cuadro
  mueve la imagen (los mismos valores que «Correr»); la **ruedita** cambia el
  zoom (el mismo valor que «Zoom»). Los campos numéricos se mueven junto.
- **En la barra de medios**: las tarjetas usan el mismo control, así que
  después de guardar muestran el encuadre (por ejemplo una página de PDF con
  «Ajustar» se ve con sus barras). Que no cueste: decodificá las imágenes
  chicas (`DecodePixelWidth` del tamaño de la tarjeta) y reusá lo que se pueda;
  una pestaña puede tener cientos de medios.

## 2. Selección múltiple en la barra de medios

Como en el Explorador de Windows:
- **Ctrl+clic** suma o saca un medio de la selección; **Shift+clic** elige el
  tramo desde el último elegido; **Ctrl+A** elige todos los de la pestaña;
  **Esc** limpia la selección. Arrastrar un rectángulo en el espacio vacío
  para elegir varios, si sale sin complicar.
- Con varios elegidos: **Supr** (o clic derecho → Quitar de la biblioteca)
  los quita todos, con **una sola** confirmación que dice cuántos; **arrastrar**
  uno de ellos a otra pestaña mueve **todos**; arrastrarlos a la playlist los
  agrega todos, en su orden; clic derecho → «Mover a la pestaña…» con la lista
  de pestañas.
- Un clic simple sin teclas sigue haciendo lo de siempre (proyectar) y deja
  elegido sólo ese. Los elegidos se ven claramente (borde y fondo de acento),
  distinto de «el que está en pantalla».

## 3. Calidad de las páginas de PDF y PowerPoint

Alexis comparó una página de un PDF en el navegador con la imagen importada,
las dos al 160 %, y la importada se ve pixelada. Causas encontradas:
- `DocumentImporter.ImportAsync` dibuja cada página a **1920** píxeles de
  ancho (`DestinationWidth`), justo el tamaño de la pantalla: no queda
  margen para el zoom del Encuadre.
- Para mostrar, todas las imágenes se leen con `DecodePixelWidth = 1920`
  (`ProjectedView`, `SlideView`, `OutputWindow`): con zoom, se agranda esa
  versión chica.

Arreglo:
- Dibujar las páginas al **doble**: 3840 píxeles del lado largo (4K),
  manteniendo la proporción de la página (una página vertical: 3840 de alto).
- Al mostrar una imagen, leerla al tamaño que realmente se necesita: el ancho
  de la pantalla multiplicado por el zoom del Encuadre (y por lo que pida la
  escala, por ejemplo «Llenar» con una imagen vertical), **sin pasar** del
  tamaño del archivo ni de 4096. Sin zoom tiene que seguir costando lo mismo
  que hoy.
- Las páginas que ya están importadas siguen como están (no se re-importa
  nada solo). Si es fácil, clic derecho sobre una página de un documento →
  «Volver a importar en alta calidad».
- Prueba: importá un PDF de prueba (hacé uno en tu carpeta temporal, por
  ejemplo con texto fino y una foto) y compará los dos tamaños de PNG.

## Al terminar

Rearmá el instalador de la beta.7 como en `.github/workflows/release.yml`
(borrá `publish`; `dotnet publish src/EcclesiaCast.App/EcclesiaCast.App.csproj
--configuration Release --runtime win-x64 --self-contained true
-p:PublishSingleFile=false -p:Version=1.0.0-beta.7 --output publish`, y
`"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.7
installer\EcclesiaCast.iss`). Sumá a `docs/pendientes.md`, sección 1 «Para
probar», cómo probarlo. Informe final en castellano, para Alexis: qué quedó
hecho (con commits), cómo lo probaste, qué no pudiste probar y qué decidiste
vos.
