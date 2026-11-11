// Contador anónimo de uso de EcclesiaCast: un Worker de Cloudflare con una
// base D1 (ver schema.sql y LEEME.md).
//
//   POST /v1/uso      el programa avisa «esta instalación proyectó algo hoy»
//   GET  /v1/resumen  cuántas instalaciones se usaron por semana y por fin
//                     de semana (pide la clave RESUMEN_CLAVE)
//
// No se guarda la dirección IP ni ningún dato de la persona: sólo el día, un
// número al azar por instalación y la versión.

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

function pagina(datos) {
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

export default {
  async fetch(pedido, env) {
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
      return new Response(pagina(datos), { headers: { "content-type": "text/html; charset=utf-8" } });
    }

    return new Response(null, { status: 404 });
  },
};
