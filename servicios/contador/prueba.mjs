// Prueba del contador sin Cloudflare: la base D1 se imita con el SQLite que
// trae Node (node:sqlite). Uso: node servicios/contador/prueba.mjs
import { DatabaseSync } from "node:sqlite";
import { readFileSync } from "node:fs";
import assert from "node:assert/strict";
import contador, { validar } from "./src/contador.js";

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
const avisar = (cuerpo) => contador.fetch(new Request("https://contador.ecclesiacast.org/v1/uso",
  { method: "POST", body: JSON.stringify(cuerpo), headers: { "content-type": "application/json" } }), env);

// Lo que manda el programa se cuenta, una vez por día.
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0" })).status, 204);
assert.equal((await avisar({ instalacion: uno, dia: hoy, version: "1.4.0" })).status, 204);
assert.equal((await avisar({ instalacion: otro, dia: hoy, version: "1.4.0-prueba.2+d5929187" })).status, 204);
assert.equal(sqlite.prepare("SELECT COUNT(*) AS n FROM uso").get().n, 2);
assert.equal(sqlite.prepare("SELECT version FROM uso WHERE instalacion = ?").get(otro).version, "1.4.0-prueba.2");

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
const ver = (q) => contador.fetch(new Request("https://contador.ecclesiacast.org/v1/resumen" + q), env);
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
  sqlite.prepare("INSERT INTO uso VALUES (?, ?, '1.4.0')").run(dia, inst);
const datos = await (await ver("?clave=secreto&formato=json")).json();
assert.deepEqual(datos.semanas, [
  { semana: "2026-11-16", en_la_semana: 2, en_el_fin_de_semana: 1, nuevas: 0 },
  { semana: "2026-11-09", en_la_semana: 2, en_el_fin_de_semana: 2, nuevas: 2 },
]);
const html = await (await ver("?clave=secreto")).text();
assert.match(html, /Sábado o domingo/);

console.log("contador: todas las pruebas pasan");
