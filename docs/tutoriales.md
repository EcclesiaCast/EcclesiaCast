# Tutoriales (Ayuda → «Tutoriales ▸»)

En el menú Ayuda, «Tutoriales» tiene una flecha ▸ y un submenú con un ítem por tutorial: elegir uno lo
arranca. Cada tutorial es un archivo de texto (UTF-8). Los 11 que vienen con el programa están en
`src/EcclesiaCast.Core/Tutorials/*.txt` (se incluyen solos al compilar). Para sumar uno propio, o
reemplazar uno que viene, dejá un `.txt` en la carpeta **Tutoriales** de los datos del programa
(`%APPDATA%\EcclesiaCast\Tutoriales`): si se llama igual que uno que viene, lo reemplaza. Un archivo
que no se entiende (le faltan pasos, un texto es muy largo, o pide abrir algo que no existe) se ignora
sin avisar: revisalo contra esta página.

```
# una línea con # es un comentario
titulo: Crear una canción nueva
resumen: Una línea para el ítem.
icono: 🎵
orden: 1

[paso]
señalar: SongsAddButton
abrir: agregar
titulo: El botón ➕ Agregar
texto: Lo que dice la tarjeta. Si sigue en otra línea,
se junta con la de arriba.

[paso]
abrir: ventana:editor
resaltar: LyricsBox
titulo: Escribí la letra
texto: Abre el editor de verdad y marca el cuadro de la letra.
```

- De **3 a 10 pasos** (los que vienen: de 6 a 9). Cada texto, de **230 letras como máximo**: se lee en
  pocos segundos.
- `señalar:` es el `x:Name` de un elemento de `MainWindow.xaml`. Si el paso nombra una parte de la
  pestaña Canciones o de Biblia (por ejemplo `SongsList`, `BibleQueryBox`), el programa abre esa pestaña
  solo. Si el elemento no se ve en ese momento (un panel achicado), el paso se salta.
- `abrir:` abre algo **antes** de mostrar la tarjeta, para que el paso muestre la pantalla de la que
  habla. Se cierra solo al pasar a un paso que abre otra cosa (o ninguna) y al terminar:
  - `menu:NombreDelMenu`: un menú de la barra de arriba abierto (`menu:MenuBroadcast`, `MenuBibles`,
    `MenuSongs`, `MenuBackgrounds`, `MenuProjection`). El foco ilumina el menú junto con su lista.
  - `agregar`: el menú del botón ➕ Agregar de Canciones.
  - `ventana:editor`, `ventana:buscar` (buscar canciones en internet), `ventana:importar` (traer de otro
    programa), `ventana:importar-holyrics` (la misma, con Holyrics elegido), `ventana:obs` (Salida para
    OBS o vMix) y `ventana:celular` (el QR): la ventana real, no modal, a la derecha; la tarjeta se
    queda del lado libre. Varios pasos seguidos con la misma ventana la dejan abierta.
  - `herramienta:TabPlates` (o cualquier otra pestaña de las herramientas de texto): elige esa pestaña.
  - `historial` (el 🕘 Historial de Biblia), `vista:lista` y `vista:grilla` (la vista de los libros).
  Al terminar el tutorial, las pestañas y la vista vuelven a como estaban.
- `resaltar:` es el `x:Name` de un elemento **de la ventana que abrió `abrir:`**: se le dibuja un
  marco que titila. Una prueba revisa que existan.
- `dibujo:` pone un dibujo en la tarjeta: `celular-pin` (la pantalla del PIN en el teléfono) o
  `celular-control` (el control). Están dibujados en `Controls/TutorialPicture.cs`.
- Un elemento nuevo para señalar se agrega poniéndole `x:Name` en el XAML. Una prueba revisa que todos
  los que usan los tutoriales que vienen existan, tanto en la ventana principal como en las otras.
- Escribir en español rioplatense; «español» (nunca «castellano») y «funciona sin internet».
- Las acciones de `abrir:` las ejecuta `MainWindow.Tour.cs`; los nombres válidos están en
  `Core/Ui/TourActions.cs`.
