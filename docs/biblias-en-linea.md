# Biblias en línea con API.Bible (investigación del 16/10/2026)

**Por qué:** Tyndale contestó el 16/10/2026 que no puede dar un permiso para meter
la NTV dentro del programa y mandó a pedirla por [API.Bible](https://api.bible)
(un servicio de la American Bible Society que reparte textos con licencia). La
idea es una función nueva, **«Biblias en línea»**, que pide los versículos por
internet. No es un reemplazo de las Biblias del instalador: el programa sigue
funcionando sin conexión.

Todo lo de abajo sale de la documentación pública ([docs.api.bible](https://docs.api.bible)).
Lo que **no se pudo comprobar** está marcado.

## Cómo funciona

- **Dirección y clave.** `https://rest.api.bible/v1/…` con la clave en el
  encabezado `api-key`. La clave se saca gratis creando una cuenta en api.bible.
- **Listar Biblias.** `GET /v1/bibles?language=spa` (código de idioma de tres
  letras). Cada Biblia trae `id` (por ejemplo `78a9f6124f344018-01`),
  `abbreviation`, `name`, `language`, `type`; con `include-full-details=true`
  trae el aviso de derechos.
- **Libros y capítulos.** `/bibles/{id}/books`, `/books/{libro}/chapters`.
- **Pasajes.** `/bibles/{id}/passages/JHN.3.16-JHN.3.18` con
  `content-type=text` (también `html` o `json`), `include-verse-numbers`,
  `include-titles`, etc. Máximo **200 versículos** por pedido. La respuesta trae
  `content`, `reference` y `copyright`.
- **Buscar.** `/bibles/{id}/search?query=…`: un versículo coincide si tiene
  **todas** las palabras, en cualquier orden; admite `*` y `?`, `limit`, `offset`,
  `sort` y `fuzziness` (tolera errores de tipeo). Busca **dentro de una sola
  Biblia**, así que la búsqueda en todas las Biblias a la vez (Ctrl+K) no se
  puede hacer en línea sin un pedido por versión.
- **Formato.** Todas usan el estándar USX (el de USFM).
- **FUMS** (conteo de uso): obligatorio para aplicaciones web. Cada respuesta con
  Escritura trae un `fumsToken` y hay que informarlo (con su script, o con un GET
  a `https://fums.api.bible/f3`: token, id de dispositivo y de sesión, ambos
  anónimos). Los programas sin funciones web pueden saltearlo. **No se
  comprobó** si una aplicación de escritorio que muestra una web (WebView2) lo
  necesita: preguntar a support@api.bible si hace falta.

## Planes y límites

| | Starter | Pro | Enterprise |
|---|---|---|---|
| Precio | **$0** | pago (no publican el precio) | a medida |
| Consultas por mes | **5.000** | 150.000 | a medida |
| Biblias con derechos | hasta **3** | sin límite | sin límite |
| Biblias libres | todas | todas | todas |
| Uso | **sólo sin fines de lucro** | comercial | comercial |

- Las consultas que sobran **no se acumulan**; al llegar al tope el servicio se
  corta hasta el mes siguiente (en Starter no hay pago por excedente). En Pro,
  cada 1.000 consultas extra cuestan US$ 1.
- «Sin fines de lucro» es, según su guía, un proyecto que no genera ni va a
  generar ingresos; ponen como ejemplos una aplicación de estudio personal sin
  publicidad y **«un sitio web de una iglesia gratuito»**. EcclesiaCast, gratis y
  sin publicidad, entra ahí.
- **Todo lo que genera ingresos es comercial**, y las licencias comerciales
  siempre tienen costo mensual.

## Qué reglas pesan sobre un programa de escritorio

- **Guardar texto (caché):** se permite con límites: menos de **500 versículos
  seguidos**, y borrar la caché **cada 14 días o menos**. Un capítulo cabe (el
  más largo, Salmo 119, tiene 176 versículos); un libro entero, no. Sirve para
  tener a mano el capítulo del culto, no para bajar la Biblia completa.
- **Aviso de derechos:** hay que mostrarlo junto a todo el texto que se muestre.
  Hay que verlo cómo queda en la pantalla del proyector (en las Biblias del
  instalador el programa no lo muestra hoy).
- **No se puede sublicenciar** el contenido: no se puede revender ni repartir.
- **Una sola clave compartida no sirve:** con 5.000 consultas por mes para todas
  las iglesias y 3 Biblias con derechos por cuenta, se acabaría el primer
  domingo. La forma que cierra: **cada iglesia crea su cuenta gratis y pone su
  propia clave en el programa**, y elige en su cuenta qué Biblias quiere. Así el
  contrato lo firma cada iglesia, no EcclesiaCast, y los límites son de cada una.
- La clave **no debe ir escrita en el programa** (la documentación lo desaconseja
  en el código de un navegador, y en un programa instalado cualquiera la sacaría).
  Se guardaría en la biblioteca de cada iglesia.

## Qué Biblias en castellano podrían servir

**No se pudo ver el catálogo**: la tabla de api.bible sólo muestra las filas a
quien inició sesión, y crear la cuenta es cosa del dueño del proyecto. Lo que se
sabe:

- **Seguro (libres, en todos los planes):** las de dominio público y Creative
  Commons, que probablemente incluyen las que ya trae el instalador (RV1909, BLM,
  VBL y similares). Para esas no hace falta la función en línea.
- **Con derechos (a confirmar en el catálogo):** la **RVR1960** es de las tres
  traducciones que la American Bible Society administra directamente (junto con
  la CEV y la GNT), así que es la candidata más probable. Los socios que
  nombran son Biblica (NVI), **Tyndale (NTV)**, HarperCollins, Lockman (LBLA/NBLH),
  Holman (NVI en España/EE.UU., Biblias Holman) y NavPress, pero la
  documentación no dice qué versiones en castellano tiene cada uno.
- **TLA y PDT:** sin datos. La TLA es de las Sociedades Bíblicas Unidas y la PDT
  de la Liga Bíblica; ninguna aparece en lo leído.
- Algunas Biblias piden además un **acuerdo aparte con el dueño** («Unique
  License»), aunque se tenga cuenta.

**Para saber cuáles hay:** crear la cuenta gratis, entrar a api.bible/bibles y
filtrar por idioma «Spanish» (o llamar `GET /v1/bibles?language=spa` con la
clave). Con la lista se decide cuáles valen la pena: lo ideal es la NTV, la
RVR1960, la NVI y la TLA, y las 3 con derechos del plan gratis alcanzan para
una iglesia que elija bien.

## Cómo sería en el programa (idea, sin programar todavía)

1. Menú **Archivo → Biblias en línea…**: se pega la clave, el programa pide la
   lista de Biblias en castellano de esa clave y se tildan las que se quieren.
2. Esas Biblias aparecen en la fila de versiones con una marca de «en línea».
   Elegir libro y capítulo pide el capítulo (una consulta); proyectar un versículo
   no gasta consultas, porque sale del capítulo ya pedido.
3. **Caché** de capítulos recientes (nunca más de 500 versículos seguidos, borrada
   a los 14 días), para que un capítulo abierto siga andando si se cae el internet
   a mitad del culto.
4. **Buscar** funciona por Biblia en línea (palabras o referencia), no en todas
   juntas. La búsqueda por referencia («Juan 3:16») se puede resolver sin gastar
   consultas porque el programa ya conoce los libros.
5. Mostrar el aviso de derechos y avisar cuando se esté llegando al tope de
   5.000 consultas del mes.
6. Dos Biblias a la vez: una local y una en línea, o dos en línea (el pedido de
   cada una es aparte).

**Riesgos:** sin internet no hay texto (la caché ayuda pero no alcanza); el
límite de 5.000 consultas es el de cada iglesia; los términos pueden cambiar; y
**falta confirmar con API.Bible** el tema del FUMS y qué versiones en castellano
están disponibles. Esas dos preguntas se pueden hacer en su Discord (el enlace
está en el panel de cada cuenta) o a support@api.bible.
