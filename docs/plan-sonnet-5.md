# Plan de trabajo para Sonnet — zoom y corrimiento que muestran toda la imagen

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2) y lo que agrega
[plan-sonnet-2.md](plan-sonnet-2.md) al principio: no abrir la aplicación ni
ninguna ventana visible (para lo visual, dibujar a PNG desde un programa de
consola STA en tu carpeta temporal), no escribir nunca en la biblioteca real
(`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`), rama `beta6`, sin
push, un commit por punto en inglés con `Co-Authored-By: Claude Sonnet 5.5
<noreply@anthropic.com>`, castellano rioplatense para lo que se ve, CHANGELOG
[Sin publicar] y `docs/pendientes.md` al día.

## El problema

Alexis puso una página vertical (la tapa de un manual) en «Rellenar» y le hizo
zoom: no puede mostrar la parte de la página que quiere. Al alejar el zoom, el
recorte se achica con negro alrededor en vez de mostrar más de la página; al
correrla, nunca llega a la parte de abajo.

**Causa** (`Services/MediaFraming.Apply`): para «Rellenar» pone
`Stretch.UniformToFill` en un `Image` del tamaño del contenedor. WPF recorta lo
que sobra al tamaño de ese `Image` (recorte de diseño) y **después** aplica el
`RenderTransform` de zoom y corrimiento al pedazo ya recortado. Lo que quedó
afuera no se puede volver a mostrar. Pasa en la salida (`ProjectedView`,
`VlcVideoSurface`) y en la vista previa nueva (`FramedMediaPreview`), porque
todos usan esa regla.

Además, las miniaturas de las imágenes (`MediaThumbnails.Create`, 320×180) se
guardan **ya recortadas** a 16:9: las tarjetas no pueden mostrar bien una imagen
vertical aunque se arregle el encuadre.

## 1. Encuadre sin recorte previo

El zoom y el corrimiento tienen que trabajar sobre **la imagen entera**, y el
único recorte es el borde de la pantalla (o del cuadro de vista previa).
Una forma simple: dibujar siempre la imagen entera dentro del contenedor
(`Stretch.Uniform`, que nunca recorta) y hacer «Rellenar» con una escala extra
en el `RenderTransform`: el factor entre «cubrir» y «caber»
(`max(cw/w, ch/h) / min(cw/w, ch/h)`, con el tamaño de la imagen en píxeles),
multiplicado por el zoom, más el corrimiento. El contenedor recorta lo que
sale de la pantalla (`ClipToBounds` donde haga falta). Si encontrás una forma
mejor, usala, pero que el resultado sea ese.

- Hace falta el tamaño de la imagen: está en el `Source` (`BitmapSource`
  `PixelWidth/PixelHeight`). En los videos el primer cuadro llega después:
  volvé a aplicar el encuadre cuando cambia el `Source` (mirá `OnFormat` en
  `VlcVideoSurface`).
- Que «Rellenar» sin zoom ni corrimiento se vea **exactamente igual** que
  hoy, y que «Ajustar» y «Estirar» no cambien. Prueba en Core o con dibujos a
  PNG de los tres modos con una imagen vertical, una horizontal y una 16:9,
  con zoom 50 %, 100 % y 200 %, y corrimientos a los cuatro lados.
- Con «Rellenar» y zoom alejado, se ve más de la imagen (con el color de
  relleno en lo que no cubre); corriendo una página vertical se llega de
  arriba a abajo.
- Revisá todos los lugares que llaman a `MediaFraming.Apply` (la salida, el
  LIVE y el PREVIEW, el video, la vista previa de las tarjetas y la de
  Propiedades) y también la lectura de imágenes según el zoom
  (`ImageDecodeSize`), que dependía del encuadre viejo.

## 2. Miniaturas que respetan la forma

Las miniaturas de **imágenes** (y de páginas de documentos) se guardan con su
proporción (por ejemplo, 320 del lado largo), sin recortar a 16:9, para que la
tarjeta pueda encuadrarlas igual que la pantalla. Las de video siguen como
están. Las existentes se rehacen solas una vez al abrir (como ya se hace con
las que faltan; con una marca en los ajustes para que sea una sola vez), en
segundo plano.

## 3. Propiedades: ver la imagen entera al encuadrar

En el cuadro de vista previa de Propiedades (`FramedMediaPreview` con
`UseOriginal`), mostrá también **lo que queda fuera de la pantalla**: la
imagen entera, con la parte de afuera oscurecida (como un recortador de
fotos) y el rectángulo de la pantalla bien marcado. Así se ve qué parte de la
página se va a mostrar y hacia dónde correrla. El cuadro puede crecer un poco
para tener lugar alrededor del rectángulo. Arrastrar y la ruedita siguen
funcionando igual. En las tarjetas de la barra de medios no: ahí sólo lo que se
ve en pantalla.

## Al terminar

Rearmá el instalador de la beta.7 como en `.github/workflows/release.yml`
(borrá `publish`; `dotnet publish src/EcclesiaCast.App/EcclesiaCast.App.csproj
--configuration Release --runtime win-x64 --self-contained true
-p:PublishSingleFile=false -p:Version=1.0.0-beta.7 --output publish`, y
`"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.7
installer\EcclesiaCast.iss`). Sumá a `docs/pendientes.md`, sección 1 «Para
probar», cómo probarlo (una página vertical en «Rellenar», alejar el zoom y
correrla hasta abajo). Informe final en castellano, para Alexis: qué quedó
hecho (con commits), cómo lo probaste, qué no pudiste probar y qué decidiste
vos.
