# Contador anónimo de uso

Cuenta cuántas instalaciones de EcclesiaCast proyectaron algo cada semana y
cada fin de semana. El programa avisa una vez por día, la primera vez que se
proyecta, con tres datos: un número al azar de la instalación, el día y la
versión. **No se guarda la dirección IP** ni nada de la iglesia o de lo que
se proyecta. Se apaga en Ajustes → Actualizaciones.

El programa le habla a `https://contador.ecclesiacast.org/v1/uso`. Mientras
el servicio no esté publicado, el programa no se queja: el día simplemente
no se cuenta.

## Publicarlo (una sola vez, desde el panel de Cloudflare)

Hace falta que `ecclesiacast.org` esté en la misma cuenta de Cloudflare. Si
el dominio se compró en Cloudflare, ya está.

1. **Base de datos.** *Storage & Databases → D1 → Create database*, nombre
   `ecclesiacast-contador`. Abrila, pestaña *Console*, pegá todo
   `schema.sql` y tocá *Execute*.
2. **El servicio.** *Compute → Workers & Pages → Create → Worker*, nombre
   `ecclesiacast-contador`, *Deploy*. Después *Edit code*: borrá lo que trae,
   pegá todo `src/contador.js` y *Deploy*.
3. **Conectar la base.** En el Worker: *Settings → Bindings → Add → D1
   database*. Nombre de la variable: `DB` (en mayúsculas). Base:
   `ecclesiacast-contador`.
4. **La clave para ver los números.** *Settings → Variables and Secrets →
   Add*, tipo *Secret*, nombre `RESUMEN_CLAVE`, y como valor una contraseña
   larga que inventes (guardala: es la que vas a usar para mirar).
5. **La dirección.** *Settings → Domains & Routes → Add → Custom domain*:
   `contador.ecclesiacast.org`.

Para probarlo, abrí en el navegador
`https://contador.ecclesiacast.org/v1/resumen?clave=TU-CLAVE`: tiene que
mostrar la tabla vacía. Con el programa nuevo instalado, proyectá algo y a
los pocos segundos tu PC aparece en la semana de hoy.

## Mirar los números

`https://contador.ecclesiacast.org/v1/resumen?clave=TU-CLAVE` muestra, por
semana (de lunes a domingo):

- **Sábado o domingo:** instalaciones que proyectaron algo el fin de semana.
  Es el número que más se parece a «iglesias que lo usaron en el culto».
- **Toda la semana:** las que proyectaron algún día.
- **Nuevas:** las que proyectaron por primera vez esa semana.

Y qué versiones se usaron en las últimas 4 semanas. Con `&formato=json`
devuelve lo mismo en datos.

Ojo: cada PC cuenta por separado. Una iglesia con dos PCs suma dos, y quien
lo prueba en su casa también suma.

## Costo

Entra en el plan gratis de Cloudflare: 100.000 pedidos por día para Workers
y 100.000 escrituras por día para D1. Con una escritura por instalación por
día, alcanza para miles de iglesias.

## Probar sin Cloudflare

`node servicios/contador/prueba.mjs` prueba el servicio contra el SQLite que
trae Node, sin instalar nada.
