// Servicio de EcclesiaCast en Cloudflare (api.ecclesiacast.org): un solo
// Worker, en un solo archivo para poder pegarlo entero en el panel.
//
//   POST /v1/uso      el programa avisa «esta instalación proyectó algo hoy»
//   GET  /v1/resumen  cuántas instalaciones se usaron por semana y por fin
//                     de semana (pide la clave RESUMEN_CLAVE)
//   GET  /v1/fondos   busca videos o fotos de fondo en Pexels con la clave
//                     de EcclesiaCast (PEXELS_CLAVE), que nunca sale de acá
//
// No se guarda la dirección IP ni ningún dato de la persona.

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

export async function resumen(db) {
  const semanas = (await db.prepare(RESUMEN_SQL).all()).results;
  const nuevas = new Map((await db.prepare(NUEVAS_SQL).all()).results.map((f) => [f.semana, f.nuevas]));
  const versiones = (await db.prepare(VERSIONES_SQL).all()).results;
  return {
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
  return `<!doctype html><html lang="es"><meta charset="utf-8"><title>EcclesiaCast · uso</title>
<style>body{font-family:system-ui,sans-serif;background:#08142A;color:#EAF0F8;padding:24px}
table{border-collapse:collapse;margin:12px 0 28px}td,th{padding:8px 14px;border-bottom:1px solid #22355A;text-align:right}
th{color:#F4B942;font-weight:600}td:first-child,th:first-child{text-align:left}h1,h2{font-weight:600}</style>
<h1>EcclesiaCast · instalaciones que proyectaron algo</h1>
<p>Cada número es una instalación distinta (una PC). Una iglesia con dos PCs cuenta dos.</p>
<h2>Por semana (empieza el lunes)</h2>
<table><tr><th>Semana</th><th>Sábado o domingo</th><th>Toda la semana</th><th>Nuevas</th></tr>${filas}</table>
<h2>Versiones (últimas 4 semanas)</h2>
<table><tr><th>Versión</th><th>Instalaciones</th></tr>${versiones}</table></html>`;
}

// ── Fondos de Pexels ────────────────────────────────────────────────────

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
 * 1920 de ancho (un proyector no muestra más), en mp4.
 */
export function mejorArchivo(archivos) {
  const mp4 = (archivos ?? []).filter((a) => a.file_type === "video/mp4" && a.width && a.link);
  const caben = mp4.filter((a) => a.width <= 1920);
  const elegidos = caben.length > 0 ? caben : mp4;
  elegidos.sort((a, b) => (caben.length > 0 ? b.width - a.width : a.width - b.width));
  const a = elegidos[0];
  return a ? { url: a.link, ancho: a.width, alto: a.height } : null;
}

/** Lo que devuelve Pexels, con sólo lo que el programa necesita. */
export function resumirPexels(tipo, datos) {
  const lista = tipo === "fotos" ? datos.photos ?? [] : datos.videos ?? [];
  const resultados = lista
    .map((x) => tipo === "fotos"
      ? {
          id: x.id, tipo: "foto", ancho: x.width, alto: x.height, duracion: 0,
          miniatura: x.src?.medium, autor: x.photographer, autorUrl: x.photographer_url, paginaUrl: x.url,
          archivo: x.src?.large2x ? { url: x.src.large2x, ancho: Math.min(x.width, 1880), alto: 0 } : null,
        }
      : {
          id: x.id, tipo: "video", ancho: x.width, alto: x.height, duracion: x.duration,
          miniatura: x.image, autor: x.user?.name, autorUrl: x.user?.url, paginaUrl: x.url,
          archivo: mejorArchivo(x.video_files),
        })
    .filter((r) => r.archivo && r.miniatura);
  return { resultados, hayMas: Boolean(datos.next_page) };
}

async function buscarFondos(url, env, ctx) {
  const pedido = pedidoDeFondos(url);
  if (!pedido) return Response.json({ error: "Escribí qué buscar (de 2 a 60 letras)." }, { status: 400 });
  if (!env.PEXELS_CLAVE) return Response.json({ error: "Falta la clave de Pexels." }, { status: 503 });

  // La misma búsqueda se contesta de la memoria de Cloudflare por un día:
  // así la clave no se pasa de los 200 pedidos por hora.
  const clave = new Request(`https://api.ecclesiacast.org/cache/fondos?q=${encodeURIComponent(pedido.q)}&tipo=${pedido.tipo}&pagina=${pedido.pagina}`);
  const memoria = caches.default;
  const guardada = await memoria.match(clave);
  if (guardada) return guardada;

  const base = pedido.tipo === "fotos" ? "https://api.pexels.com/v1/search" : "https://api.pexels.com/videos/search";
  const pexels = new URL(base);
  pexels.searchParams.set("query", pedido.q);
  pexels.searchParams.set("orientation", "landscape");
  pexels.searchParams.set("per_page", String(POR_PAGINA));
  pexels.searchParams.set("page", String(pedido.pagina));
  pexels.searchParams.set("locale", "es-ES");
  if (pedido.tipo === "videos") pexels.searchParams.set("size", "medium");

  const respuesta = await fetch(pexels, { headers: { Authorization: env.PEXELS_CLAVE } });
  if (respuesta.status === 429)
    return Response.json({ error: "Hubo muchas búsquedas en la última hora. Probá en un rato." }, { status: 429 });
  if (!respuesta.ok)
    return Response.json({ error: "Pexels no respondió. Probá de nuevo." }, { status: 502 });

  const cuerpo = Response.json(resumirPexels(pedido.tipo, await respuesta.json()), {
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
      await env.DB.prepare("INSERT OR IGNORE INTO uso (dia, instalacion, version) VALUES (?1, ?2, ?3)")
        .bind(fila.dia, fila.instalacion, fila.version)
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
