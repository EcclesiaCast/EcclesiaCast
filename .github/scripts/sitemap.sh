#!/usr/bin/env bash
# Writes sitio/sitemap.xml with every page of the site and the date of its
# last change in git, so the dates never go stale. Run by
# .github/workflows/pages.yml before uploading sitio/ (needs the full git
# history: actions/checkout with fetch-depth: 0). 404.html is left out.
set -euo pipefail
cd "$(dirname "$0")/../.."

out=sitio/sitemap.xml
{
  echo '<?xml version="1.0" encoding="UTF-8"?>'
  echo '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
  for page in index ayuda cargar-canciones biblias control-desde-el-celular videos-y-powerpoint pantallas-extra privacidad; do
    file="sitio/$page.html"
    [ -f "$file" ] || continue
    date=$(git log -1 --format=%cs -- "$file")
    [ -n "$date" ] || date=$(date -u +%F)
    if [ "$page" = index ]; then url="https://ecclesiacast.org/"; else url="https://ecclesiacast.org/$page.html"; fi
    printf '  <url>\n    <loc>%s</loc>\n    <lastmod>%s</lastmod>\n  </url>\n' "$url" "$date"
  done
  echo '</urlset>'
} > "$out"
echo "Sitemap escrito: $(grep -c '<url>' "$out") páginas."
