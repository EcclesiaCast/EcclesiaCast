# Política de privacidad

EcclesiaCast **no tiene cuentas y no manda nada tuyo a nadie**. Lo único que
cuenta por su cuenta es **cuántas instalaciones se usan**, de forma anónima y
con un interruptor para apagarlo (está en la tabla). Todo lo tuyo (canciones, Biblias, medios, playlists, temas y
ajustes) queda en tu PC, en `%APPDATA%\EcclesiaCast`, y el registro de errores
también (`%APPDATA%\EcclesiaCast\logs`).

El programa se conecta a internet **sólo cuando usás una función que lo
necesita** (y una vez por día: al abrirlo, para saber si hay una versión
nueva, y la primera vez que proyectás, para la cuenta anónima), y sólo con
los sitios de esta lista:

| Cuándo | Con quién | Qué se manda |
|---|---|---|
| Buscás una canción en internet | [musica.com](https://www.musica.com) (su buscador completo usa Google) y [lrclib.net](https://lrclib.net) | Lo que escribiste para buscar. |
| Buscás y bajás fondos de Pixabay («🎞 Fondos» en Medios) | Nosotros (`api.ecclesiacast.org`), que le pregunta a [Pixabay](https://pixabay.com/service/privacy/); los videos y fotos se bajan de Pixabay | Lo que escribiste para buscar. Nada tuyo: la búsqueda la hace nuestro servicio con su propia clave. |
| Agregás, mirás o descargás un video de YouTube | [YouTube](https://www.youtube.com) | El enlace del video. Si iniciaste sesión en YouTube dentro del programa, se usa esa sesión; sus datos se guardan sólo en tu PC. |
| Abrís una página web en el programa | La página que abrís (y Google, si escribís algo que no es una dirección) | Lo mismo que si la abrieras en un navegador. Si iniciás sesión en esa página (Google, Canva…), la sesión se guarda sólo en tu PC, la misma que usa YouTube. |
| Descargás de YouTube por primera vez, y después cada tres días al descargar | [GitHub](https://github.com) | Nada tuyo: baja o actualiza el descargador ([yt-dlp](https://github.com/yt-dlp/yt-dlp) y [deno](https://github.com/denoland/deno)). |
| La primera vez que proyectás algo en el día (se apaga en Ajustes → Actualizaciones) | Nosotros (`api.ecclesiacast.org`, en [Cloudflare](https://www.cloudflare.com/privacypolicy/)) | Un número al azar de esta instalación, el día y la versión del programa. Del lugar se anota sólo el país desde donde llega el aviso. Sirve para saber cuántas iglesias lo usan cada fin de semana y en qué países. No se guarda tu dirección IP, ni la ciudad, ni nada de lo que proyectás. Si querés, podés escribir en Ajustes el nombre de tu iglesia (es opcional y sólo informativo); si no lo escribís, no se manda. Si lo apagás, el número y el nombre se borran de tu PC. Las copias de prueba (una versión de prueba, o el programa sin instalar, corriendo desde una carpeta suelta) no mandan este aviso. |
| Al abrir el programa (una vez por día; se apaga en Ayuda) | [GitHub](https://github.com) | Nada tuyo: pregunta cuál es la última versión. Al actualizar, baja el instalador nuevo. |
| Hacés clic en un enlace (la guía de uso, informar un error) | El sitio del enlace, en tu navegador | Lo mismo que si lo abrieras a mano. |
| Instalás el programa y a la PC le falta WebView2 | [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/) | Nada tuyo: baja el componente de Microsoft que usa el programa para YouTube, para las páginas web y para buscar canciones. |
| Prendés «Seguir la canción» por primera vez y bajás el reconocedor de voz | [Hugging Face](https://huggingface.co) | Nada tuyo: baja el reconocedor de voz ([Whisper](https://github.com/ggml-org/whisper.cpp), unos 550 MB), una sola vez. |

**Seguir la canción** (en prueba) escucha la entrada de sonido que elijas
para saber por dónde va el canto. El reconocedor de voz corre en tu PC: el
sonido **no se graba, no se guarda y no se manda a ningún lado**. En el
registro del programa quedan las palabras que entendió y qué diapositiva
puso, para poder mejorarlo; ese registro no sale de tu PC salvo que vos lo
mandes.

**Proyectar otra ventana** (botón **🖥 Ventana**) tampoco sale a internet: usa
la captura de ventanas del propio Windows y dibuja la imagen dentro del
programa, en esta computadora. No se graba ni se guarda en disco, y no se manda
a ningún lado; mientras dura, Windows rodea la ventana elegida con un borde
amarillo (se ve sólo en la pantalla del operador). Lo que esa ventana muestre
se ve, tal cual, en la pantalla de la congregación: conviene cerrar antes lo
que no quieras mostrar (el correo, las notificaciones).

El **control remoto** y la **salida para OBS o vMix** no salen a internet:
abren una dirección dentro de tu red local, protegida con un PIN, y sólo
mientras los tengas prendidos.

Cada uno de esos sitios tiene su propia política de privacidad:
[Google y YouTube](https://policies.google.com/privacy),
[GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement),
[Microsoft](https://privacy.microsoft.com),
[musica.com](https://www.musica.com) y [lrclib.net](https://lrclib.net).
