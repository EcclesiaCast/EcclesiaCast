# EcclesiaCast

Software libre y nativo de Windows para proyección en iglesias: letras de canciones, Biblia multiversión, fondos de imagen y video, y formato de texto totalmente personalizable.

> ✅ **Versión 1.0.** Instalador para Windows en la
> [página de releases](https://github.com/EcclesiaCast/EcclesiaCast/releases).
> Está en uso en una iglesia real. Si algo falla,
> [contanos](https://github.com/EcclesiaCast/EcclesiaCast/issues).

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

📝 **[Pendientes](docs/pendientes.md)** — lo que falta, en un solo lugar.

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

Qué se conecta a internet y cuándo: [política de privacidad](docs/privacidad.md).

## Instalar y firma digital

Los instaladores de EcclesiaCast **todavía no están firmados digitalmente**
(*the installers are not code-signed yet*). Por eso, la primera vez que lo
abrís, Windows puede mostrar «Windows protegió su PC» o «Editor desconocido».
Si lo bajaste de la [página de versiones](https://github.com/EcclesiaCast/EcclesiaCast/releases)
de este repositorio, tocá **Más información → Ejecutar de todas formas**.

Para comprobar que el archivo es el que se publicó, GitHub muestra su huella
**SHA-256** junto a cada instalador, y el programa la verifica antes de
actualizarse solo. El instalador lo compila GitHub Actions a partir del código de
este repositorio (`.github/workflows/release.yml`).

- **Privacidad:** [política de privacidad](docs/privacidad.md).

## Contribuir

Se agradecen issues y pull requests: leé [CONTRIBUTING.md](CONTRIBUTING.md).

## Licencia

[GPL-3.0](LICENSE) — EcclesiaCast es y será siempre software libre.

> Nota sobre las Biblias: el programa importa el archivo que tengas, pero las traducciones tienen sus propios derechos de autor. El repositorio no distribuye traducciones con copyright: el instalador sólo trae Biblias de dominio público o con licencia libre (origen y licencia de cada una en `installer/bibles/LICENCIAS.md`).
