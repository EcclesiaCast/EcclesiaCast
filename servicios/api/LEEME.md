# Servicio de EcclesiaCast (api.ecclesiacast.org)

Un solo servicio en Cloudflare, en un solo archivo (`worker.js`), con dos
trabajos:

- **Contador anónimo de uso.** El programa avisa una vez por día, la primera
  vez que se proyecta, con tres datos: un número al azar de la instalación,
  el día y la versión. Del lugar se guarda **sólo el país**, que Cloudflare
  informa con cada aviso. **No se guarda la dirección IP** ni nada de la
  iglesia o de lo que se proyecta. Se apaga en Ajustes → Actualizaciones.
- **Fondos de Pixabay.** El programa busca videos y fotos de fondo en Pixabay a
  través de este servicio, que guarda la clave de Pixabay: la clave no puede
  estar en el programa porque el código es público. Cada búsqueda se recuerda
  un día (Pixabay lo pide, y así no se pasa de sus 100 búsquedas por minuto).
  Los videos y fotos se bajan directo de Pixabay, no pasan por acá.

Mientras el servicio no esté publicado, el programa no se queja: el uso no se
cuenta y «Buscar fondos en Pixabay» dice que no está disponible.

## Antes: la clave de Pixabay

1. Entrá a <https://pixabay.com> y creá una cuenta (o entrá con la tuya).
2. Abrí <https://pixabay.com/api/docs/>: con la sesión iniciada, la clave
   aparece en esa página (en los ejemplos, en «Parameters → key»).
3. Copiá la clave. No la pegues en ningún lado más que en el paso 4 de abajo.

## Publicarlo (una sola vez, desde el panel de Cloudflare)

Hace falta que `ecclesiacast.org` esté en la misma cuenta de Cloudflare.

1. **Base de datos.** *Storage & Databases → D1 → Create database*, nombre
   `ecclesiacast-api`. Abrila, pestaña *Console*, pegá todo `schema.sql` y
   tocá *Execute*.
2. **El servicio.** *Compute → Workers & Pages → Create → Worker*, nombre
   `ecclesiacast-api`, *Deploy*. Después *Edit code*: borrá lo que trae, pegá
   todo `worker.js` y *Deploy*.
3. **Conectar la base.** En el Worker: *Settings → Bindings → Add → D1
   database*. Nombre de la variable: `DB` (en mayúsculas). Base:
   `ecclesiacast-api`.
4. **Las claves.** *Settings → Variables and Secrets → Add*, tipo *Secret*,
   dos veces:
   - `PIXABAY_KEY` = la clave de Pixabay.
   - `RESUMEN_CLAVE` = una contraseña larga que inventes, para mirar los
     números (guardala).
5. **La dirección.** *Settings → Domains & Routes → Add → Custom domain*:
   `api.ecclesiacast.org`.

Para probarlo, en el navegador:

- `https://api.ecclesiacast.org/v1/fondos?q=cruz` tiene que mostrar una lista
  de videos.
- `https://api.ecclesiacast.org/v1/resumen?clave=TU-CLAVE` tiene que mostrar
  la tabla del contador (vacía al principio).

## Mirar los números de uso

`https://api.ecclesiacast.org/v1/resumen?clave=TU-CLAVE` muestra, por semana
(de lunes a domingo):

- **Sábado o domingo:** instalaciones que proyectaron algo el fin de semana.
  Es el número que más se parece a «iglesias que lo usaron en el culto».
- **Toda la semana:** las que proyectaron algún día.
- **Nuevas:** las que proyectaron por primera vez esa semana.

También **por país**: cuántas instalaciones avisaron desde cada uno, en las
últimas 4 semanas y desde el principio (cada una cuenta en el último país
desde donde avisó).

Y qué versiones se usaron en las últimas 4 semanas. Con `&formato=json`
devuelve lo mismo en datos. Cada PC cuenta por separado: una iglesia con dos
PCs suma dos.

## Qué no se cuenta

Sólo cuentan las copias que usa una iglesia de verdad. El programa **no manda
el aviso** (y ni siquiera inventa su número) cuando:

- es una **versión de prueba** (lleva un guion: `1.5.0-prueba.2`, `1.5.0-beta.1`);
- corre con una **biblioteca de prueba** (la variable `ECCLESIACAST_DATA_DIR`,
  que se usa para probar con una copia de la biblioteca);
- **no está instalado**: corre desde una carpeta suelta y no desde donde lo dejó
  el instalador (se reconoce porque el instalador siempre deja su desinstalador,
  `unins000.exe`, al lado del programa).

Además, el servicio **rechaza** los avisos con versión de prueba, y el resumen
**no muestra** las filas viejas de versiones de prueba que hayan quedado de antes.

## Empezar de cero

Para que los números arranquen limpios (por ejemplo, cuando sale la 1.5.0),
hay que hacer dos cosas, en este orden. Las dos se hacen desde el panel de
Cloudflare (<https://dash.cloudflare.com>) y no tocan nada de las iglesias.

**1. Subir el servicio nuevo** (el que rechaza las versiones de prueba):

1. *Compute → Workers & Pages* → abrí `ecclesiacast-api`.
2. Arriba a la derecha, *Edit code*.
3. Borrá todo lo que hay, pegá **todo** el `worker.js` nuevo y tocá *Deploy*.
4. Para confirmar: abrí `https://api.ecclesiacast.org/v1/resumen?clave=TU-CLAVE`;
   tiene que seguir mostrando la tabla.

**2. Borrar lo contado hasta ahora** (no se puede deshacer):

1. *Storage & Databases → D1* → abrí la base `ecclesiacast-api`.
2. Pestaña *Console*.
3. Escribí exactamente `DELETE FROM uso;` y tocá *Execute*.
4. Abrí de nuevo el resumen: las tablas tienen que quedar vacías. Se van a
   llenar solas a medida que las iglesias proyecten con el programa.

Si querés guardar los números viejos antes de borrarlos, abrí primero
`https://api.ecclesiacast.org/v1/resumen?clave=TU-CLAVE&formato=json` y guardá
la página (Ctrl+S).

## Si se queda corto con Pixabay

Con muchas iglesias buscando, el límite de 100 búsquedas por minuto puede no
alcanzar. Pixabay lo levanta gratis si se lo pedís (hay que mostrar de dónde
salen: la ventana de búsqueda dice «Fotos y videos de Pixabay» con un enlace
a pixabay.com).

## Costo

Entra en el plan gratis de Cloudflare: 100.000 pedidos por día para Workers
y 100.000 escrituras por día para D1.

## Probar sin Cloudflare

`node servicios/api/prueba.mjs` prueba el contador contra el SQLite que trae
Node y la parte de Pixabay sin salir a internet. No instala nada.
