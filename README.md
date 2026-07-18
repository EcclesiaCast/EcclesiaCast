# EcclesiaCast

Software libre y nativo de Windows para proyección en iglesias: letras de canciones, Biblia multiversión, fondos de imagen y video, y formato de texto totalmente personalizable.

> 🚧 **En desarrollo activo.** Todas las funciones de la v1.0 están implementadas y en pruebas en una iglesia real. La primera release instalable está en preparación.

## ¿Por qué?

Muchas iglesias necesitan dos programas a la vez (uno para canciones, otro para la Biblia) o pagan licencias costosas. EcclesiaCast fusiona ambas funciones en una sola aplicación gratuita y de código abierto.

## Funcionalidades

- 🎵 **Canciones** — biblioteca con título, artista y letra por párrafos, búsqueda instantánea, e importación desde `.txt` y desde archivos **`.pro` de ProPresenter 7**.
- 📖 **Biblia** — múltiples versiones importables (JSON y Zefania XML), navegación libro → capítulo → versículo, búsqueda por referencia (`Juan 3:16`, `jn 3:16-18`, `sal 23`) y por texto, y **proyección de dos versiones a la vez**.
- 🖥️ **Proyección** — salida a pantalla completa en un segundo monitor, con *Clear*, *Black*, *Logo*, aviso al pie y resaltado de palabras en vivo.
- 🎞️ **Fondos** — imágenes y videos en bucle detrás del texto, organizados en pestañas, con comportamiento fondo/primer plano, escala y control de audio.
- 🎨 **Temas** — tipografía, tamaño con auto-ajuste, color, alineación, márgenes y fondo; tema por canción y diseño por diapositiva, con editor visual estilo ProPresenter.
- 📋 **Playlist del servicio** — canciones, pasajes y medios en orden, operable solo con el teclado de punta a punta.

📖 **[Guía de uso completa](docs/guia-de-uso.md)**

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

## Tus datos

Todo vive en `%APPDATA%\EcclesiaCast\ecclesiacast.db` — copiá ese archivo y tenés el backup completo.

## Contribuir

Se agradecen issues y pull requests: leé [CONTRIBUTING.md](CONTRIBUTING.md).

## Licencia

[GPL-3.0](LICENSE) — EcclesiaCast es y será siempre software libre.

> Nota sobre las Biblias: el programa importa el archivo que tengas, pero las traducciones tienen sus propios derechos de autor. El repositorio no distribuye traducciones con copyright.
