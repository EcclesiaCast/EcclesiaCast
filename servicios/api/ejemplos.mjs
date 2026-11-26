// Datos de ejemplo (inventados) de GitHub y GoatCounter, con la forma en que
// contestan. Los usa prueba.mjs; no hacen falta para publicar el servicio.

const archivo = (name, download_count) => ({ name, download_count });
const release = (tag_name, dia, assets, extra = {}) => ({ tag_name, published_at: `${dia}T12:00:00Z`, draft: false, assets, ...extra });

export const RELEASES = [
  release("v1.5.0", "2026-11-25", [archivo("EcclesiaCast-1.5.0-setup.exe", 3)]),
  release("v1.4.0", "2026-11-17", [archivo("EcclesiaCast-1.4.0-setup.exe", 700)]),
  release("v1.3.1", "2026-11-09", [archivo("EcclesiaCast-1.3.1-setup.exe", 1227)]),
  release("fondos-extra-1", "2026-10-20", [archivo("EcclesiaCast-fondos-extra.zip", 43)], { prerelease: true }),
  release("v1.0.0-beta.16", "2026-10-10", [archivo("EcclesiaCast-1.0.0-beta.16-setup.exe", 9)], { prerelease: true }),
  release("v9.9.9", "2026-11-26", [archivo("EcclesiaCast-9.9.9-setup.exe", 999)], { draft: true }),
];

const dias = (...pares) => pares.map(([day, daily]) => ({ day, daily }));
const hit = (path_id, path, count, stats = [], event = false) => ({ path_id, path, count, stats, event });

// En dos tandas, como cuando hay más direcciones de las que entran en un pedido.
export const HITS = [
  [
    hit(1, "/", 120, dias(["2026-11-24", 30], ["2026-11-26", 90])),
    hit(2, "/ayuda.html", 30, dias(["2026-11-24", 10], ["2026-11-26", 20])),
    hit(3, "descargar", 55, dias(["2026-11-26", 55]), true),
  ],
  [
    hit(4, "mandar-a-la-compu", 12, [], true),
    hit(5, "boton-nuevo", 2, [], true),
  ],
];

export const REFERENCIAS = [
  { name: "instagram.com", count: 80 },
  { name: "", count: 40 },
  { name: "<img src=x>", count: 1 },
];

/** Lo que contestaría cada dirección. */
export function ejemplo(url) {
  if (url.hostname === "api.github.com") return RELEASES;
  if (url.pathname.endsWith("/stats/toprefs")) return { stats: REFERENCIAS, more: false };
  const segunda = url.searchParams.has("exclude_paths");
  return { hits: HITS[segunda ? 1 : 0], more: !segunda };
}
