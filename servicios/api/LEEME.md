# Servicio de EcclesiaCast (api.ecclesiacast.org)

Un solo servicio en Cloudflare, en un solo archivo (`worker.js`), con dos
trabajos:

- **Contador anónimo de uso.** El programa avisa una vez por día, la primera
  vez que se proyecta, con tres datos: un número al azar de la instalación,
  el día y la versión. **No se guarda la dirección IP** ni nada de la iglesia
  o de lo que se proyecta. Se apaga en Ajustes → Actualizaciones.
- **Fondos de Pexels.** El programa busca videos y fotos de fondo en Pexels a
  través de este servicio, que guarda la clave de Pexels: la clave no puede
  estar en el programa porque el código es público. Cada búsqueda se recuerda
  un día, para no pasar el límite de Pexels (200 búsquedas por hora). Los
  videos se bajan directo de Pexels, no pasan por acá.

Mientras el servicio no esté publicado, el programa no se queja: el uso no se
cuenta y «Buscar fondos en Pexels» dice que no está disponible.

## Antes: la clave de Pexels

1. Entrá a <https://www.pexels.com/api/> y creá una cuenta (o entrá con la
   tuya).
2. *Your API Key* → completá para qué es: «EcclesiaCast, programa libre para
   proyectar en iglesias; busca videos y fotos de fondo» y la dirección
   `https://ecclesiacast.org`.
3. Copiá la clave que te dan. No la pegues en ningún lado más que en el paso
   4 de abajo.

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
   - `PEXELS_CLAVE` = la clave de Pexels.
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

Y qué versiones se usaron en las últimas 4 semanas. Con `&formato=json`
devuelve lo mismo en datos. Cada PC cuenta por separado: una iglesia con dos
PCs suma dos.

## Si se queda corto con Pexels

Con muchas iglesias buscando, el límite de 200 búsquedas por hora puede no
alcanzar. Pexels lo levanta gratis si se lo pedís desde su página de la API,
mostrando que el programa los nombra (la ventana de búsqueda dice «Fotos y
videos de Pexels» y el autor de cada uno).

## Costo

Entra en el plan gratis de Cloudflare: 100.000 pedidos por día para Workers
y 100.000 escrituras por día para D1.

## Probar sin Cloudflare

`node servicios/api/prueba.mjs` prueba el contador contra el SQLite que trae
Node y la parte de Pexels sin salir a internet. No instala nada.
