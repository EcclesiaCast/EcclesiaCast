-- Contador anónimo de uso de EcclesiaCast (base D1 de Cloudflare).
-- Una fila por instalación y por día en que proyectó algo. No hay nombres,
-- direcciones IP ni nada de lo que se proyecta. Del lugar, sólo el país.
CREATE TABLE IF NOT EXISTS uso (
  dia         TEXT NOT NULL,  -- «2026-11-15», el día como lo ve la PC de la iglesia
  instalacion TEXT NOT NULL,  -- 32 caracteres al azar, uno por instalación
  version     TEXT NOT NULL,  -- versión del programa
  pais        TEXT NOT NULL DEFAULT '',  -- «UY»: lo informa Cloudflare; vacío si no se sabe
  iglesia     TEXT NOT NULL DEFAULT '',  -- nombre que la iglesia quiso poner (opcional); vacío si no
  PRIMARY KEY (dia, instalacion)
);

-- Descargas del instalador, para el panel: la tarea diaria anota el total que
-- GitHub acumula en cada versión. No tiene nada de las iglesias.
CREATE TABLE IF NOT EXISTS descargas (
  dia     TEXT NOT NULL,     -- «2026-11-26», el día de la foto (UTC)
  version TEXT NOT NULL,     -- etiqueta del release, «v1.5.0»
  total   INTEGER NOT NULL,  -- descargas acumuladas hasta ese momento
  PRIMARY KEY (dia, version)
);

-- Si la tabla «uso» ya existía (antes de la 1.4.0), agregale la columna una sola vez:
--   ALTER TABLE uso ADD COLUMN iglesia TEXT NOT NULL DEFAULT '';
