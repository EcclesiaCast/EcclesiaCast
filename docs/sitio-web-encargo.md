# Encargo: página web de EcclesiaCast

Para empezar en un chat nuevo (leer también `README.md`, `docs/guia-de-uso.md`
y `docs/pendientes.md`, y la memoria del proyecto).

## Para qué

La firma digital gratuita (SignPath Foundation) nos rechazó el 15/10/2026 por
**falta de visibilidad pública**: 0 estrellas, 5 descargas, ninguna mención
afuera. Una página propia es el primer paso para cambiar eso, y de paso le da a
cualquier iglesia un lugar claro donde ver qué hace el programa y bajarlo.
No es un gasto: tiene que costar **0**.

## Quién la va a leer

Encargados de proyección y pastores de iglesias chicas de habla hispana
(Uruguay, Argentina y el resto de Latinoamérica). Gente que **no es técnica**,
que hoy usa PowerPoint o programas pagos, y que desconfía de bajar un
instalador sin firma. Todo en **castellano rioplatense** (el usuario de este
proyecto no maneja inglés), simple y sin jerga.

## Qué tiene que tener

1. **Qué es, en una frase, y un botón grande «Descargar para Windows»**
   (a la última versión de https://github.com/EcclesiaCast/EcclesiaCast/releases).
2. **Capturas y un video corto** (30 a 60 segundos) del programa en uso:
   canciones, Biblia con dos versiones a la vez, fondos con video, el control
   desde el celular, las salidas para OBS.
3. **Las funciones**, cada una con su captura (ver el README y la guía).
4. **«Cómo instalarlo»**, con la verdad sobre el aviso de Windows: el
   instalador **todavía no está firmado digitalmente**, por eso Windows muestra
   «Windows protegió su PC»; hay que tocar «Más información → Ejecutar de todas
   formas». Explicarlo con capturas y sin esconderlo: la confianza se gana
   diciendo la verdad. Incluir cómo comprobar la huella SHA-256 de GitHub.
5. **Por qué es gratis y de código abierto** (GPL-3.0) y cómo colaborar o contar
   una falla (los *issues* de GitHub).
6. **Preguntas frecuentes**: qué computadora hace falta, si funciona sin
   internet, qué Biblias trae y cómo agregar las propias, si se puede usar en
   transmisión.
7. Enlace a la política de privacidad (`docs/privacidad.md`).

## Cosas que NO se pueden decir

- Que el instalador está firmado o que lo avala SignPath.
- Que lo usan «muchas iglesias»: hoy es una iglesia. Decirlo tal cual (un
  proyecto nuevo, en uso real en una iglesia de Uruguay).
- Funciones que sean sólo ideas (NDI, Google Slides con enlace, etc.).
- Nada de estrellas, reseñas o testimonios inventados.
- No incluir textos de Biblias con derechos (RVR1960, NVI...): sólo las libres
  que trae el instalador (RV1909, BLM, VBL).

## Cómo hacerla

- **GitHub Pages** (gratis), desde este mismo repositorio, en una carpeta propia
  (por ejemplo `sitio/`) publicada con un flujo de GitHub Actions. La dirección
  queda `https://ecclesiacast.github.io/EcclesiaCast/`. Un dominio propio es
  opcional y cuesta dinero: no se compra sin preguntar.
- HTML y CSS simples, **sin dependencias ni servicios externos** (nada de
  Google Fonts ni analíticas: el proyecto tiene una política de privacidad y no
  espía a nadie). Que se vea bien en el celular.
- Título, descripción y etiquetas de redes sociales bien puestos (para que al
  compartir el enlace salga una tarjeta con imagen), y `sitemap.xml`.
- Las capturas se sacan del programa real. **Abrir el programa en la PC del
  usuario sólo si él lo autoriza** (ver la nota de la memoria sobre no abrirlo
  mientras la usa para trabajar).

## Después de la página

Para reunir las «señales» que pide SignPath (y para que más iglesias lo
conozcan): un video de demostración en YouTube, contarlo en grupos de iglesias y
de tecnología para iglesias, y pedir a las iglesias que lo usen que lo marquen
con una estrella en GitHub. Eso va en `docs/pendientes.md`, sección de la firma.
