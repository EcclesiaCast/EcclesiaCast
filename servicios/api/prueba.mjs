// Prueba del contador sin Cloudflare: la base D1 se imita con el SQLite que
// trae Node (node:sqlite). Uso: node servicios/api/prueba.mjs
import { DatabaseSync } from "node:sqlite";
import { readFileSync } from "node:fs";
import assert from "node:assert/strict";
import contador, { validar, paisDe, nombreDePais, pedidoDeFondos, mejorArchivo, resumirPixabay, esVersionDePrueba,
  leerReleases, resumirWeb, descargasPorSemana, lunesDe, ultimoFinDeSemana, panelDatos, claveDelPedido } from "./worker.js";
import { RELEASES, HITS, REFERENCIAS, ejemplo } from "./ejemplos.mjs";

const sqlite = new DatabaseSync(":memory:");
sqlite.exec(readFileSync(new URL("./schema.sql", import.meta.url), "utf8"));

// Lo mínimo de la API de D1 que usa el contador.
const DB = {
  prepare(sql) {
    const st = sqlite.prepare(sql);
    let args = [];
    return {
      bind(...a) { args = a; return this; },
      async run() { return st.run(...args); },
      async all() { return { results: st.all(...args).map((r) => ({ ...r })) }; },
    };
  },
};
const env = { DB, RESUMEN_CLAVE: "secreto" };

const hoy = new Date().toISOString().slice(0, 10);
const uno = "0123456789abcdef0123456789abcdef";
const otro = "fedcba9876543210fedcba9876543210";
// Cloudflare le agrega «cf» a cada pedido, con el país.
const avisar = (cuerpo, pais) => contador.fetch(Object.assign(new Request("https://api.ecclesiacast.org/v1/uso",
  { method: "POST", body: JSON.stringify(cuerpo), headers: { "content-type": "application/json" } }),
  pais ? { cf: { country: pais } } : {}), env);

// Lo que manda el programa se cuenta, una vez por día.
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0" })).status, 204);
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0" })).status, 204);
assert.equal((await avisar({ instalacion: otro, dia: hoy, version: "1.4.0+d5929187" }, "UY")).status, 204);
assert.equal(sqlite.prepare("SELECT COUNT(*) AS n FROM uso").get().n, 2);
assert.equal(sqlite.prepare("SELECT version FROM uso WHERE instalacion = ?").get(otro).version, "1.4.0");

// Las versiones de prueba (llevan «-») son de quienes hacen el programa: no se guardan.
const tercero = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
for (const prueba of ["1.4.0-prueba.2+d5929187", "1.5.0-beta.1", "1.5.0-rc.1"])
  assert.equal((await avisar({ instalacion: tercero, dia: hoy, version: prueba }, "UY")).status, 400, prueba);
assert.equal(sqlite.prepare("SELECT COUNT(*) AS n FROM uso WHERE instalacion = ?").get(tercero).n, 0);
assert.equal(esVersionDePrueba("1.4.0-prueba.4"), true);
assert.equal(esVersionDePrueba("1.5.0+abc-def"), false); // el «-» de los datos de compilación no cuenta
assert.equal(esVersionDePrueba("1.5.0"), false);

// El nombre de la iglesia es opcional: sin nombre queda vacío; si lo ponen después el mismo día, se actualiza.
assert.equal(sqlite.prepare("SELECT iglesia FROM uso WHERE instalacion = ?").get(uno).iglesia, "");
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0", iglesia: "  Iglesia   del Centro " })).status, 204);
assert.equal(sqlite.prepare("SELECT iglesia FROM uso WHERE instalacion = ?").get(uno).iglesia, "Iglesia del Centro");
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0" })).status, 204);
assert.equal(sqlite.prepare("SELECT iglesia FROM uso WHERE instalacion = ?").get(uno).iglesia, "Iglesia del Centro");

// El resumen lista los nombres, sin dejar que un nombre rompa la página.
assert.equal((await avisar({ instalacion: otro, dia: hoy, version: "1.4.0", iglesia: "<b>Otra</b>" })).status, 204);
{
  const pagina = await (await contador.fetch(new Request("https://api.ecclesiacast.org/v1/resumen?clave=secreto"), env)).text();
  assert.ok(pagina.includes("Iglesia del Centro"));
  assert.ok(pagina.includes("&lt;b&gt;Otra&lt;/b&gt;"));
  assert.ok(!pagina.includes("<b>Otra</b>"));
}

// Del lugar se guarda sólo el país; sin dato queda vacío.
assert.equal(sqlite.prepare("SELECT pais FROM uso WHERE instalacion = ?").get(otro).pais, "UY");
assert.equal(sqlite.prepare("SELECT pais FROM uso WHERE instalacion = ?").get(uno).pais, "");
assert.equal(paisDe({ cf: { country: "ar" } }), "AR");
for (const raro of [{}, { cf: {} }, { cf: { country: "XX" } }, { cf: { country: "T1" } }, { cf: { country: "Uruguay" } }, undefined])
  assert.equal(paisDe(raro), "", JSON.stringify(raro));
assert.equal(nombreDePais("UY"), "Uruguay");
assert.equal(nombreDePais(""), "Sin dato");

// Lo que no cuadra no entra.
for (const malo of [
  { instalacion: "hola", dia: hoy, version: "1.4.0" },
  { instalacion: uno, dia: "2020-01-01", version: "1.4.0" },
  { instalacion: uno, dia: hoy, version: "<script>" },
  { instalacion: uno, dia: hoy },
]) assert.equal((await avisar(malo)).status, 400, JSON.stringify(malo));
assert.equal((await contador.fetch(new Request("https://x/v1/uso", { method: "POST", body: "no es json" }), env)).status, 400);

// El día de la PC puede ir un día adelante o atrás de UTC, no más.
const ahora = new Date("2026-11-15T02:30:00Z");
assert.ok(validar({ instalacion: uno, dia: "2026-11-14", version: "1.4.0" }, ahora));
assert.ok(validar({ instalacion: uno, dia: "2026-11-16", version: "1.4.0" }, ahora));
assert.equal(validar({ instalacion: uno, dia: "2026-11-10", version: "1.4.0" }, ahora), null);

// El resumen pide la clave.
const ver = (q) => contador.fetch(new Request("https://api.ecclesiacast.org/v1/resumen" + q), env);
assert.equal((await ver("")).status, 403);
assert.equal((await ver("?clave=otra")).status, 403);

// Semanas, fines de semana y nuevas: un sábado, un domingo y un martes.
sqlite.exec("DELETE FROM uso");
const filas = [
  ["2026-11-14", uno], ["2026-11-15", uno], ["2026-11-15", otro], // sábado y domingo
  ["2026-11-17", uno],                                              // martes siguiente
  ["2026-11-22", otro],                                             // domingo siguiente
];
for (const [dia, inst] of filas)
  sqlite.prepare("INSERT INTO uso (dia, instalacion, version, pais) VALUES (?, ?, '1.4.0', ?)").run(dia, inst, inst === uno ? "UY" : "MX");
const datos = await (await ver("?clave=secreto&formato=json")).json();
assert.deepEqual(datos.semanas, [
  { semana: "2026-11-16", en_la_semana: 2, en_el_fin_de_semana: 1, nuevas: 0 },
  { semana: "2026-11-09", en_la_semana: 2, en_el_fin_de_semana: 2, nuevas: 2 },
]);
// Por país: cada instalación cuenta una vez. Las fechas de la prueba son
// fijas, así que «últimas 4 semanas» depende del día en que se corre.
assert.deepEqual(datos.paises.map((p) => [p.pais, p.nombre, p.instalaciones]),
  [["MX", "México", 1], ["UY", "Uruguay", 1]]);
// Las filas viejas de versiones de prueba (de antes de rechazarlas) no entran
// en ninguna tabla: ni semanas, ni nuevas, ni versiones, ni países, ni iglesias.
{
  const vieja = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
  sqlite.prepare("INSERT INTO uso (dia, instalacion, version, pais, iglesia) VALUES (?, ?, '1.4.0-prueba.4', 'AR', 'Iglesia de prueba')")
    .run(hoy, vieja);
  sqlite.prepare("INSERT INTO uso (dia, instalacion, version, pais, iglesia) VALUES ('2026-11-15', ?, '1.4.0-prueba.4', 'AR', '')")
    .run(vieja);
  const sin = await (await ver("?clave=secreto&formato=json")).json();
  assert.deepEqual(sin.semanas.filter((s) => s.semana === "2026-11-09" || s.semana === "2026-11-16"), datos.semanas);
  assert.ok(sin.semanas.every((s) => s.semana === "2026-11-09" || s.semana === "2026-11-16"));
  assert.ok(!sin.paises.some((p) => p.pais === "AR"));
  assert.ok(!sin.iglesias.some((i) => i.iglesia === "Iglesia de prueba"));
  assert.ok(!sin.versiones_ultimas_4_semanas.some((v) => v.version.includes("-")));
  sqlite.prepare("DELETE FROM uso WHERE instalacion = ?").run(vieja);
}

const html = await (await ver("?clave=secreto")).text();
assert.match(html, /Sábado o domingo/);
assert.match(html, /Por país/);
assert.match(html, /Uruguay/);



// ── Fondos de Pixabay ──
const u = (q) => new URL("https://api.ecclesiacast.org/v1/fondos" + q);
assert.deepEqual(pedidoDeFondos(u("?q=%20Cruz%20%20atardecer%20")), { q: "cruz atardecer", tipo: "videos", pagina: 1 });
assert.equal(pedidoDeFondos(u("?q=a")), null);
assert.equal(pedidoDeFondos(u("?q=cruz&pagina=0")), null);
assert.equal(pedidoDeFondos(u("?q=cruz&tipo=fotos&pagina=3")).tipo, "fotos");

// De los tamaños de un video, el más grande que no pase de 1920 de ancho.
assert.deepEqual(mejorArchivo({
  large: { url: "4k", width: 3840, height: 2160, thumbnail: "t4" },
  medium: { url: "hd", width: 1920, height: 1080, thumbnail: "th" },
  small: { url: "720", width: 1280, height: 720, thumbnail: "t7" },
  tiny: { url: "", width: 0, height: 0, thumbnail: "" },
}), { url: "hd", ancho: 1920, alto: 1080, miniatura: "th" });
// Si sólo hay más grandes, el más chico de esos.
assert.equal(mejorArchivo({ large: { url: "4k", width: 3840, height: 2160 }, medium: { url: "2k", width: 2560, height: 1440 } }).url, "2k");
assert.equal(mejorArchivo({}), null);
assert.equal(mejorArchivo(undefined), null);

const videos = resumirPixabay("videos", { totalHits: 100, hits: [
  { id: 1, duration: 12, pageURL: "p", user: "Ana", user_id: 7,
    videos: { medium: { url: "hd", width: 1920, height: 1080, thumbnail: "m.jpg" } } },
  { id: 2, duration: 5, pageURL: "p2", user: "Beto", user_id: 8, videos: { medium: { url: "", width: 0, height: 0 } } },
]}, 1);
assert.equal(videos.resultados.length, 1);
assert.equal(videos.resultados[0].autor, "Ana");
assert.equal(videos.resultados[0].autorUrl, "https://pixabay.com/users/Ana-7/");
assert.equal(videos.hayMas, true);

const fotos = resumirPixabay("fotos", { totalHits: 10, hits: [
  { id: 3, imageWidth: 5000, imageHeight: 3000, pageURL: "p", user: "Cy", user_id: 9,
    webformatURL: "w.jpg", largeImageURL: "l.jpg" }] }, 1);
assert.equal(fotos.resultados[0].archivo.url, "l.jpg");
assert.equal(fotos.resultados[0].archivo.ancho, 1280);
assert.equal(fotos.hayMas, false);

// Sin clave de Pixabay, avisa en vez de romperse; y una búsqueda mala no llega a Pixabay.
assert.equal((await contador.fetch(new Request("https://api.ecclesiacast.org/v1/fondos?q=cruz"), { DB })).status, 503);
assert.equal((await contador.fetch(new Request("https://api.ecclesiacast.org/v1/fondos?q=x"), env)).status, 400);

// ── Panel ──
sqlite.prepare("INSERT INTO uso (dia, instalacion, version, pais, iglesia) VALUES (?, ?, '1.4.0', 'UY', 'Iglesia de ejemplo')").run("2026-11-22", "cccccccccccccccccccccccccccccccc");
// GitHub y GoatCounter se imitan con los datos de ejemplo de «ejemplos.mjs»:
// la prueba no sale a internet.
const pedidos = [];
let fallas = {}; // código de error que tiene que devolver cada servicio
globalThis.fetch = async (direccion, opciones = {}) => {
  const url = new URL(direccion);
  pedidos.push({ url, headers: opciones.headers ?? {} });
  const servicio = url.hostname === "api.github.com" ? "github" : "goatcounter";
  if (fallas[servicio]) return new Response("error", { status: fallas[servicio] });
  return Response.json(ejemplo(url));
};

const envPanel = { ...env, GOATCOUNTER_TOKEN: "clave-goat" };
const basic = (clave, usuario = "alexis") => "Basic " + Buffer.from(`${usuario}:${clave}`).toString("base64");
const verPanel = (autorizacion, entorno = envPanel, ruta = "/panel") => contador.fetch(new Request(
  "https://api.ecclesiacast.org" + ruta, { headers: autorizacion ? { authorization: autorizacion } : {} }), entorno);

// Sin clave (o con otra) no se ve nada ni se consulta a nadie; y la clave no vale en la dirección.
for (const [autorizacion, ruta] of [[null], [basic("otra")], ["Bearer secreto"], ["Basic %%%"], [null, "/panel?clave=secreto"]]) {
  const r = await verPanel(autorizacion, envPanel, ruta);
  assert.equal(r.status, 401, String(autorizacion));
  assert.match(r.headers.get("www-authenticate"), /^Basic realm=/);
}
assert.equal((await verPanel(basic("secreto"), { DB })).status, 403); // sin RESUMEN_CLAVE no hay panel
assert.equal(pedidos.length, 0);
// La clave puede tener tildes y «:».
assert.equal(claveDelPedido(new Request("https://x/panel", { headers: { authorization: basic("ñandú:2026") } })), "ñandú:2026");

// Releases: el instalador por versión (las pruebas no suman al total), otros archivos aparte, borradores afuera.
const leidos = leerReleases(RELEASES);
assert.equal(leidos.total, 1930);
assert.deepEqual(leidos.instaladores.map((i) => [i.version, i.total, i.prueba]),
  [["v1.5.0", 3, false], ["v1.4.0", 700, false], ["v1.3.1", 1227, false], ["v1.0.0-beta.16", 9, true]]);
assert.deepEqual(leidos.otros, [{ version: "fondos-extra-1", archivo: "EcclesiaCast-fondos-extra.zip", total: 43 }]);

// Web: los botones no suman visitas; los conocidos aparecen aunque estén en cero.
const web = resumirWeb(HITS.flat(), REFERENCIAS, "2026-11-24", "2026-11-26");
assert.equal(web.visitas, 150);
assert.deepEqual(web.por_dia, [{ dia: "2026-11-24", visitas: 40 }, { dia: "2026-11-25", visitas: 0 }, { dia: "2026-11-26", visitas: 110 }]);
assert.deepEqual(web.paginas.map((p) => p.ruta), ["/", "/ayuda.html"]);
assert.equal(web.botones.find((b) => b.ruta === "descargar").veces, 55);
assert.equal(web.botones.find((b) => b.ruta === "donar").veces, 0);
assert.equal(web.botones.at(-1).nombre, "boton-nuevo");
assert.equal(web.de_donde[1].nombre, "Directo o sin dato");

// Historial: la primera foto es el punto de partida; una versión nueva suma entera;
// un total que baja o una versión que falta en una foto no rompen la cuenta.
assert.equal(lunesDe("2026-11-22"), "2026-11-16");
assert.equal(lunesDe("2026-11-23"), "2026-11-23");
const foto = (dia, version, total) => ({ dia, version, total });
assert.deepEqual(descargasPorSemana([
  foto("2026-11-20", "v1.4.0", 500), foto("2026-11-20", "v1.3.1", 1200),
  foto("2026-11-22", "v1.4.0", 560), foto("2026-11-22", "v1.3.1", 1190),
  foto("2026-11-23", "v1.4.0", 570), foto("2026-11-23", "v1.5.0", 3),
  foto("2026-11-24", "v1.4.0", 575), foto("2026-11-24", "v1.3.1", 1205), foto("2026-11-24", "v1.5.0", 3),
]), { desde: "2026-11-20", semanas: [{ semana: "2026-11-16", descargas: 60 }, { semana: "2026-11-23", descargas: 33 }] });
assert.deepEqual(descargasPorSemana([]), { desde: null, semanas: [] });
assert.deepEqual(ultimoFinDeSemana("2026-11-26"), { semana: "2026-11-16", anterior: "2026-11-09" });
assert.deepEqual(ultimoFinDeSemana("2026-11-22"), { semana: "2026-11-09", anterior: "2026-11-02" }); // el domingo todavía no terminó

// La tarea diaria guarda una foto por día, sin las versiones de prueba; repetirla actualiza.
const tarea = (dia) => contador.scheduled({ scheduledTime: Date.parse(dia + "T23:55:00Z") }, envPanel);
await tarea("2026-11-25");
await tarea("2026-11-25");
assert.deepEqual(sqlite.prepare("SELECT version, total FROM descargas ORDER BY version").all().map((f) => ({ ...f })),
  [{ version: "v1.3.1", total: 1227 }, { version: "v1.4.0", total: 700 }, { version: "v1.5.0", total: 3 }]);
fallas = { github: 403 };
await assert.rejects(tarea("2026-11-26"), /GitHub rechazó/);
assert.equal(sqlite.prepare("SELECT COUNT(*) AS n FROM descargas").get().n, 3);
fallas = {};

// El panel junta todo. A GoatCounter se le pide por tandas y con la clave en el encabezado.
pedidos.length = 0;
const datosPanel = await panelDatos(envPanel, undefined, new Date("2026-11-26T15:00:00Z"));
assert.equal(datosPanel.descargas.datos.total, 1930);
assert.equal(datosPanel.web.datos.visitas, 150);
assert.equal(datosPanel.web.datos.desde, "2026-10-30");
assert.equal(datosPanel.historial.datos.desde, "2026-11-25");
const aGoat = pedidos.filter((p) => p.url.hostname === "ecclesiacast.goatcounter.com");
assert.deepEqual(aGoat.map((p) => p.url.pathname),
  ["/api/v0/stats/hits", "/api/v0/stats/hits", "/api/v0/stats/toprefs"]);
assert.equal(aGoat[0].url.searchParams.get("start"), "2026-10-30T00:00:00Z");
assert.equal(aGoat[0].url.searchParams.get("end"), "2026-11-26T23:00:00Z");
assert.equal(aGoat[1].url.searchParams.get("exclude_paths"), "1,2,3");
assert.equal(aGoat[0].headers.authorization, "Bearer clave-goat");
const aGitHub = pedidos.find((p) => p.url.hostname === "api.github.com");
assert.ok(aGitHub.headers["user-agent"]);
assert.equal(aGitHub.headers.authorization, undefined);
pedidos.length = 0;
await panelDatos({ ...envPanel, GITHUB_TOKEN: "clave-gh" }, undefined);
assert.equal(pedidos.find((p) => p.url.hostname === "api.github.com").headers.authorization, "Bearer clave-gh");

// La página: privada, sin scripts ni cookies, sin claves, y lo de afuera escapado.
{
  const r = await verPanel(basic("secreto"), { ...envPanel, GITHUB_TOKEN: "clave-gh" });
  assert.equal(r.status, 200);
  assert.equal(r.headers.get("cache-control"), "no-store");
  assert.equal(r.headers.get("set-cookie"), null);
  assert.match(r.headers.get("content-security-policy"), /default-src 'none'/);
  const pagina = await r.text();
  for (const texto of ["Uso del programa", "Descargas (GitHub)", "Visitas a la web", "1.930", "v1.0.0-beta.16 (prueba)",
    "Donar por PayPal", "Iglesia de ejemplo", "&lt;img src=x&gt;", "<svg"]) assert.ok(pagina.includes(texto), texto);
  for (const texto of ["<script", "<img", "clave-goat", "clave-gh", "secreto", "class=\"aviso\""])
    assert.ok(!pagina.includes(texto), texto);
}

// Si una fuente falla, esa parte avisa y el resto se ve igual.
fallas = { github: 403, goatcounter: 401 };
{
  const pagina = await (await verPanel(basic("secreto"))).text();
  assert.ok(pagina.includes("GitHub rechazó la consulta"));
  assert.ok(pagina.includes("GoatCounter no aceptó la clave"));
  assert.ok(pagina.includes("Iglesia de ejemplo"));
  assert.ok(pagina.includes("<strong>—</strong>"));
}
fallas = {};
assert.ok((await (await verPanel(basic("secreto"), env)).text()).includes("Falta la clave GOATCOUNTER_TOKEN"));
// Sin la tabla nueva (todavía no se ejecutó el SQL), el panel abre igual.
sqlite.exec("DROP TABLE descargas");
{
  const callado = console.error;
  console.error = () => {};
  const r = await verPanel(basic("secreto"));
  console.error = callado;
  assert.equal(r.status, 200);
  assert.ok((await r.text()).includes("Creaste la tabla"));
}

console.log("servicio: todas las pruebas pasan");
