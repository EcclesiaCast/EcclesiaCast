# Contribuir a EcclesiaCast

¡Gracias por querer ayudar! EcclesiaCast es software libre (GPL-3.0) hecho para
que cualquier iglesia pueda proyectar sus servicios sin pagar licencias.

## Poner el proyecto en marcha

Necesitás **Windows** y el **SDK de .NET 10**:

```bash
git clone https://github.com/EcclesiaCast/EcclesiaCast.git
cd EcclesiaCast
dotnet build
dotnet test
dotnet run --project src/EcclesiaCast.App
```

No hace falta instalar VLC: las librerías de video vienen en un paquete NuGet.

## Cómo está organizado

| Proyecto | Qué contiene |
|---|---|
| `EcclesiaCast.Core` | Dominio puro: canciones, Biblia, temas, medios, playlists y el `PresentationService`. **Sin dependencias de UI**, y es donde viven los tests. |
| `EcclesiaCast.Data` | Persistencia con SQLite + EF Core, importadores y migraciones. |
| `EcclesiaCast.Presentation` | Lo que decide algo y no dibuja: servicios, el control remoto y las interfaces de plataforma (`Platform/`). .NET común, **sin WPF ni nada de Windows**: lo comparten la versión de Windows y la de Mac. |
| `EcclesiaCast.Platform.Windows` | Lo que le habla a Windows y no dibuja: PowerPoint, las páginas de un PDF, las teclas multimedia, los dispositivos de sonido. Sin WPF ni Windows Forms. |
| `EcclesiaCast.App` | La interfaz WPF: ventana del operador, ventana de salida, editores, y la versión de WPF de las interfaces de plataforma (`Platform/`). |

Cada sistema llena las interfaces de plataforma al arrancar (`App.xaml.cs`):
el hilo de la pantalla, los relojes, los avisos, los diálogos de archivos, las
carpetas, las imágenes, las fuentes. `Presentation` sólo conoce las interfaces.

**`Presentation` no conoce pantallas; si no compila sin WPF, no va ahí.** Lo
que usa imágenes o ventanas de WPF se queda en `App`; lo que le habla a Windows
sin dibujar va en `Platform.Windows`.

Dos reglas que sostienen el diseño:

1. **Todo lo que se proyecta pasa por `PresentationService`.** Es el único que
   sabe qué está en vivo; la ventana de salida y los previews solo escuchan su
   evento `Changed`. Así es imposible que el operador vea algo distinto de lo
   que ve la congregación.
2. **El dominio no conoce WPF.** Si algo se puede resolver en `Core`, va en
   `Core` y se cubre con tests.

## Estilo

- **Código y commits en inglés**; la interfaz, en español.
- Comentá el *por qué*, no el *qué*: el código ya dice qué hace.
- Antes de abrir un PR: `dotnet build` y `dotnet test` en verde.

## Buenas primeras contribuciones

- Importadores de otros formatos de canciones (OpenSong, OpenLyrics).
- Traducciones de la interfaz.
- Reportar bugs con pasos para reproducirlos — son muy valiosos.

## Reportar un problema

Abrí un [issue](https://github.com/EcclesiaCast/EcclesiaCast/issues) contando
qué esperabas, qué pasó y cómo reproducirlo. Si la app falló, adjuntá el log de
`%APPDATA%\EcclesiaCast\logs\`.
