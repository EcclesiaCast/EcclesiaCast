# Biblias que trae el instalador

El instalador de EcclesiaCast se publica en GitHub para cualquiera, así que
**sólo trae Biblias de dominio público o con una licencia libre que permite
redistribuirlas**. Cada archivo `.ecbiblias` de esta carpeta es un zip con el
texto de una versión (en el JSON que lee el programa) y su `manifest.json`.

Los textos se bajaron de [eBible.org](https://ebible.org) el 14/10/2026 (formato
USFX) y se convirtieron con un programa propio de una sola vez: se conserva el
texto de los versículos y se descartan las notas al pie, los títulos de sección,
las referencias cruzadas y las marcas de formato. Cada página de origen
publica la licencia de su versión.

| Archivo | Versión | Licencia | Origen |
|---|---|---|---|
| `RV1909.ecbiblias` | Reina-Valera 1909 (RV1909) | Dominio público | <https://ebible.org/Scriptures/details.php?id=spaRV1909> |
| `BLM.ecbiblias` | Biblia libre para el mundo (BLM) | Dominio público (CC0) | <https://ebible.org/Scriptures/details.php?id=spablm> |
| `VBL.ecbiblias` | Versión Biblia Libre (VBL) | Creative Commons Atribución-CompartirIgual 4.0 (CC BY-SA 4.0) | <https://ebible.org/Scriptures/details.php?id=spavbl> |
| `PDDPT.ecbiblias` | Palabra de Dios para ti (PDDPT) | Creative Commons Atribución 4.0 / CC BY-SA 4.0 (ver abajo) | <https://ebible.org/Scriptures/details.php?id=spapddpt> |

## Reina-Valera 1909

- Traducción: Casiodoro de Reina y Cipriano de Valera; revisión de 1909.
- Licencia: dominio público. eBible.org la publica como «Public Domain /
  Dominio Público».
- Observación: la fuente trae vacíos 18 versículos (por ejemplo Job 38:39-41);
  en la versión instalada simplemente no aparecen.

## Biblia libre para el mundo (BLM)

- Traducción: David Williams y Michael Paul Johnson, a partir de la «World
  English Bible» (también de dominio público).
- Licencia: dominio público; la introducción de la propia obra la dedica con la
  CC0 1.0 (<https://creativecommons.org/publicdomain/zero/1.0/deed.es_ES>).
- Observación: sus autores la describen como un **borrador de traducción que
  sigue en revisión**. Por eso el instalador no la tilda por defecto.
- Contacto de los autores para avisar errores: spablm@eBible.org.

## Versión Biblia Libre (VBL)

- Copyright © 2018-2025 Jonathan Gallagher y Shelly Barrios de Avila.
  Colaboradores: Gustavo Sanabria y Rebekah Põldaas.
  Sitio de la obra: <http://www.freebibleversion.org>.
- Licencia: Creative Commons Atribución-CompartirIgual 4.0
  (<http://creativecommons.org/licenses/by-sa/4.0/>). Se puede compartir y
  redistribuir en cualquier formato y hacer revisiones razonables, siempre que
  se mantenga esta nota de copyright y la fuente, se indique cualquier cambio
  que se haga al texto (sin sugerir que los autores lo aprueban) y lo que se
  redistribuya lleve la misma licencia.
- Cambios hechos acá: sólo de formato (se pasó del USFX a JSON y se quitaron
  notas, títulos y marcas). El texto de los versículos no se tocó. Este
  archivo conserva la licencia CC BY-SA 4.0 del original.
- Versión de la fuente: 5.2 de octubre de 2025.

## Palabra de Dios para ti (PDDPT)

- Traducción: «Biblia Latinoamericana Textual», de la Asociación Bíblica
  Latinoamericana (<https://rnova2.com>); © 2020. Se abrevia **PDDPT** para no
  confundirla con la «Palabra de Dios para Todos» (PDT), que tiene derechos y no
  entra.
- Licencia: la página de eBible.org la publica con Creative Commons Atribución 4.0
  y la introducción del propio texto dice CC BY-SA 4.0. Se cumple la más
  exigente (BY-SA): se conserva esta nota de copyright y la fuente, se indica
  cualquier cambio y lo que se redistribuya lleva la misma licencia.
- Cambios hechos acá: sólo de formato (del USFX a JSON; se quitaron notas, títulos
  y marcas). El texto de los versículos no se tocó.
- Origen: <https://ebible.org/Scriptures/details.php?id=spapddpt>, bajada el
  16/10/2026 (fuente del 13/10/2026). Si encuentra algún error: spapddpt@eBible.org.
- Observación: sigue el texto crítico, así que algunos versículos difieren de la
  Reina-Valera (por ejemplo Marcos 16 termina en el versículo 9) y 11 versículos
  vienen vacíos en la fuente.

## Qué no entra (y por qué)

Se revisó el catálogo en castellano de eBible.org y se **descartaron**:

- **Reina Valera Gómez (sparvg)**: © Humberto Gómez Caballero; prohíbe
  reproducirla con fines de lucro y sólo permite repartirla sin cambiar el
  texto (no es una licencia libre).
- **Santa Biblia Valera 1602 Purificada (spav1602p)**: «Todos los derechos
  reservados» (Iglesia Bautista Bíblica de la Gracia).
- **La Biblia de las Américas (LBLA)** y **Nueva Biblia Latinoamericana de Hoy
  (NBLH)**: © The Lockman Foundation; hace falta su permiso.
- Cualquier otra con derechos (RVR1960, NTV, NVI, TLA, PDT, TCB...): **no va
  en el repositorio ni en el instalador**. Para esas, cada operador arma un
  paquete propio con las que ya tiene en su biblioteca (Archivo > Exportar
  paquete de Biblias) y lo usa sólo en sus propias computadoras.

Ante la duda sobre la licencia de una versión, no se incluye.
