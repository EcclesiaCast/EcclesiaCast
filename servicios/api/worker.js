// Servicio de EcclesiaCast en Cloudflare (api.ecclesiacast.org): un solo
// Worker, en un solo archivo para poder pegarlo entero en el panel.
//
//   POST /v1/uso      el programa avisa «esta instalación proyectó algo hoy»
//   GET  /v1/resumen  cuántas instalaciones se usaron por semana y por fin
//                     de semana (pide la clave RESUMEN_CLAVE)
//   GET  /v1/fondos   busca videos o fotos de fondo en Pixabay con la clave
//                     de EcclesiaCast (PIXABAY_KEY), que nunca sale de acá
//
// No se guarda la dirección IP ni ningún dato de la persona. Del lugar sólo
// queda el país, que Cloudflare informa con cada pedido.

// ── Contador de uso ─────────────────────────────────────────────────────

const INSTALACION = /^[0-9a-f]{32}$/;
const DIA = /^\d{4}-\d{2}-\d{2}$/;
const VERSION = /^[0-9A-Za-z.+\-]{1,40}$/;

/** Revisa lo que mandó el programa. Devuelve la fila a guardar, o null si algo no cuadra. */
export function validar(cuerpo, ahora = new Date()) {
  if (!cuerpo || typeof cuerpo !== "object") return null;
  const instalacion = String(cuerpo.instalacion ?? "").toLowerCase();
  const dia = String(cuerpo.dia ?? "");
  const version = String(cuerpo.version ?? "").split("+")[0];
  if (!INSTALACION.test(instalacion) || !DIA.test(dia) || !VERSION.test(version)) return null;

  // El día lo pone la PC de la iglesia: se acepta el de hoy en cualquier
  // huso horario (un día antes o después de hoy en UTC), nada más.
  const fecha = Date.parse(dia + "T12:00:00Z");
  if (Number.isNaN(fecha) || Math.abs(fecha - ahora.getTime()) > 2 * 86400000) return null;

  return { dia, instalacion, version };
}

/**
 * El país desde donde llegó el aviso, como lo informa Cloudflare (dos letras,
 * «UY»). Es lo único del lugar que se guarda: la dirección IP no. Vacío si
 * Cloudflare no lo sabe («XX») o si llega por la red Tor («T1»).
 */
export function paisDe(pedido) {
  const pais = String(pedido?.cf?.country ?? "").toUpperCase();
  return /^[A-Z]{2}$/.test(pais) && pais !== "XX" ? pais : "";
}

const RESUMEN_SQL = `
  SELECT date(dia, '-6 days', 'weekday 1')                                  AS semana,
         COUNT(DISTINCT instalacion)                                         AS en_la_semana,
         COUNT(DISTINCT CASE WHEN strftime('%w', dia) IN ('0', '6')
                             THEN instalacion END)                           AS en_el_fin_de_semana
    FROM uso
   GROUP BY semana
   ORDER BY semana DESC
   LIMIT 52`;

const NUEVAS_SQL = `
  SELECT date(primera, '-6 days', 'weekday 1') AS semana, COUNT(*) AS nuevas
    FROM (SELECT instalacion, MIN(dia) AS primera FROM uso GROUP BY instalacion)
   GROUP BY semana`;

const VERSIONES_SQL = `
  SELECT version, COUNT(DISTINCT instalacion) AS instalaciones
    FROM uso
   WHERE dia >= date('now', '-28 days')
   GROUP BY version
   ORDER BY instalaciones DESC`;

// Una instalación cuenta en el último país desde donde avisó.
const PAISES_SQL = `
  SELECT pais,
         COUNT(*)                                                   AS instalaciones,
         SUM(CASE WHEN ultimo >= date('now', '-28 days') THEN 1 ELSE 0 END) AS ultimas_4_semanas
    FROM (SELECT instalacion, pais, MAX(dia) AS ultimo FROM uso GROUP BY instalacion)
   GROUP BY pais
   ORDER BY instalaciones DESC, pais`;

const nombres = new Intl.DisplayNames(["es"], { type: "region" });

/** «Uruguay» para «UY»; «Sin dato» cuando no se supo el país. */
export function nombreDePais(pais) {
  if (!pais) return "Sin dato";
  try {
    return nombres.of(pais) ?? pais;
  } catch {
    return pais;
  }
}

export async function resumen(db) {
  const semanas = (await db.prepare(RESUMEN_SQL).all()).results;
  const nuevas = new Map((await db.prepare(NUEVAS_SQL).all()).results.map((f) => [f.semana, f.nuevas]));
  const versiones = (await db.prepare(VERSIONES_SQL).all()).results;
  const paises = (await db.prepare(PAISES_SQL).all()).results;
  return {
    paises: paises.map((p) => ({ ...p, nombre: nombreDePais(p.pais) })),
    semanas: semanas.map((s) => ({ ...s, nuevas: nuevas.get(s.semana) ?? 0 })),
    versiones_ultimas_4_semanas: versiones,
  };
}

function paginaResumen(datos) {
  const filas = datos.semanas
    .map((s) => `<tr><td>${s.semana}</td><td>${s.en_el_fin_de_semana}</td><td>${s.en_la_semana}</td><td>${s.nuevas}</td></tr>`)
    .join("");
  const versiones = datos.versiones_ultimas_4_semanas
    .map((v) => `<tr><td>${v.version}</td><td>${v.instalaciones}</td></tr>`)
    .join("");
  const paises = datos.paises
    .map((p) => `<tr><td>${p.nombre}</td><td>${p.ultimas_4_semanas}</td><td>${p.instalaciones}</td></tr>`)
    .join("");
  return `<!doctype html><html lang="es"><meta charset="utf-8"><title>EcclesiaCast · uso</title>
<style>body{font-family:system-ui,sans-serif;background:#08142A;color:#EAF0F8;padding:24px}
table{border-collapse:collapse;margin:12px 0 28px}td,th{padding:8px 14px;border-bottom:1px solid #22355A;text-align:right}
th{color:#F4B942;font-weight:600}td:first-child,th:first-child{text-align:left}h1,h2{font-weight:600}</style>
<h1>EcclesiaCast · instalaciones que proyectaron algo</h1>
<p>Cada número es una instalación distinta (una PC). Una iglesia con dos PCs cuenta dos.</p>
<h2>Por semana (empieza el lunes)</h2>
<table><tr><th>Semana</th><th>Sábado o domingo</th><th>Toda la semana</th><th>Nuevas</th></tr>${filas}</table>
<h2>Por país</h2>
<p>Cada instalación cuenta en el último país desde donde avisó.</p>
<table><tr><th>País</th><th>Últimas 4 semanas</th><th>Desde el principio</th></tr>${paises}</table>
<h2>Versiones (últimas 4 semanas)</h2>
<table><tr><th>Versión</th><th>Instalaciones</th></tr>${versiones}</table></html>`;
}

// ── Fondos de Pixabay ────────────────────────────────────────────────────

const POR_PAGINA = 24;

/** Revisa la búsqueda. Devuelve { q, tipo, pagina } o null. */
export function pedidoDeFondos(url) {
  const q = (url.searchParams.get("q") ?? "").trim().replace(/\s+/g, " ").toLowerCase();
  const tipo = url.searchParams.get("tipo") === "fotos" ? "fotos" : "videos";
  const pagina = Number.parseInt(url.searchParams.get("pagina") ?? "1", 10);
  if (q.length < 2 || q.length > 60 || !Number.isInteger(pagina) || pagina < 1 || pagina > 20) return null;
  return { q, tipo, pagina };
}

/**
 * El archivo de video que conviene bajar: el más grande que no pase de
 * 1920 de ancho (un proyector no muestra más). Pixabay ofrece cuatro
 * tamaños (large, medium, small, tiny); algunos vienen vacíos.
 */
export function mejorArchivo(tamanos) {
  const todos = Object.values(tamanos ?? {}).filter((a) => a && a.url && a.width);
  const caben = todos.filter((a) => a.width <= 1920);
  const elegidos = caben.length > 0 ? caben : todos;
  elegidos.sort((a, b) => (caben.length > 0 ? b.width - a.width : a.width - b.width));
  const a = elegidos[0];
  return a ? { url: a.url, ancho: a.width, alto: a.height, miniatura: a.thumbnail } : null;
}

const autorUrl = (x) => (x.user && x.user_id ? `https://pixabay.com/users/${encodeURIComponent(x.user)}-${x.user_id}/` : undefined);

/** Lo que devuelve Pixabay, con sólo lo que el programa necesita. */
export function resumirPixabay(tipo, datos, pagina = 1) {
  const lista = datos.hits ?? [];
  const resultados = lista
    .map((x) => {
      if (tipo === "fotos") {
        return {
          id: x.id, tipo: "foto", ancho: x.imageWidth, alto: x.imageHeight, duracion: 0,
          miniatura: x.webformatURL, autor: x.user, autorUrl: autorUrl(x), paginaUrl: x.pageURL,
          archivo: x.largeImageURL ? { url: x.largeImageURL, ancho: Math.min(x.imageWidth, 1280), alto: 0 } : null,
        };
      }
      const archivo = mejorArchivo(x.videos);
      return {
        id: x.id, tipo: "video", ancho: archivo?.ancho ?? 0, alto: archivo?.alto ?? 0, duracion: x.duration,
        miniatura: archivo?.miniatura, autor: x.user, autorUrl: autorUrl(x), paginaUrl: x.pageURL,
        archivo: archivo ? { url: archivo.url, ancho: archivo.ancho, alto: archivo.alto } : null,
      };
    })
    .filter((r) => r.archivo && r.miniatura);
  return { resultados, hayMas: pagina * POR_PAGINA < (datos.totalHits ?? 0) };
}

async function buscarFondos(url, env, ctx) {
  const pedido = pedidoDeFondos(url);
  if (!pedido) return Response.json({ error: "Escribí qué buscar (de 2 a 60 letras)." }, { status: 400 });
  if (!env.PIXABAY_KEY) return Response.json({ error: "Falta la clave de Pixabay." }, { status: 503 });

  // Pixabay pide guardar cada respuesta 24 horas: la misma búsqueda se
  // contesta de la memoria de Cloudflare por un día (y la clave no se pasa
  // de los 100 pedidos por minuto).
  const clave = new Request(`https://api.ecclesiacast.org/cache/fondos?q=${encodeURIComponent(pedido.q)}&tipo=${pedido.tipo}&pagina=${pedido.pagina}`);
  const memoria = caches.default;
  const guardada = await memoria.match(clave);
  if (guardada) return guardada;

  const pixabay = new URL(pedido.tipo === "fotos" ? "https://pixabay.com/api/" : "https://pixabay.com/api/videos/");
  pixabay.searchParams.set("key", env.PIXABAY_KEY);
  pixabay.searchParams.set("q", pedido.q);
  pixabay.searchParams.set("lang", "es");
  pixabay.searchParams.set("safesearch", "true");
  pixabay.searchParams.set("per_page", String(POR_PAGINA));
  pixabay.searchParams.set("page", String(pedido.pagina));
  if (pedido.tipo === "fotos") {
    pixabay.searchParams.set("image_type", "photo");
    pixabay.searchParams.set("orientation", "horizontal");
  }

  const respuesta = await fetch(pixabay);
  if (respuesta.status === 429)
    return Response.json({ error: "Hubo muchas búsquedas en el último minuto. Probá en un rato." }, { status: 429 });
  if (!respuesta.ok)
    return Response.json({ error: "Pixabay no respondió. Probá de nuevo." }, { status: 502 });

  const cuerpo = Response.json(resumirPixabay(pedido.tipo, await respuesta.json(), pedido.pagina), {
    headers: { "cache-control": "public, max-age=86400" },
  });
  ctx?.waitUntil?.(memoria.put(clave, cuerpo.clone()));
  return cuerpo;
}

// ── Entrada ─────────────────────────────────────────────────────────────

export default {
  async fetch(pedido, env, ctx) {
    const url = new URL(pedido.url);

    if (url.pathname === "/v1/uso" && pedido.method === "POST") {
      let cuerpo;
      try {
        cuerpo = await pedido.json();
      } catch {
        return new Response(null, { status: 400 });
      }
      const fila = validar(cuerpo);
      if (!fila) return new Response(null, { status: 400 });
      await env.DB.prepare("INSERT OR IGNORE INTO uso (dia, instalacion, version, pais) VALUES (?1, ?2, ?3, ?4)")
        .bind(fila.dia, fila.instalacion, fila.version, paisDe(pedido))
        .run();
      return new Response(null, { status: 204 });
    }

    if (url.pathname === "/v1/resumen" && pedido.method === "GET") {
      if (!env.RESUMEN_CLAVE || url.searchParams.get("clave") !== env.RESUMEN_CLAVE)
        return new Response("No.", { status: 403 });
      const datos = await resumen(env.DB);
      if (url.searchParams.get("formato") === "json")
        return Response.json(datos);
      return new Response(paginaResumen(datos), { headers: { "content-type": "text/html; charset=utf-8" } });
    }

    if (url.pathname === "/v1/fondos" && pedido.method === "GET")
      return buscarFondos(url, env, ctx);

    return new Response(null, { status: 404 });
  },
};
