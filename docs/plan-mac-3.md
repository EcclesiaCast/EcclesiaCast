# Tanda 3 (Sonnet) — Pasar de .NET 8 a .NET 10

Reglas: [plan-mac-reglas.md](plan-mac-reglas.md) (leelo, es corto). No hace
falta leer el plan completo.

**Rama:** la de esta sesión, cortada de `main` **después de publicada la
1.4.0**. **Tamaño:** mediano. **Riesgo:** medio, porque toca la versión de
Windows que usan las iglesias.

## Para qué

Microsoft dejó de dar soporte a .NET 8 el 10/11/2026. .NET 10 tiene soporte
hasta noviembre de 2028 y es la base del trabajo de Mac. Este cambio **no
agrega nada**: el programa tiene que hacer exactamente lo mismo que antes.

## Qué hay que hacer

1. **Proyectos:** `net8.0` → `net10.0` en `EcclesiaCast.Core`,
   `EcclesiaCast.Data` y `EcclesiaCast.Core.Tests`;
   `net8.0-windows10.0.19041.0` → `net10.0-windows10.0.19041.0` en
   `EcclesiaCast.App`. `SupportedOSPlatformVersion` no se toca.

2. **Paquetes atados a la versión:** `Microsoft.EntityFrameworkCore.*` y
   `Microsoft.Extensions.DependencyInjection` de `8.0.*` a `10.0.*`. El resto
   de los paquetes **no se actualiza** en esta tanda, salvo que no compile con
   .NET 10 (entonces, la versión mínima que funcione, y se anota).

3. **Entity Framework 10:**
   - **No se crea ninguna migración nueva.** Si `dotnet ef migrations
     has-pending-model-changes` dice que hay cambios, frená y anotalo en el
     informe: no generes nada.
   - Una prueba nueva: copiar a una carpeta temporal una base creada con las
     migraciones actuales, abrirla con el código nuevo, correr `Migrate()` y
     leer una canción y un versículo. Si hay en las pruebas una base de
     ejemplo, usala.
   - `DatabaseBackup` obliga `journal_mode=DELETE`: comprobá con una prueba que
     sigue siendo un solo archivo después de un respaldo.

4. **Flujos de GitHub** (`build.yml`, `release.yml`): `dotnet-version` a
   `10.0.x`. Nada más.

5. **Instalador** (`installer/EcclesiaCast.iss`): no debería cambiar (el
   programa lleva .NET adentro). Buscá menciones a «.NET 8» o «net8.0» en todo
   el repositorio (instalador, README, CONTRIBUTING, docs, sitio) y
   actualizalas.

6. **Advertencias nuevas.** .NET 10 trae analizadores nuevos. El build tiene
   que quedar sin advertencias **arreglando el código**, no apagándolas. Si
   una advertencia pide un cambio que altera el comportamiento, no lo hagas:
   anotala y apagala sólo en esa línea, con un comentario que diga por qué.

7. **Cambios de comportamiento de WPF entre 8 y 10.** Buscá en la
   documentación de Microsoft la lista de «breaking changes» de WPF y de .NET
   en las versiones 9 y 10, y revisá con Grep si alguno toca algo que el
   programa usa. Van al informe, uno por uno, con «nos toca» o «no nos toca».

8. **CHANGELOG:** una línea en «Sin publicar»: «El programa pasa a .NET 10,
   la versión con soporte de Microsoft hasta 2028. No cambia nada a la vista.»

## Terminado cuando

- `dotnet build --configuration Release` sin advertencias y `dotnet test` en
  verde.
- `dotnet publish src/EcclesiaCast.App -c Release -r win-x64
  --self-contained` termina bien (no ejecutes lo publicado).
- El informe trae la lista del punto 7 y el tamaño de la carpeta publicada
  antes y después.

## Qué prueba Alexis después

Un instalador de prueba, como cualquier versión: instalar encima de la 1.4.0,
abrir su biblioteca, proyectar canción, Biblia y video, YouTube, el celular.
Tiene que ser igual.
