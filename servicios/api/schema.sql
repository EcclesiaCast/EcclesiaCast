-- Contador anónimo de uso de EcclesiaCast (base D1 de Cloudflare).
-- Una fila por instalación y por día en que proyectó algo. No hay nombres,
-- direcciones IP ni nada de lo que se proyecta. Del lugar, sólo el país.
CREATE TABLE IF NOT EXISTS uso (
  dia         TEXT NOT NULL,  -- «2026-11-15», el día como lo ve la PC de la iglesia
  instalacion TEXT NOT NULL,  -- 32 caracteres al azar, uno por instalación
  version     TEXT NOT NULL,  -- versión del programa
  pais        TEXT NOT NULL DEFAULT '',  -- «UY»: lo informa Cloudflare; vacío si no se sabe
  PRIMARY KEY (dia, instalacion)
);
