# Biblias para bajar desde el programa

En el programa, **Biblia → 📥 Biblias → «Bajar Biblias de internet…»** lee una
lista (`catalogo.json`) y muestra las Biblias por idioma. Cada iglesia marca
las que quiere y baja **sólo esas**. El programa comprueba cada archivo con su
huella SHA-256 antes de agregarlo, así que un archivo cambiado o roto se
rechaza.

La lista y las Biblias están en **Cloudflare R2**, en un «bucket» llamado
`ecclesiacast-biblias`. La dirección de la lista está en
`AppInfo.BibleCatalogUrl`. Mientras esa dirección esté vacía, el programa
avisa que todavía no hay Biblias para bajar.

## Preparar los archivos

El generador de esta carpeta (`generador/`) hace tres cosas:

```
dotnet run --project generador -- usfx <eBible _usfx.zip> "<nombre>" <SIGLA> <idioma> <salida.ecbiblias>
dotnet run --project generador -- separar <carpeta> <paquete.ecbiblias>...
dotnet run --project generador -- lista <carpeta> "SIGLA=licencia" ...
```

- `usfx` convierte una Biblia de eBible.org: toma sólo los 66 libros y deja
  afuera notas, referencias y títulos.
- `separar` hace un archivo por versión a partir de paquetes `.ecbiblias`.
- `lista` escribe `catalogo.json` con todos los `.ecbiblias` de la carpeta
  (nombre, sigla, idioma, tamaño, huella y licencia).

Las que están hoy (8/11/2026), todas libres:

| Sigla | Versión | Idioma | Licencia | Origen |
|---|---|---|---|---|
| RV1909 | Reina-Valera 1909 | Español | Dominio público | installer/bibles |
| BLM | Biblia libre para el mundo | Español | Dominio público (CC0) | installer/bibles |
| VBL | Versión Biblia Libre | Español | CC BY-SA 4.0 | installer/bibles |
| PDDPT | Palabra de Dios para ti | Español | CC BY 4.0 | installer/bibles |
| KJV | King James Version | Inglés | Dominio público | eBible.org `eng-kjv2006` |
| WEB | World English Bible | Inglés | Dominio público | eBible.org `engwebp` |
| BPM | Bíblia Portuguesa Mundial | Portugués | Dominio público (borrador en revisión) | eBible.org `porbrbsl` |
| LSG | Louis Segond 1910 | Francés | Dominio público | eBible.org `fraLSG` |

Las licencias de las cuatro en español están explicadas en
[../bibles/LICENCIAS.md](../bibles/LICENCIAS.md). **Una versión con derechos
entra a la lista sólo con el permiso por escrito de su editorial anotado en
`docs/pendientes.md`.**

## Subirlas a Cloudflare R2 (una sola vez)

1. Crear la cuenta en <https://dash.cloudflare.com> y activar la verificación en
   dos pasos.
2. En el menú, **R2 Object Storage** → activarlo (pide una tarjeta; lo gratis
   alcanza: 10 GB guardados y descargas sin costo).
3. **Create bucket** → nombre `ecclesiacast-biblias` → crear.
4. En el bucket, **Settings → Public access → R2.dev subdomain → Allow**. Copiar
   la dirección que da (algo como `https://pub-xxxxxxxx.r2.dev`).
5. En el bucket, **Objects → Upload**: arrastrar todos los archivos de la carpeta
   «Biblias para subir a R2» (los `.ecbiblias` y `catalogo.json`).
6. Probar en el navegador `https://pub-xxxxxxxx.r2.dev/catalogo.json`: tiene que
   verse la lista.
7. Poner esa dirección (con `/catalogo.json` al final) en
   `AppInfo.BibleCatalogUrl` y sacar una versión del programa.

## Agregar o cambiar una Biblia después

Poner el `.ecbiblias` nuevo en la carpeta, volver a correr `lista` y subir a R2
el archivo nuevo y el `catalogo.json` nuevo. No hace falta sacar otra versión
del programa: lo lee la próxima vez que alguien abre «Bajar Biblias…».

Para probar una lista antes de subirla: servir la carpeta en
`http://localhost:<puerto>/` y abrir el programa con
`ECCLESIACAST_BIBLE_CATALOG=http://localhost:<puerto>/catalogo.json`.
