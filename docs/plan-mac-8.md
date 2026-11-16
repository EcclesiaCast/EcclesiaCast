# Tanda 8 (Opus) — Revisión cruzada de la fase 1

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). Contexto:
[plan-mac.md](plan-mac.md), sección 5.

**Rama:** la de esta sesión, cortada de `mac` con las tandas 4 a 7
fusionadas. **Tamaño:** chico. **No escribas funciones nuevas:** revisás y
arreglás lo que encuentres.

## Para qué

La fase 1 movió unas 15 mil líneas sin cambiar (se supone) nada. Antes de que
Alexis pruebe el instalador en la iglesia hace falta una segunda mirada,
distinta de la de quien lo escribió.

## Qué revisar

Trabajá sobre `git diff <commit anterior a la tanda 4>..HEAD`, por partes;
no leas archivos enteros.

1. **Hilos.** Cada `IUiDispatcher.Post` e `InvokeAsync` nuevo: ¿reemplaza a un
   `BeginInvoke` o a un `Invoke`? ¿Se perdió alguna prioridad
   (`DispatcherPriority`)? ¿Hay algo que ahora corre fuera del hilo de la
   pantalla y toca una propiedad enlazada?
2. **Relojes.** Cada `IUiTimer`: mismo intervalo, mismo arranque (¿arrancaba
   solo?), mismo hilo. Los seis de `MainViewModel` (audio, tarjetas, escucha,
   tiempos, transporte, cuenta regresiva).
3. **Diálogos.** Los que antes se abrían con `Owner = MainWindow`: ¿siguen
   teniendo dueño? ¿Siguen siendo modales?
4. **Rutas.** Todo lo que pasó a `IPlatformPaths`: ¿da la misma carpeta que
   antes en Windows, letra por letra? ¿Se respeta `ECCLESIACAST_DATA_DIR`?
   Escribí una prueba que lo compruebe si no la hay.
5. **Orden de arranque** en `App.xaml.cs`: comparalo con el de antes.
6. **Vida de los objetos.** Singletons que eran singletons; nada que antes
   vivía toda la sesión y ahora se crea dos veces. Los delegados de LibVLC.
7. **La regla de la estructura.** `Presentation` sin WPF (lo dice el
   compilador) y además sin `OperatingSystem.IsWindows()` suelto: lo que
   depende del sistema va por una interfaz.
8. **La tanda 7**, ventana por ventana: momento del guardado, cancelar,
   avisos. Es donde más probable es una diferencia.
9. **Lo que las otras tandas dejaron «dudoso»** en sus informes (están en los
   mensajes de los commits de fusión o te los pasa Fable): resolvé cada uno.

## Qué entregar

- Arreglos, un commit por hallazgo, cada uno con su prueba si es lógica.
- `docs/pruebas-fase-1.md`: la lista de lo que Alexis prueba en la iglesia
  con el instalador de prueba. En español simple, por zona del programa,
  empezando por lo que más se movió. Al final, la lista de la tanda 7.
- Informe: hallazgos por gravedad (rompe / cambia algo / prolijidad), qué se
  arregló y qué queda a criterio de Fable.

## Terminado cuando

Build sin advertencias, pruebas en verde en Windows, `dotnet publish` de
`App` bien, y la hoja de prueba escrita.
