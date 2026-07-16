# EcclesiaCast

Software libre y nativo de Windows para proyección en iglesias: letras de canciones, Biblia multiversión, fondos de imagen y video, y formato de texto totalmente personalizable.

> 🚧 **En desarrollo activo.** El proyecto está en el sprint 0 de 8. Todavía no hay una versión usable para servicios; la primera release instalable llegará al final del plan de desarrollo.

## ¿Por qué?

Muchas iglesias necesitan dos programas a la vez (uno para canciones, otro para la Biblia) o pagan licencias costosas. EcclesiaCast fusiona ambas funciones en una sola aplicación gratuita y de código abierto.

## Funcionalidades planificadas (v1.0)

- 🎵 **Canciones** — biblioteca con título, artista y letra por secciones (verso, coro, puente), búsqueda instantánea.
- 📖 **Biblia** — múltiples versiones importables por JSON o XML (Zefania, OSIS), búsqueda por referencia (`Juan 3:16`) y por texto.
- 🖥️ **Proyección** — salida a pantalla completa en un segundo monitor, con fondos de imagen o video en loop y transiciones.
- 🎨 **Temas** — tipografía, tamaño, color, alineación, sombra y márgenes editables y reutilizables.
- 📋 **Playlist del servicio** — el orden del culto completo, operable solo con el teclado.

## Stack

C# / .NET 8 · WPF (MVVM) · SQLite + EF Core · LibVLCSharp

## Compilar y ejecutar

Requisitos: [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) en Windows 10/11.

```
dotnet build
dotnet run --project src/EcclesiaCast.App
```

Tests:

```
dotnet test
```

## Estructura

| Proyecto | Responsabilidad |
|---|---|
| `EcclesiaCast.Core` | Entidades y lógica de dominio. Sin dependencias de UI. |
| `EcclesiaCast.Data` | Persistencia SQLite e importadores de Biblias. |
| `EcclesiaCast.App` | Aplicación WPF: ventana del operador y ventana de salida. |

## Licencia

[GPL-3.0](LICENSE) — EcclesiaCast es y será siempre software libre.
