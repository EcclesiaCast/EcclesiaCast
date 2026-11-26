// Servicio de EcclesiaCast en Cloudflare (api.ecclesiacast.org): un solo
// Worker, en un solo archivo para poder pegarlo entero en el panel.
//
//   POST /v1/uso      el programa avisa «esta instalación proyectó algo hoy»
//   GET  /v1/resumen  cuántas instalaciones se usaron por semana y por fin
//                     de semana (pide la clave RESUMEN_CLAVE)
//   GET  /panel       el panel privado: uso, descargas de GitHub y visitas a
//                     la web (pide la misma clave, sin ponerla en la dirección)
//   GET  /v1/fondos   busca videos o fotos de fondo en Pixabay con la clave
//                     de EcclesiaCast (PIXABAY_KEY), que nunca sale de acá
//
// Además, una tarea diaria (Cron Trigger) anota el total de descargas de cada
// versión: GitHub sólo da el acumulado, y así se ve cuántas hubo por semana.
//
// No se guarda la dirección IP ni ningún dato de la persona. Del lugar sólo
// queda el país, que Cloudflare informa con cada pedido.

// ── Contador de uso ─────────────────────────────────────────────────────

const INSTALACION = /^[0-9a-f]{32}$/;
const DIA = /^\d{4}-\d{2}-\d{2}$/;
const VERSION = /^[0-9A-Za-z.+\-]{1,40}$/;

// Las versiones de prueba («1.4.0-prueba.4», «1.5.0-beta.1»: llevan «-») son
// de quienes hacen el programa, no de iglesias: no se guardan, y las filas
// viejas que quedaron no entran en el resumen.
export function esVersionDePrueba(version) {
  return String(version ?? "").split("+")[0].includes("-");
}
const REALES = "(SELECT * FROM uso WHERE version NOT LIKE '%-%')";

/** Revisa lo que mandó el programa. Devuelve la fila a guardar, o null si algo no cuadra. */
export function validar(cuerpo, ahora = new Date()) {
  if (!cuerpo || typeof cuerpo !== "object") return null;
  const instalacion = String(cuerpo.instalacion ?? "").toLowerCase();
  const dia = String(cuerpo.dia ?? "");
  const version = String(cuerpo.version ?? "").split("+")[0];
  if (!INSTALACION.test(instalacion) || !DIA.test(dia) || !VERSION.test(version)) return null;
  if (esVersionDePrueba(version)) return null;

  // El día lo pone la PC de la iglesia: se acepta el de hoy en cualquier
  // huso horario (un día antes o después de hoy en UTC), nada más.
  const fecha = Date.parse(dia + "T12:00:00Z");
  if (Number.isNaN(fecha) || Math.abs(fecha - ahora.getTime()) > 2 * 86400000) return null;

  return { dia, instalacion, version, iglesia: String(cuerpo.iglesia ?? "").replace(/\s+/g, " ").trim().slice(0, 60) };
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
    FROM ${REALES}
   GROUP BY semana
   ORDER BY semana DESC
   LIMIT 52`;

const NUEVAS_SQL = `
  SELECT date(primera, '-6 days', 'weekday 1') AS semana, COUNT(*) AS nuevas
    FROM (SELECT instalacion, MIN(dia) AS primera FROM ${REALES} GROUP BY instalacion)
   GROUP BY semana`;

const VERSIONES_SQL = `
  SELECT version, COUNT(DISTINCT instalacion) AS instalaciones
    FROM ${REALES}
   WHERE dia >= date('now', '-28 days')
   GROUP BY version
   ORDER BY instalaciones DESC`;

// Una instalación cuenta en el último país desde donde avisó.
const PAISES_SQL = `
  SELECT pais,
         COUNT(*)                                                   AS instalaciones,
         SUM(CASE WHEN ultimo >= date('now', '-28 days') THEN 1 ELSE 0 END) AS ultimas_4_semanas
    FROM (SELECT instalacion, pais, MAX(dia) AS ultimo FROM ${REALES} GROUP BY instalacion)
   GROUP BY pais
   ORDER BY instalaciones DESC, pais`;

// Las iglesias que pusieron su nombre (opcional): el último nombre de cada instalación.
const IGLESIAS_SQL = `
  SELECT u.iglesia, u.pais, MAX(u.dia) AS ultimo
    FROM ${REALES} u
   WHERE u.iglesia <> ''
     AND u.dia = (SELECT MAX(dia) FROM ${REALES} WHERE instalacion = u.instalacion AND iglesia <> '')
   GROUP BY u.instalacion
   ORDER BY ultimo DESC, u.iglesia`;

/** Lo que escribió una iglesia, listo para ir dentro de la página sin romperla. */
export function escapar(texto) {
  return String(texto).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

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
  const iglesias = (await db.prepare(IGLESIAS_SQL).all()).results;
  return {
    paises: paises.map((p) => ({ ...p, nombre: nombreDePais(p.pais) })),
    iglesias: iglesias.map((i) => ({ ...i, nombre_pais: nombreDePais(i.pais) })),
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
  const iglesias = datos.iglesias.length
    ? datos.iglesias.map((i) => `<tr><td>${escapar(i.iglesia)}</td><td>${i.nombre_pais}</td><td>${i.ultimo}</td></tr>`).join("")
    : `<tr><td colspan="3">Todavía ninguna puso su nombre.</td></tr>`;
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
<table><tr><th>Versión</th><th>Instalaciones</th></tr>${versiones}</table>
<h2>Iglesias que pusieron su nombre</h2>
<p>Es opcional (Ajustes → Actualizaciones). Una iglesia con dos PCs puede aparecer dos veces.</p>
<table><tr><th>Iglesia</th><th>País</th><th>Último uso</th></tr>${iglesias}</table></html>`;
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

// ── Panel privado ───────────────────────────────────────────────────────
//
// Junta en una página lo que está repartido: el uso (la base D1), las
// descargas (los releases de GitHub) y las visitas a la web (GoatCounter).
// No usa cookies ni scripts, y no junta ningún dato nuevo de las iglesias.
// Las claves (GOATCOUNTER_TOKEN y, si hace falta, GITHUB_TOKEN) son secretos
// del Worker: nunca van en la página.

const GITHUB_RELEASES = "https://api.github.com/repos/EcclesiaCast/EcclesiaCast/releases";
const GOATCOUNTER = "https://ecclesiacast.goatcounter.com/api/v0";
const DIAS_WEB = 28;

// Los botones de la web que se cuentan (sitio/*.html y sitio/descargar.js).
const BOTONES = [
  ["descargar", "Descargar el programa"],
  ["mandar-a-la-compu", "Abrió «Mandármelo a la compu» (celular)"],
  ["mandar-whatsapp", "Se lo mandó por WhatsApp"],
  ["mandar-correo", "Se lo mandó por correo"],
  ["mandar-compartir", "Se lo mandó con Compartir"],
  ["mandar-copiar", "Copió el enlace"],
  ["donar", "Donar por PayPal"],
];

/** Un error que se puede mostrar en el panel tal cual (sin claves ni detalles internos). */
class Aviso extends Error {}

const diaDe = (fecha) => fecha.toISOString().slice(0, 10);
const sumarDias = (dia, cuantos) => diaDe(new Date(Date.parse(dia + "T12:00:00Z") + cuantos * 86400000));
/** El lunes de la semana de ese día (igual que las semanas del uso). */
export const lunesDe = (dia) => sumarDias(dia, -((new Date(dia + "T12:00:00Z").getUTCDay() + 6) % 7));
const sumar = (lista, valor) => lista.reduce((total, x) => total + valor(x), 0);

/**
 * Recuerda un rato lo que contestó otro servicio, para no pedírselo en cada
 * visita. Los errores no se recuerdan.
 */
async function conMemoria(nombre, segundos, ctx, pedir) {
  const memoria = globalThis.caches?.default;
  const clave = new Request(`https://api.ecclesiacast.org/cache/panel/${nombre}`);
  const guardada = await memoria?.match(clave);
  if (guardada) return guardada.json();
  const datos = await pedir();
  if (memoria) {
    const guardar = memoria.put(clave, Response.json(datos, { headers: { "cache-control": `public, max-age=${segundos}` } }));
    ctx?.waitUntil?.(guardar);
  }
  return datos;
}

// Descargas (GitHub)

/** De los releases de GitHub: el instalador de cada versión y, aparte, los demás archivos. */
export function leerReleases(releases) {
  const instaladores = [];
  const otros = [];
  for (const r of releases) {
    if (r.draft) continue;
    const version = String(r.tag_name ?? "");
    for (const a of r.assets ?? []) {
      const total = Number(a.download_count) || 0;
      if (String(a.name).endsWith("-setup.exe"))
        instaladores.push({
          version, total, prueba: esVersionDePrueba(version),
          publicada: String(r.published_at ?? "").slice(0, 10),
        });
      else otros.push({ version, archivo: String(a.name), total });
    }
  }
  instaladores.sort((a, b) => b.publicada.localeCompare(a.publicada));
  return { instaladores, otros, total: sumar(instaladores.filter((i) => !i.prueba), (i) => i.total) };
}

async function pedirReleases(env) {
  // GitHub rechaza los pedidos sin «user-agent».
  const headers = { accept: "application/vnd.github+json", "user-agent": "ecclesiacast-panel" };
  if (env.GITHUB_TOKEN) headers.authorization = `Bearer ${env.GITHUB_TOKEN}`;
  const todos = [];
  for (let pagina = 1; pagina <= 10; pagina++) {
    const respuesta = await fetch(`${GITHUB_RELEASES}?per_page=100&page=${pagina}`, { headers });
    if (respuesta.status === 403 || respuesta.status === 429)
      throw new Aviso("GitHub rechazó la consulta (suele ser por demasiados pedidos). Probá en una hora; si sigue, cargá GITHUB_TOKEN (ver LEEME.md).");
    if (respuesta.status === 401)
      throw new Aviso("GitHub no aceptó la clave GITHUB_TOKEN. Puede estar vencida.");
    if (!respuesta.ok) throw new Aviso(`GitHub respondió con un error (${respuesta.status}).`);
    const lista = await respuesta.json();
    todos.push(...lista);
    if (lista.length < 100) break;
  }
  return leerReleases(todos);
}

/** La foto del día: cuántas descargas acumula cada versión (sin las de prueba). */
export async function guardarDescargas(db, dia, descargas) {
  const porVersion = new Map();
  for (const i of descargas.instaladores)
    if (!i.prueba) porVersion.set(i.version, (porVersion.get(i.version) ?? 0) + i.total);
  for (const [version, total] of porVersion)
    await db.prepare("INSERT INTO descargas (dia, version, total) VALUES (?1, ?2, ?3) ON CONFLICT (dia, version) DO UPDATE SET total = excluded.total")
      .bind(dia, version, total)
      .run();
}

/**
 * Descargas nuevas por semana: la diferencia entre cada foto y la anterior. La
 * primera foto es el punto de partida y no suma. Si faltó algún día, lo de ese
 * hueco cae en la semana de la foto siguiente.
 */
export function descargasPorSemana(filas) {
  const fotos = new Map();
  for (const f of filas) {
    if (!fotos.has(f.dia)) fotos.set(f.dia, []);
    fotos.get(f.dia).push(f);
  }
  const dias = [...fotos.keys()].sort();
  const semanas = new Map();
  // Lo último que se vio de cada versión: si una foto no la trae, después no vuelve a sumar entera.
  const visto = new Map((fotos.get(dias[0]) ?? []).map((f) => [f.version, f.total]));
  for (const dia of dias.slice(1)) {
    let nuevas = 0;
    for (const { version, total } of fotos.get(dia)) {
      nuevas += Math.max(0, total - (visto.get(version) ?? 0));
      visto.set(version, total);
    }
    const semana = lunesDe(dia);
    semanas.set(semana, (semanas.get(semana) ?? 0) + nuevas);
  }
  return { desde: dias[0] ?? null, semanas: [...semanas].map(([semana, descargas]) => ({ semana, descargas })) };
}

// Se mira el último medio año; «desde» es la primera foto de ese tramo.
async function leerHistorial(db, hoy) {
  const filas = (await db.prepare("SELECT dia, version, total FROM descargas WHERE dia >= ?1 ORDER BY dia")
    .bind(sumarDias(hoy, -190)).all()).results;
  return descargasPorSemana(filas);
}

// Visitas (GoatCounter)

async function pedirGoatcounter(env, ruta, parametros) {
  const url = new URL(GOATCOUNTER + ruta);
  for (const [nombre, valor] of Object.entries(parametros)) url.searchParams.set(nombre, valor);
  const respuesta = await fetch(url, {
    headers: { authorization: `Bearer ${env.GOATCOUNTER_TOKEN}`, "content-type": "application/json" },
  });
  if (respuesta.status === 401 || respuesta.status === 403)
    throw new Aviso("GoatCounter no aceptó la clave. Revisá GOATCOUNTER_TOKEN y que tenga el permiso «Read statistics».");
  if (respuesta.status === 429)
    throw new Aviso("GoatCounter pidió esperar (demasiados pedidos). Probá en un minuto.");
  if (!respuesta.ok) throw new Aviso(`GoatCounter respondió con un error (${respuesta.status}).`);
  return respuesta.json();
}

/**
 * De lo que devuelve GoatCounter: visitas por día, páginas y botones. Cuenta
 * visitantes por página: quien abrió dos páginas suma una en cada una. Los
 * botones («eventos») van aparte y no suman visitas.
 */
export function resumirWeb(hits, referencias, desde, hasta) {
  const dias = new Map();
  for (let d = desde; d <= hasta; d = sumarDias(d, 1)) dias.set(d, 0);
  const paginas = [];
  const eventos = new Map();
  for (const h of hits) {
    const cuenta = Number(h.count) || 0;
    if (h.event) {
      eventos.set(String(h.path), (eventos.get(String(h.path)) ?? 0) + cuenta);
      continue;
    }
    paginas.push({ ruta: String(h.path), visitas: cuenta });
    for (const s of h.stats ?? []) {
      const dia = String(s.day).slice(0, 10);
      if (dias.has(dia)) dias.set(dia, dias.get(dia) + (Number(s.daily) || 0));
    }
  }
  paginas.sort((a, b) => b.visitas - a.visitas);
  const conocidos = new Set(BOTONES.map(([ruta]) => ruta));
  return {
    desde,
    hasta,
    visitas: sumar(paginas, (p) => p.visitas),
    por_dia: [...dias].map(([dia, visitas]) => ({ dia, visitas })),
    paginas,
    botones: [
      ...BOTONES.map(([ruta, nombre]) => ({ ruta, nombre, veces: eventos.get(ruta) ?? 0 })),
      ...[...eventos].filter(([ruta]) => !conocidos.has(ruta)).map(([ruta, veces]) => ({ ruta, nombre: ruta, veces })),
    ],
    de_donde: referencias.map((r) => ({ nombre: r.name || "Directo o sin dato", visitas: Number(r.count) || 0 })),
  };
}

async function pedirWeb(env, hoy) {
  const desde = sumarDias(hoy, 1 - DIAS_WEB);
  const rango = { start: `${desde}T00:00:00Z`, end: `${hoy}T23:00:00Z` };

  // De a 100 direcciones; para la tanda siguiente se le dice cuáles ya mandó.
  const hits = [];
  for (let tanda = 0; tanda < 5; tanda++) {
    const parametros = { ...rango, limit: "100", group: "day" };
    if (hits.length > 0) parametros.exclude_paths = hits.map((h) => h.path_id).join(",");
    const datos = await pedirGoatcounter(env, "/stats/hits", parametros);
    hits.push(...(datos.hits ?? []));
    if (!datos.more) break;
  }
  const referencias = await pedirGoatcounter(env, "/stats/toprefs", { ...rango, limit: "10" });
  return resumirWeb(hits, referencias.stats ?? [], desde, hoy);
}

// La página

/** Cada fuente por separado: si una falla, las demás se ven igual. */
async function probar(pedir, mensaje) {
  try {
    return { datos: await pedir() };
  } catch (error) {
    if (error instanceof Aviso) return { error: error.message };
    console.error(error);
    return { error: mensaje };
  }
}

export async function panelDatos(env, ctx, ahora = new Date()) {
  const hoy = diaDe(ahora);
  const [uso, descargas, historial, web] = await Promise.all([
    probar(() => resumen(env.DB), "No se pudo leer la base del contador."),
    probar(() => conMemoria("github", 3600, ctx, () => pedirReleases(env)), "No se pudo conectar con GitHub."),
    probar(() => leerHistorial(env.DB, hoy), "No se pudo leer el historial. ¿Creaste la tabla «descargas»? (ver LEEME.md)"),
    probar(async () => {
      if (!env.GOATCOUNTER_TOKEN) throw new Aviso("Falta la clave GOATCOUNTER_TOKEN (ver LEEME.md).");
      return conMemoria(`web-${hoy}`, 600, ctx, () => pedirWeb(env, hoy));
    }, "No se pudo conectar con GoatCounter."),
  ]);
  return { hoy, uso, descargas, historial, web };
}

/** El último fin de semana que ya terminó: el lunes de esa semana y el de la anterior. */
export function ultimoFinDeSemana(hoy) {
  const semana = sumarDias(lunesDe(hoy), -7);
  return { semana, anterior: sumarDias(semana, -7) };
}

const formato = new Intl.NumberFormat("es-AR");
const numero = (x) => formato.format(x);
/** «23/11» para «2026-11-23». */
const corta = (dia) => `${dia.slice(8, 10)}/${dia.slice(5, 7)}`;

const COLOR_ORO = "#c98500";
const COLOR_AZUL = "#3987e5";

/**
 * Barras dibujadas en el servidor (sin scripts). Al pasar el mouse por un grupo
 * se ven sus números; los mismos datos están también en una tabla.
 */
export function barras(titulo, grupos, series) {
  if (grupos.length === 0) return "<p>Todavía no hay datos.</p>";
  const ancho = 720, alto = 200, izquierda = 44, abajo = 22, arriba = 10;
  const maximo = Math.max(1, ...grupos.flatMap((g) => g.valores));
  const paso = (ancho - izquierda) / grupos.length;
  const barra = Math.max(2, Math.min(24, (paso * 0.75 - 2 * (series.length - 1)) / series.length));
  const piso = alto - abajo;
  const cada = Math.ceil(grupos.length / 8);
  const guias = [...new Set([maximo, Math.round(maximo / 2), 0])].map((v) => {
    const y = piso - (v / maximo) * (piso - arriba);
    return `<line x1="${izquierda}" x2="${ancho}" y1="${y}" y2="${y}"/><text x="${izquierda - 6}" y="${y + 4}" text-anchor="end">${numero(v)}</text>`;
  }).join("");
  const dibujos = grupos.map((g, i) => {
    const centro = izquierda + paso * (i + 0.5);
    const total = barra * series.length + 2 * (series.length - 1);
    const columnas = g.valores.map((v, j) => {
      if (!v) return "";
      const h = Math.max(1, (v / maximo) * (piso - arriba));
      const x = centro - total / 2 + j * (barra + 2);
      const r = Math.min(4, barra / 2, h);
      return `<path fill="${series[j].color}" d="M${x} ${piso}v${r - h}q0 ${-r} ${r} ${-r}h${barra - 2 * r}q${r} 0 ${r} ${r}v${h - r}z"/>`;
    }).join("");
    const detalle = `${g.etiqueta} · ${g.valores.map((v, j) => `${series[j].nombre}: ${numero(v)}`).join(" · ")}`;
    const rotulo = (grupos.length - 1 - i) % cada === 0
      ? `<text x="${centro}" y="${alto - 6}" text-anchor="middle">${escapar(g.etiqueta)}</text>` : "";
    return `<g><title>${escapar(detalle)}</title><rect class="zona" x="${centro - paso / 2}" y="${arriba}" width="${paso}" height="${piso - arriba}"/>${columnas}${rotulo}</g>`;
  }).join("");
  const leyenda = series.length > 1
    ? `<p class="leyenda">${series.map((s) => `<span><i style="background:${s.color}"></i>${s.nombre}</span>`).join("")}</p>` : "";
  return `${leyenda}<svg viewBox="0 0 ${ancho} ${alto}" role="img" aria-label="${escapar(titulo)}">${guias}${dibujos}</svg>`;
}

/** Una tabla; todo lo que va en las celdas se escapa. */
function tabla(columnas, filas, vacia = "Todavía no hay datos.") {
  const cuerpo = filas.length
    ? filas.map((f) => `<tr>${f.map((c) => `<td>${escapar(typeof c === "number" ? numero(c) : c)}</td>`).join("")}</tr>`).join("")
    : `<tr><td colspan="${columnas.length}">${vacia}</td></tr>`;
  return `<div class="tabla"><table><tr>${columnas.map((c) => `<th>${c}</th>`).join("")}</tr>${cuerpo}</table></div>`;
}

const avisoDe = (texto) => `<p class="aviso">⚠ ${escapar(texto)}</p>`;
const desplegable = (titulo, contenido) => `<details><summary>${titulo}</summary>${contenido}</details>`;

function tarjeta(titulo, valor, nota) {
  return `<div class="tarjeta"><p>${titulo}</p><strong>${valor === null ? "—" : numero(valor)}</strong><small>${nota}</small></div>`;
}

function tarjetas({ hoy, uso, descargas, web }) {
  const { semana, anterior } = ultimoFinDeSemana(hoy);
  const dato = (lunes, campo) => (uso.datos ? uso.datos.semanas.find((s) => s.semana === lunes)?.[campo] ?? 0 : null);
  const previo = dato(anterior, "en_el_fin_de_semana");
  return `<div class="tarjetas">${
    tarjeta("Usaron el programa el fin de semana", dato(semana, "en_el_fin_de_semana"),
      `Instalaciones, ${corta(sumarDias(semana, 5))} y ${corta(sumarDias(semana, 6))}. El anterior: ${previo === null ? "—" : numero(previo)}.`)
  }${tarjeta("Instalaciones nuevas", dato(semana, "nuevas"), `Semana del ${corta(semana)}.`)
  }${tarjeta("Descargas del instalador", descargas.datos?.total ?? null, "Desde el principio, sin versiones de prueba.")
  }${tarjeta("Visitas a la web", web.datos?.visitas ?? null, `Últimos ${DIAS_WEB} días.`)}</div>`;
}

function seccionUso({ error, datos }) {
  if (error) return avisoDe(error);
  const semanas = datos.semanas.slice(0, 12).reverse();
  return `<p>Cada número es una instalación (una PC): una iglesia con dos PCs cuenta dos. La semana en curso está incompleta.</p>
<h3>Instalaciones que proyectaron algo, por semana</h3>${barras(
    "Instalaciones por semana",
    semanas.map((s) => ({ etiqueta: corta(s.semana), valores: [s.en_el_fin_de_semana, s.en_la_semana] })),
    [{ nombre: "Sábado o domingo", color: COLOR_ORO }, { nombre: "Toda la semana", color: COLOR_AZUL }],
  )}
${desplegable("Ver todas las semanas", tabla(["Semana (lunes)", "Sábado o domingo", "Toda la semana", "Nuevas"],
    datos.semanas.map((s) => [s.semana, s.en_el_fin_de_semana, s.en_la_semana, s.nuevas])))}
<h3>Por país</h3>${tabla(["País", "Últimas 4 semanas", "Desde el principio"],
    datos.paises.map((p) => [p.nombre, p.ultimas_4_semanas, p.instalaciones]))}
<h3>Versiones en uso (últimas 4 semanas)</h3>${tabla(["Versión", "Instalaciones"],
    datos.versiones_ultimas_4_semanas.map((v) => [v.version, v.instalaciones]))}
<h3>Iglesias que pusieron su nombre</h3>${tabla(["Iglesia", "País", "Último uso"],
    datos.iglesias.map((i) => [i.iglesia, i.nombre_pais, i.ultimo]), "Todavía ninguna puso su nombre.")}`;
}

function seccionHistorial({ error, datos }) {
  if (error) return avisoDe(error);
  if (!datos.desde)
    return "<p>Todavía no hay fotos guardadas. La primera la saca la tarea diaria (ver LEEME.md).</p>";
  if (datos.semanas.length === 0)
    return `<p>El historial empezó el ${corta(datos.desde)}: las descargas por semana aparecen con la foto del día siguiente.</p>`;
  return `${barras("Descargas nuevas por semana",
    datos.semanas.slice(-26).map((s) => ({ etiqueta: corta(s.semana), valores: [s.descargas] })),
    [{ nombre: "Descargas", color: COLOR_ORO }])}
<p>Contadas desde el ${corta(datos.desde)}. La primera semana y la actual pueden estar incompletas.</p>
${desplegable("Ver en tabla", tabla(["Semana (lunes)", "Descargas"], [...datos.semanas].reverse().map((s) => [s.semana, s.descargas])))}`;
}

function seccionDescargas({ error, datos }, historial) {
  const porVersion = error ? avisoDe(error) : `<h3>Por versión (instalador)</h3>${tabla(["Versión", "Publicada", "Descargas"],
    datos.instaladores.map((i) => [i.prueba ? `${i.version} (prueba)` : i.version, i.publicada, i.total]))}
${datos.otros.length ? `<h3>Otros archivos</h3>${tabla(["Publicación", "Archivo", "Descargas"],
    datos.otros.map((o) => [o.version, o.archivo, o.total]))}` : ""}`;
  return `<p>Incluyen las actualizaciones (el programa baja de GitHub el instalador nuevo): no son todas instalaciones nuevas.</p>
<h3>Descargas nuevas por semana</h3>${seccionHistorial(historial)}
${porVersion}`;
}

function seccionWeb({ error, datos }) {
  if (error) return avisoDe(error);
  return `<p>Del ${corta(datos.desde)} al ${corta(datos.hasta)}. GoatCounter cuenta visitantes, no recargas; quien abre dos páginas suma una en cada una.</p>
<h3>Visitas por día</h3>${barras("Visitas por día",
    datos.por_dia.map((d) => ({ etiqueta: corta(d.dia), valores: [d.visitas] })),
    [{ nombre: "Visitas", color: COLOR_AZUL }])}
${desplegable("Ver en tabla", tabla(["Día", "Visitas"], [...datos.por_dia].reverse().map((d) => [d.dia, d.visitas])))}
<h3>Botones</h3>${tabla(["Botón", "Veces"], datos.botones.map((b) => [b.nombre, b.veces]))}
<h3>Páginas más vistas</h3>${tabla(["Página", "Visitas"], datos.paginas.slice(0, 10).map((p) => [p.ruta, p.visitas]))}
<h3>De dónde llegan</h3>${tabla(["Origen", "Visitantes"], datos.de_donde.map((r) => [r.nombre, r.visitas]))}`;
}

export function paginaPanel(todo) {
  return `<!doctype html><html lang="es"><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1"><title>EcclesiaCast · panel</title>
<style>
body{font-family:system-ui,sans-serif;background:#08142A;color:#EAF0F8;margin:0 auto;padding:24px 16px;max-width:900px;line-height:1.5}
h1,h2,h3{font-weight:600}h2{color:#F4B942;margin-top:40px;border-bottom:1px solid #22355A;padding-bottom:6px}h3{margin:24px 0 8px;font-size:1rem}
p,small{color:#B4C2D8}
.tarjetas{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px}
.tarjeta{border:1px solid #22355A;border-radius:10px;padding:14px 16px;display:flex;flex-direction:column}
.tarjeta p{margin:0;font-size:.9rem}.tarjeta strong{font-size:2.2rem;font-weight:600}
svg{width:100%;height:auto;display:block}svg text{fill:#B4C2D8;font-size:11px}svg line{stroke:#22355A}
.zona{fill:transparent}g:hover .zona{fill:#ffffff12}
.leyenda span{margin-right:18px}.leyenda i{display:inline-block;width:10px;height:10px;border-radius:2px;margin-right:6px}
.tabla{overflow-x:auto}table{border-collapse:collapse;font-variant-numeric:tabular-nums}
td,th{padding:6px 12px;border-bottom:1px solid #22355A;text-align:right}th{color:#F4B942;font-weight:600}td:first-child,th:first-child{text-align:left}
summary{cursor:pointer;color:#B4C2D8;margin:8px 0}
.aviso{border:1px solid #F4B942;border-radius:8px;padding:10px 14px;color:#EAF0F8}
</style>
<h1>EcclesiaCast · panel</h1>
${tarjetas(todo)}
<h2>Uso del programa</h2>${seccionUso(todo.uso)}
<h2>Descargas (GitHub)</h2>${seccionDescargas(todo.descargas, todo.historial)}
<h2>Visitas a la web (GoatCounter)</h2>${seccionWeb(todo.web)}
<p><small>Las descargas pueden tener hasta una hora de atraso y las visitas hasta diez minutos.</small></p></html>`;
}

/** La clave escrita en la ventana de acceso del navegador (el usuario no importa). */
export function claveDelPedido(pedido) {
  const [tipo, dato] = (pedido.headers.get("authorization") ?? "").split(" ");
  if (tipo.toLowerCase() !== "basic" || !dato) return null;
  try {
    const texto = new TextDecoder().decode(Uint8Array.from(atob(dato), (c) => c.charCodeAt(0)));
    const dosPuntos = texto.indexOf(":");
    return dosPuntos < 0 ? null : texto.slice(dosPuntos + 1);
  } catch {
    return null;
  }
}

async function panel(pedido, env, ctx) {
  if (!env.RESUMEN_CLAVE) return new Response("No.", { status: 403 });
  if (claveDelPedido(pedido) !== env.RESUMEN_CLAVE)
    return new Response("Hace falta la clave.", {
      status: 401,
      headers: {
        "www-authenticate": 'Basic realm="Panel de EcclesiaCast", charset="UTF-8"',
        "content-type": "text/plain; charset=utf-8",
      },
    });
  return new Response(paginaPanel(await panelDatos(env, ctx)), {
    headers: {
      "content-type": "text/html; charset=utf-8",
      "cache-control": "no-store",
      "content-security-policy": "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'",
      "referrer-policy": "no-referrer",
      "x-robots-tag": "noindex",
    },
  });
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
      await env.DB.prepare("INSERT INTO uso (dia, instalacion, version, pais, iglesia) VALUES (?1, ?2, ?3, ?4, ?5) ON CONFLICT (dia, instalacion) DO UPDATE SET iglesia = excluded.iglesia WHERE excluded.iglesia <> ''")
        .bind(fila.dia, fila.instalacion, fila.version, paisDe(pedido), fila.iglesia)
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

    if ((url.pathname === "/panel" || url.pathname === "/panel/") && pedido.method === "GET")
      return panel(pedido, env, ctx);

    if (url.pathname === "/v1/fondos" && pedido.method === "GET")
      return buscarFondos(url, env, ctx);

    return new Response(null, { status: 404 });
  },

  // La tarea diaria (Cron Trigger): anota las descargas de hoy. Si GitHub
  // falla, el error queda en los registros de Cloudflare y se prueba mañana.
  async scheduled(evento, env) {
    const dia = diaDe(new Date(evento?.scheduledTime ?? Date.now()));
    await guardarDescargas(env.DB, dia, await pedirReleases(env));
  },
};
