# Política de privacidad

EcclesiaCast **no tiene cuentas, no junta estadísticas y no manda datos de uso
a nadie**. Todo lo tuyo (canciones, Biblias, medios, playlists, temas y
ajustes) queda en tu PC, en `%APPDATA%\EcclesiaCast`, y el registro de errores
también (`%APPDATA%\EcclesiaCast\logs`).

El programa se conecta a internet **sólo cuando usás una función que lo
necesita**, y sólo con los sitios de esta lista:

| Cuándo | Con quién | Qué se manda |
|---|---|---|
| Buscás una canción en internet | [musica.com](https://www.musica.com) (su buscador completo usa Google) y [lrclib.net](https://lrclib.net) | Lo que escribiste para buscar. |
| Agregás, mirás o descargás un video de YouTube | [YouTube](https://www.youtube.com) | El enlace del video. Si iniciaste sesión en YouTube dentro del programa, se usa esa sesión; sus datos se guardan sólo en tu PC. |
| Descargás de YouTube por primera vez, y después cada tres días al descargar | [GitHub](https://github.com) | Nada tuyo: baja o actualiza el descargador ([yt-dlp](https://github.com/yt-dlp/yt-dlp) y [deno](https://github.com/denoland/deno)). |
| Proyectás una página web | El sitio que elegiste | Lo mismo que si la abrieras en un navegador. |
| Hacés clic en un enlace (la guía de uso, informar un error) | El sitio del enlace, en tu navegador | Lo mismo que si lo abrieras a mano. |
| Instalás el programa y a la PC le falta WebView2 | [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/) | Nada tuyo: baja el componente de Microsoft que muestra YouTube y las páginas web. |

El **control remoto** y la **salida para OBS o vMix** no salen a internet:
abren una dirección dentro de tu red local, protegida con un PIN, y sólo
mientras los tengas prendidos.

Cada uno de esos sitios tiene su propia política de privacidad:
[Google y YouTube](https://policies.google.com/privacy),
[GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement),
[Microsoft](https://privacy.microsoft.com),
[musica.com](https://www.musica.com) y [lrclib.net](https://lrclib.net).

Si algún día el programa empieza a conectarse por su cuenta (por ejemplo, para
avisar que hay una versión nueva), va a figurar en esta lista y se va a poder
apagar.
