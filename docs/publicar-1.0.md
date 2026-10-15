# Publicar la 1.0 — lista para el día

Todo lo de abajo ya está pensado. **No se hace nada de esto hasta que** la firma
esté resuelta **o se haya decidido publicar sin firma** (ver «Firma digital del instalador» en
[pendientes.md](pendientes.md)), la última beta se haya instalado y probado, y
Alexis haya usado el programa en un servicio real sin fallas graves.

## 1. CHANGELOG

El flujo de publicación toma de `CHANGELOG.md` la sección de la versión que se
publica (`## [1.0.0]`) y, si no la encuentra, la de `## [Sin publicar]`. Esa
sección es lo que ve la gente en el aviso de actualización, así que tiene que
estar en castellano y bien escrita.

1. Renombrar `## [Sin publicar]` a `## [1.0.0] - AAAA-MM-DD` (la fecha del día).
2. Arriba de esa línea, dejar un `## [Sin publicar]` **vacío** para lo que venga
   después.
3. Releer la sección que queda: hoy tiene todo lo que salió desde el 27/9/2026,
   mezclado con cosas que ya no existen o cambiaron. Conviene que la 1.0 cuente
   lo que **es** el programa, no la historia de las betas.

## 2. README

Cambiar el aviso del principio (hoy dice «Beta»):

```markdown
> ✅ **Versión 1.0.** Instalador para Windows en la
> [página de releases](https://github.com/EcclesiaCast/EcclesiaCast/releases).
> Está en uso en una iglesia real. Si algo falla,
> [contanos](https://github.com/EcclesiaCast/EcclesiaCast/issues).
```

Y en «Instalar y firma digital», comprobar que lo que dice sigue siendo verdad:
hoy dice que el instalador **no** está firmado. Si para entonces se consiguió
un certificado, cambiarlo.

## 3. Etiqueta

```bash
git checkout main && git pull
git tag -a v1.0.0 -m "1.0.0"
git push origin v1.0.0
```

El flujo (`.github/workflows/release.yml`) corre las pruebas, arma el programa,
pide la firma del programa (sólo si hay firma configurada; si no, se saltea), arma el
instalador, pide la firma del instalador (**otra
aprobación**) y publica. Como la etiqueta no lleva guion (`v1.0.0`, no
`v1.0.0-beta.16`), GitHub la publica como versión estable y no como pre-versión.

## 4. Después de publicar

- Bajar el instalador desde GitHub y comprobar la firma: clic derecho →
  Propiedades → Firmas digitales → «SignPath Foundation».
- Instalarlo **sobre** una beta anterior y comprobar que la actualización
  conserva canciones, Biblias y medios, y que el programa se vuelve a abrir solo.
- Con una beta anterior instalada, comprobar que ofrece actualizarse a la 1.0
  desde Ayuda → «Buscar actualizaciones…».
- Mover lo hecho de [pendientes.md](pendientes.md) a «Hecho» y borrar esta lista
  del repositorio o dejarla como historia.
