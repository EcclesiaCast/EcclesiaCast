# Guía de pruebas de la beta

Lista para probar EcclesiaCast en la PC del proyector, ordenada por lo que más
importa. Marcá lo que vaya saliendo bien y anotá lo que falle.

Si algo falla, anotá **qué estabas haciendo, qué esperabas y qué pasó**, y
guardá el archivo de registro más nuevo de `%APPDATA%\EcclesiaCast\logs`.

---

## 1. Lo que nunca probó nadie

Esto sólo se puede verificar en la PC del proyector. Es lo más importante.

### Instalación desde cero

- [ ] Descargar el instalador de la
      [página de releases](https://github.com/EcclesiaCast/EcclesiaCast/releases).
- [ ] Windows va a avisar que el programa no tiene firma digital ("Windows
      protegió tu PC"). Es esperable: hay que elegir *Más información* →
      *Ejecutar de todas formas*. **Anotá si el aviso asusta o confunde.**
- [ ] Aceptar el cartel de permisos (UAC) e instalar.
- [ ] Si esa PC no tiene WebView2, el instalador lo descarga e instala solo.
      Necesita internet. **Verificá que no se cuelgue ni tire error.**
- [ ] Queda el acceso directo con el ícono nuevo y la app abre.

### El proyector

- [ ] Conectar el proyector y abrir la app.
- [ ] En "Salida" aparece el proyector con su resolución real.
- [ ] "Poner en vivo" proyecta, y `Esc` apaga la salida.
- [ ] Si cambiás de pantalla en la lista, la salida se muda sin cerrarse.
- [ ] **El teclado no se lo roba la salida**: con algo proyectado, escribí en el
      buscador de canciones. Las letras tienen que ir al panel del operador.

### Rendimiento con video (lo que más me preocupa)

- [ ] Proyectar un video de fondo con la letra de una canción encima.
- [ ] Mirar si va **fluido o a tirones**.
- [ ] Abrir el Administrador de tareas y anotar cuánto **CPU y memoria** usa.

> En mi máquina daba medio núcleo de CPU y unos 810 MB de memoria, pero es una
> máquina de 16 núcleos. En una PC más modesta puede pesar bastante más. **Si va
> a tirones, avisame y bajo el video a 720p**, que es la solución prevista.

### YouTube

Esto sólo lo podés hacer vos, porque va con tu cuenta.

- [ ] Iniciar sesión con la cuenta Premium de la iglesia en el navegador
      integrado (botón "YouTube"). Se hace **una sola vez** y queda guardada.
- [ ] Proyectar un video y confirmar que **no salen anuncios**.
- [ ] **Apagar los subtítulos en la cuenta de YouTube**: Configuración →
      Subtítulos → desactivar "Mostrar siempre los subtítulos". Si no, se
      proyectan encima del video. Verificado que no se pueden apagar desde el
      programa: es una preferencia de la cuenta.
- [ ] Cerrar y volver a abrir EcclesiaCast: la sesión tiene que seguir puesta.

---

## 2. Cosas que ya verifiqué yo, pero conviene confirmar en tu PC

Todo esto lo probé proyectando en un segundo monitor y anda. Lo repito acá
porque tu hardware y tu proyector son otros.

- [ ] **Fondo sin diapositiva**: aplicá un video de fondo *sin* nada proyectado.
      Tiene que verse el video (esto estaba roto hasta anteayer).
- [ ] **Bucle**: dejá un video de fondo corriendo y mirá que dé la vuelta sin
      cortes ni parpadeo.
- [ ] **Miniaturas**: la primera vez que abras la app va a regenerar las
      miniaturas de los videos sola (tarda unos segundos). Verificá que se vean
      **fotogramas de los videos** y no el cono naranja de VLC.
- [ ] **Aviso al pie** sobre un video, y los botones Clear / Black / Logo (F1,
      F2, F3) con un video de fondo.

### Del Inspector de medios

Ya verifiqué que el Inspector abre y **guarda** (cambié la escala por la UI y
quedó en la base), y que cada valor hace lo suyo en la salida. Lo que queda es
confirmarlo con tu proyector y tus ojos:

- [ ] Clic derecho en un medio → *Propiedades (Inspector)*.
- [ ] Cambiar la **escala** (Rellenar / Ajustar / Estirar), guardar, aplicar el
      medio y ver que la salida cambia de verdad. Probá con un video que **no**
      sea 16:9 — los `01` a `08` tuyos son 3:2 y sirven.
- [ ] Cambiar **fin de video** a "Logo", proyectar un video corto y ver que al
      terminar pasa al logo.
- [ ] Cambiar **Fondo / Primer plano** y ver que Primer plano tapa la letra.
- [ ] Cambiar el **volumen** y el silencio de un video con audio (esto es lo
      único que no pude escuchar yo).

---

## 3. Ensayo general

Lo ideal: armar un servicio completo, como si fuera el domingo, y usarlo de
punta a punta.

- [ ] Armar una **playlist** con canciones, pasajes bíblicos y medios.
- [ ] Navegar toda la playlist con las **flechas ← →**, incluido el salto de un
      elemento al siguiente. Con un pasaje (ej. `Juan 3:16-18`): al entrar cae
      en el 16, al llegar al 18 la flecha sigue al próximo elemento, y volviendo
      desde el elemento siguiente aterriza en el 18. Si el pasaje es el último
      elemento, la flecha sigue de largo por el capítulo (a propósito).
- [ ] **Biblia**: tipear una referencia (ej. `Juan 3:16`) y proyectarla con
      Enter. Apretar Enter de nuevo tiene que dejar el mismo versículo (hubo un
      bug que saltaba al versículo 1; está corregido).
- [ ] Biblia con **dos versiones a la vez**.
- [ ] **Resaltado en vivo** sobre un versículo proyectado.
- [ ] Importar una canción desde `.txt` y desde un archivo de ProPresenter.
- [ ] Cambiar el **tema** de una canción y ver que se aplica al proyectar.
- [ ] Dejar el programa **abierto una o dos horas** con un video de fondo, para
      confirmar que no se cierra solo ni se pone lento.

---

## 4. Para decidir

- [ ] **El logo**: hoy el botón Logo (F3) proyecta el texto "EcclesiaCast", que
      está escrito a mano en el código. Debería poder ser el logo de tu iglesia.
      Falta definir si va una imagen de archivo, un texto configurable, o las
      dos, y desde dónde se elige.
