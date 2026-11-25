# Tutoriales (Ayuda → «Tutoriales…»)

Cada tutorial es un archivo de texto (UTF-8). Los 8 que vienen con el programa están en
`src/EcclesiaCast.Core/Tutorials/*.txt` (se incluyen solos al compilar). Para sumar uno propio, o
reemplazar uno que viene, dejá un `.txt` en la carpeta **Tutoriales** de los datos del programa
(`%APPDATA%\EcclesiaCast\Tutoriales`): si se llama igual que uno que viene, lo reemplaza. Un archivo
que no se entiende (le faltan pasos, o un texto es muy largo) se ignora sin avisar: revisalo contra esta página.

```
# una línea con # es un comentario
titulo: Crear una canción nueva
resumen: Una línea para la tarjeta.
icono: 🎵
orden: 1

[paso]
señalar: SongsAddButton
titulo: El botón ➕ Agregar
texto: Lo que dice la tarjeta. Si sigue en otra línea,
se junta con la de arriba.

[paso]
titulo: Un paso que no señala nada
texto: Sin «señalar», la tarjeta sale en el medio de la ventana.
```

- De **3 a 10 pasos** (los que vienen: de 5 a 8). Cada texto, de **230 letras como máximo**: se lee en
  pocos segundos.
- `señalar:` es el `x:Name` de un elemento de `MainWindow.xaml`. Si el paso nombra una pestaña de
  Canciones o de Biblia (por ejemplo `SongsList`, `BibleQueryBox`), el programa abre esa pestaña solo.
  Si el elemento no se ve en ese momento (un panel achicado), el paso se salta.
- Un elemento nuevo para señalar se agrega poniéndole `x:Name` en el XAML. Una prueba revisa que todos
  los que usan los tutoriales que vienen existan.
- Escribir en español rioplatense; «español» (nunca «castellano») y «funciona sin internet».
- Lo que está dentro de otras ventanas (por ejemplo el editor de canciones) no se puede señalar: el
  paso va sin `señalar:` y lo explica el texto.
