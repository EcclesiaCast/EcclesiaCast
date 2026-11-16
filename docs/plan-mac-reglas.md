# Reglas para todas las tandas de la versión de Mac

Valen para cualquier modelo que reciba un `docs/plan-mac-N.md`. El plan
completo está en [plan-mac.md](plan-mac.md); **no hace falta leerlo entero**:
cada encargo dice qué sección mirar.

1. **No abrir el programa ni ninguna ventana** en esta computadora: Alexis la
   usa para trabajar. Se prueba con `dotnet build`, `dotnet test` y los bancos
   de render. Un banco es un proyecto `WinExe` (nunca de consola: abre una
   ventana negra) o una prueba con `Avalonia.Headless`, que dibuja en memoria.
2. **Build sin advertencias y pruebas en verde antes de cada commit.** Si
   algo ya venía roto, se dice en el informe; no se tapa.
3. **Editar con Edit**, nunca con `sed`, `perl` ni scripts que reescriban
   archivos.
4. **Commits sin línea `Co-Authored-By`.** Mensaje en inglés, en imperativo,
   como los que ya hay (`git log --oneline -5`). Un commit por punto del
   encargo.
5. **No subir versión, no crear tags, no hacer push.** Eso lo pide Alexis.
6. **Cada cambio que nota quien usa el programa** anota su línea en
   `CHANGELOG.md`, bajo «Sin publicar», en español simple. Lo que es sólo
   reordenar código va en una línea al final de esa sección.
7. **No tocar la biblioteca real** (`%APPDATA%\EcclesiaCast`). Para probar con
   datos, una carpeta temporal y la variable `ECCLESIACAST_DATA_DIR`.
8. **Buscar con Grep; no leer enteros los archivos grandes**
   (`MainWindow.xaml`, `MainWindow.xaml.cs`, `MainViewModel.*.cs`,
   `SlideView.xaml.cs`, `VlcVideoSurface.xaml.cs`, `ThemeManagerWindow.xaml`).
   Leer sólo el tramo que hace falta.
9. **Textos para la gente:** español rioplatense simple. Se dice «español»
   (nunca «castellano») y «funciona sin internet».
10. **No proponer ni agregar** acordes, organización del equipo ni letra
    animada.
11. **La versión de Windows no puede empeorar.** Si un punto del encargo
    obliga a cambiar cómo se comporta el programa en Windows, no se hace: se
    explica en el informe.
12. **Si lo que dice el encargo no coincide con el código, gana el código.**
    No se inventa: se anota en el informe qué se encontró.
13. **Trabajar sólo en la copia de trabajo propia** de la sesión, en la rama
    que indica el encargo. Sin `git stash` suelto (la pila es compartida con
    otras sesiones).

**Informe final**, en español: por cada punto, el commit, qué se hizo y cómo
se comprobó; lo que no se hizo y por qué; y lo que se encontró distinto de lo
esperado.
