namespace EcclesiaCast.App.Remote;

/// <summary>
/// The page a streaming program points at: the words that are on the
/// projector, drawn over nothing at all.
///
/// Transparent on purpose. OBS's Browser Source and vMix's Web Input both
/// keep the alpha channel, so the church gets its lyrics over the camera
/// instead of a photograph of the projector screen, and the operator needs
/// no capture card and no second computer.
/// </summary>
public static class OutputPage
{
    public const string Html = """
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <title>EcclesiaCast — Salida para transmisión</title>
          <style>
            /* Transparent background: the streaming program composites this
               over the camera. ?fondo=negro paints it black for anyone whose
               setup cannot keep the alpha channel. */
            html, body {
              margin: 0; height: 100%; background: transparent; overflow: hidden;
              font-family: 'Segoe UI', system-ui, sans-serif;
            }
            body.opaco { background: #000; }

            #canvas {
              position: absolute; inset: 0;
              display: flex; flex-direction: column;
              padding: 4vh 6vw; box-sizing: border-box;
            }
            #canvas.arriba { justify-content: flex-start; }
            #canvas.centro { justify-content: center; }
            #canvas.abajo  { justify-content: flex-end; }

            /* The picture or video of the announcements page, behind the words
               (the words ride over it, as on the projector). */
            #media {
              position: absolute; inset: 0; width: 100%; height: 100%;
              background: transparent;
            }

            #main, #secondary { margin: 0; white-space: pre-wrap; line-height: 1.15; }
            /* The marker pen, the same yellow as on the projector. */
            mark { background: #E8C34A; color: #000; text-shadow: none; }
            #secondary { opacity: .92; font-size: .62em; margin-top: .4em; }
            /* Lives outside #canvas, so it gets its own colour: inheriting
               from the body meant black text on a black stream. */
            #caption {
              position: absolute; right: 6vw; bottom: 4vh;
              font-size: 2.2vh; opacity: .85; color: #fff;
              text-shadow: 0 0 1vh rgba(0,0,0,.9);
            }

            /* The lower third rides over everything, exactly as it does on
               the projector. */
            #overlay {
              position: absolute; left: 0; right: 0; bottom: 0;
              padding: 2.2vh 5vw; background: rgba(0,0,0,.82);
              border-top: .5vh solid #2F63C9; color: #fff;
              font-size: 3.4vh; text-align: center;
            }

            #countdown {
              position: absolute; inset: 0;
              display: flex; flex-direction: column;
              align-items: center; justify-content: center;
              color: #fff; text-align: center;
            }
            #countdownHeading { font-size: 5vh; font-weight: 300; opacity: .9; }
            #countdownClock { font-size: 18vh; font-weight: 600; line-height: 1; }

            .oculto { display: none !important; }

            /* ── Own design of a layer (Transmisión → Estilos…) ──────────────
               With a style the page draws #plate instead of #canvas: the words
               sit top, centre or bottom with a safety margin, over the plate
               the operator chose. Sizes are em of #stage, so the whole plate
               scales with the font size. */
            body.estilo #canvas, body.estilo #caption { display: none !important; }
            body:not(.estilo) #stage, body:not(.estilo) #sbg, body:not(.estilo) #sgrad { display: none !important; }

            #sbg { position: absolute; inset: 0; background: transparent; overflow: hidden; }
            #sbg img, #sbg video { width: 100%; height: 100%; object-fit: cover; display: block; }

            #sgrad { position: absolute; left: 0; right: 0; opacity: 0; transition: opacity .2s; pointer-events: none; }

            #stage {
              position: absolute; inset: 0; box-sizing: border-box;
              display: flex; flex-direction: column; align-items: center;
              pointer-events: none;
            }
            #stage.p-top { justify-content: flex-start; }
            #stage.p-center { justify-content: center; }
            #stage.p-bottom { justify-content: flex-end; }
            #stage.k-line { align-items: flex-start; }

            #swrap { position: relative; width: max-content; opacity: 0; display: flex; flex-direction: column; }
            #plate {
              position: relative; box-sizing: border-box;
              padding: .4em .85em; text-align: center; line-height: 1.18;
            }
            #stage.k-line #plate { text-align: left; }
            #smain, #ssecondary { margin: 0; white-space: pre-wrap; overflow-wrap: break-word; }
            #ssecondary { font-size: .62em; opacity: .92; margin-top: .35em; }

            /* The small tag over the plate: «CORO», or the reference of a verse. */
            #label {
              width: fit-content; margin: 0 auto .18em;
              font-size: .36em; font-weight: 700; letter-spacing: .16em; line-height: 1;
              text-transform: uppercase; white-space: nowrap;
              color: var(--accent); text-shadow: 0 .1em .4em rgba(0,0,0,.85), 0 0 .15em rgba(0,0,0,.7);
            }
            #stage.k-line #label { margin-left: 0; }

            /* Pestaña: dark rounded plate, a thick accent line under it, the
               label as an accent tab centred on top. */
            #stage.k-tab #plate {
              background: var(--plate); border: 1px solid rgba(255,255,255,.13);
              border-bottom: .12em solid var(--accent); border-radius: .3em;
              padding: .5em 1em .42em; box-shadow: 0 .3em 1em rgba(0,0,0,.38);
            }
            #stage.k-tab #label {
              margin: 0 auto -1px; padding: .42em 1.5em .4em;
              background: var(--accent); color: var(--onaccent); text-shadow: none;
              border-radius: .5em .5em 0 0; position: relative; z-index: 1;
            }

            /* Vidrio: translucent plate that blurs the camera behind it. */
            #stage.k-glass #plate {
              background: linear-gradient(rgba(255,255,255,.1), rgba(255,255,255,.03)), var(--plateglass);
              -webkit-backdrop-filter: blur(1.8vh) saturate(1.35); backdrop-filter: blur(1.8vh) saturate(1.35);
              border: 1px solid rgba(255,255,255,.3); border-radius: .34em;
              box-shadow: inset 0 1px 0 rgba(255,255,255,.2), 0 .3em 1em rgba(0,0,0,.25);
              padding: .5em 1em; text-shadow: 0 .04em .12em rgba(0,0,0,.45);
            }
            #stage.k-glass #label {
              margin-bottom: .45em; padding: .45em 1.2em; border-radius: 2em; color: #fff;
              background: var(--plateglass); border: 1px solid rgba(255,255,255,.3);
              -webkit-backdrop-filter: blur(1.8vh); backdrop-filter: blur(1.8vh); text-shadow: none;
            }

            /* Sólida: no plate, one strong even shadow to read over any camera. */
            #stage.k-solid #plate, #stage.k-line #plate {
              text-shadow: 0 .04em .06em rgba(0,0,0,.95), 0 0 .2em rgba(0,0,0,.85), 0 0 .5em rgba(0,0,0,.6);
            }

            /* Degradé: no box, the colour fades in from the edge (#sgrad). */
            #stage.k-gradient #plate { text-shadow: 0 .04em .14em rgba(0,0,0,.55); }

            /* Halo: a soft dark glow behind the letters. */
            #stage.k-halo #plate {
              text-shadow: 0 0 .22em var(--halo), 0 0 .5em var(--halo), 0 0 1em var(--halo), 0 0 1.6em var(--halo);
            }

            /* Filete: soft shadow and a thin accent line that fades out sideways. */
            #stage.k-fillet #plate { text-shadow: 0 .04em .12em rgba(0,0,0,.8), 0 0 .5em rgba(0,0,0,.45); padding-bottom: .5em; }
            #stage.k-fillet #plate::after {
              content: ''; position: absolute; left: -4%; right: -4%; bottom: 0; height: max(2px, .04em);
              background: linear-gradient(90deg, transparent, var(--accent) 18%, var(--accent) 82%, transparent);
            }

            /* Línea: like Sólida, with a vertical accent line to the left. */
            #stage.k-line #plate { border-left: .1em solid var(--accent); padding: .1em .7em .1em .6em; }

            /* Ninguna: the thin outline the projector has always had. */
            #stage.k-none #plate {
              text-shadow: -.035em -.035em 0 #000, .035em -.035em 0 #000, -.035em .035em 0 #000, .035em .035em 0 #000,
                           0 -.05em 0 #000, 0 .05em 0 #000, -.05em 0 0 #000, .05em 0 0 #000;
            }
            #stage.k-none #label { text-shadow: -.05em -.05em 0 #000, .05em -.05em 0 #000, -.05em .05em 0 #000, .05em .05em 0 #000; }

            /* Where the label sits (Estilos → Etiqueta): left, centre or right
               of the plate, over it or under it. It comes last so it wins over
               the margins each plate gives the label. */
            #stage.l-left #label { margin-left: 0; margin-right: auto; }
            #stage.l-center #label { margin-left: auto; margin-right: auto; }
            #stage.l-right #label { margin-left: auto; margin-right: 0; }
            #stage.v-below #label { order: 2; margin-top: .18em; margin-bottom: 0; }
            #stage.v-below.k-tab #label { margin-top: -1px; border-radius: 0 0 .5em .5em; }
            #stage.v-below.k-glass #label { margin-top: .45em; }
          </style>
        </head>
        <body>
          <div id="sbg"></div>
          <div id="mediaBox"></div>
          <div id="sgrad"></div>
          <div id="stage" class="p-bottom k-tab">
            <div id="swrap">
              <div id="label" class="oculto"></div>
              <div id="plate">
                <div id="smain"></div>
                <div id="ssecondary" class="oculto"></div>
              </div>
            </div>
          </div>
          <div id="canvas" class="centro">
            <div id="main"></div>
            <div id="secondary" class="oculto"></div>
          </div>
          <div id="caption" class="oculto"></div>
          <div id="countdown" class="oculto">
            <div id="countdownHeading"></div>
            <div id="countdownClock"></div>
          </div>
          <div id="overlay" class="oculto"></div>

          <script>
            var params = new URLSearchParams(location.search);
            var pin = params.get('pin') || '';
            if (params.get('fondo') === 'negro') document.body.classList.add('opaco');

            // The page has one address per channel: /salida (everything),
            // /salida/biblia, /salida/canciones and /salida/anuncios. The server
            // does the choosing; the page only says which one it is.
            var canal = (location.pathname.replace(/\/+$/, '').split('/')[2] || '').toLowerCase();
            var sound = params.get('sonido') === '1';

            var canvas = document.getElementById('canvas');
            var mediaBox = document.getElementById('mediaBox');
            var mediaKey = '';
            var main = document.getElementById('main');
            var secondary = document.getElementById('secondary');
            var caption = document.getElementById('caption');
            var overlay = document.getElementById('overlay');
            var countdown = document.getElementById('countdown');
            var countdownHeading = document.getElementById('countdownHeading');
            var countdownClock = document.getElementById('countdownClock');

            function show(el, on) { el.classList.toggle('oculto', !on); }

            // Writes the text with the highlighted stretches ([start, length]
            // pairs, in order) wrapped in <mark>.
            function setText(el, text, marks) {
              el.textContent = '';
              var at = 0;
              (marks || []).forEach(function (m) {
                if (m[0] > at) el.appendChild(document.createTextNode(text.substring(at, m[0])));
                var mark = document.createElement('mark');
                mark.textContent = text.substring(m[0], m[0] + m[1]);
                el.appendChild(mark);
                at = m[0] + m[1];
              });
              if (at < text.length) el.appendChild(document.createTextNode(text.substring(at)));
            }

            // Draws the picture or video that is alone on the screen, and takes it
            // away when it goes. The element is only rebuilt when the media
            // changes, or the video would start over at every poll. The video is
            // muted: the projector already plays its sound, and a second copy of
            // it in the stream would be an echo (add «&sonido=1» to hear it).
            function paintMedia(media) {
              var key = media ? media.kind + ':' + media.id + ':' + media.fit + ':' + media.loop : '';
              if (key === mediaKey) return;
              mediaKey = key;
              mediaBox.textContent = '';
              if (!media) return;

              var el = document.createElement(media.kind === 'video' ? 'video' : 'img');
              el.id = 'media';
              el.src = '/api/media?pin=' + encodeURIComponent(pin) + '&id=' + media.id;
              el.style.objectFit = media.fit || 'cover';
              if (media.kind === 'video') {
                el.autoplay = true;
                el.loop = !!media.loop;
                el.muted = !sound;
                el.playsInline = true;
                el.addEventListener('canplay', function () { el.play().catch(function () {}); });
              }
              mediaBox.appendChild(el);
            }

            // Draws what the computer sent (also called by the style editor's
            // preview). With a style the layer has its own design; without one
            // it is drawn as the projector draws it.
            function paint(s) {
              paintMedia(s.media);
              document.body.classList.toggle('estilo', !!s.style);
              paintBackground(s);
              if (s.style) paintStyled(s); else paintLegacy(s);

              show(countdown, !!s.countdown);
              countdownHeading.textContent = s.countdownHeading || '';
              show(countdownHeading, !!s.countdownHeading);
              countdownClock.textContent = s.countdown || '';

              show(overlay, !!s.overlay);
              overlay.textContent = s.overlay || '';
            }

            var stage = document.getElementById('stage');
            var swrap = document.getElementById('swrap');
            var plate = document.getElementById('plate');
            var smain = document.getElementById('smain');
            var ssecondary = document.getElementById('ssecondary');
            var label = document.getElementById('label');
            var sgrad = document.getElementById('sgrad');
            var sbg = document.getElementById('sbg');
            var bgKey = '';
            var plateKey = '';

            function rgba(hex, a) {
              var h = String(hex || '').replace('#', '');
              if (h.length !== 6 || isNaN(parseInt(h, 16))) h = '000000';
              var n = parseInt(h, 16);
              return 'rgba(' + (n >> 16 & 255) + ',' + (n >> 8 & 255) + ',' + (n & 255) + ',' + a + ')';
            }

            // Dark text over a light accent, light over a dark one: the tab
            // stays readable whatever colour the operator picks.
            function onColor(hex) {
              var h = String(hex || '').replace('#', '');
              var n = parseInt(h.length === 6 ? h : '000000', 16);
              var l = 0.299 * (n >> 16 & 255) + 0.587 * (n >> 8 & 255) + 0.114 * (n & 255);
              return l > 150 ? '#0A1114' : '#FFFFFF';
            }

            // The fixed picture, video or colour behind everything. It is there
            // while the page is on, and leaves with Black, Clear and Logo like
            // everything else. Rebuilt only when it changes, or the video would
            // start over at every poll.
            function paintBackground(s) {
              var st = s.style;
              var key = '';
              if (st && !s.blank && st.background === 'color') key = 'c:' + st.backgroundColor;
              else if (st && !s.blank && st.background === 'file' && st.backgroundFile)
                key = 'f:' + st.backgroundFile + ':' + (st.backgroundUrl || canal);
              if (key === bgKey) return;
              bgKey = key;
              sbg.textContent = '';
              sbg.style.background = 'transparent';
              if (!key) return;
              if (key.charAt(0) === 'c') { sbg.style.background = st.backgroundColor; return; }

              var video = /\.(mp4|m4v|webm|mov)$/i.test(st.backgroundFile);
              var el = document.createElement(video ? 'video' : 'img');
              el.src = st.backgroundUrl || '/api/fondo?pin=' + encodeURIComponent(pin) + '&canal=' + encodeURIComponent(canal);
              if (video) {
                el.autoplay = true; el.loop = true; el.muted = true; el.playsInline = true;
                el.addEventListener('canplay', function () { el.play().catch(function () {}); });
              }
              sbg.appendChild(el);
            }

            // The layer with its own design: the words inside #plate, set top,
            // centre or bottom with the safety margin.
            function paintStyled(s) {
              var st = s.style;
              var text = s.showText ? (s.mainText || '') : '';
              var tag = st.showLabel ? (s.label || '') : '';
              var kind = st.plate || 'tab';
              var pos = st.position || 'bottom';
              var op = (st.plateOpacity == null ? 85 : st.plateOpacity) / 100;
              var base = st.fontSize / 1080 * 100;

              // Nothing changed: leave it alone, or the fade would replay at every poll.
              var key = JSON.stringify([st, text, s.secondaryText, tag, s.mainMarks, s.secondaryMarks, !!s.overlay]);
              if (key === plateKey) return;
              plateKey = key;

              if (!text) {
                swrap.style.transition = 'opacity .2s';
                swrap.style.opacity = '0';
                sgrad.style.opacity = '0';
                return;
              }

              // A short fade between one text and the next instead of a jump.
              swrap.style.transition = 'none';
              swrap.style.opacity = '0';
              void swrap.offsetWidth;

              var m = st.marginPercent || 0;
              stage.className = 'p-' + pos + ' k-' + kind + ' l-' + (st.labelAlign || 'center') + (st.labelBelow ? ' v-below' : '');
              label.style.fontSize = (0.36 * (st.labelSize || 100) / 100).toFixed(3) + 'em';
              stage.style.padding = m + 'vh ' + m + 'vw ' + (s.overlay && pos === 'bottom' ? Math.max(m, 14) : m) + 'vh';
              stage.style.setProperty('--accent', st.accentColor);
              stage.style.setProperty('--onaccent', onColor(st.accentColor));
              stage.style.setProperty('--plate', rgba(st.plateColor, op));
              stage.style.setProperty('--plateglass', rgba(st.plateColor, Math.min(op, 1) * 0.55));
              stage.style.setProperty('--halo', rgba(st.plateColor, Math.max(op, 0.6) * 0.95));
              stage.style.color = st.textColor;
              stage.style.fontFamily = "'" + (st.fontFamily || 'Segoe UI') + "', 'Segoe UI', sans-serif";
              stage.style.fontWeight = st.bold ? '700' : '400';
              stage.style.textTransform = st.uppercase ? 'uppercase' : 'none';
              swrap.style.maxWidth = 'min(' + st.widthPercent + 'vw, 100%)';

              setText(smain, text, s.mainMarks);
              show(ssecondary, !!s.secondaryText);
              setText(ssecondary, s.secondaryText || '', s.secondaryMarks);
              show(label, !!tag);
              label.textContent = tag;

              // The words shrink in steps of 5 % until the plate fits in the
              // top half of the screen, as the projector does.
              var scale = 1;
              stage.style.fontSize = base.toFixed(2) + 'vh';
              for (var i = 0; i < 20 && swrap.offsetHeight > window.innerHeight * 0.5 && scale > 0.3; i++) {
                scale -= 0.05;
                stage.style.fontSize = (base * scale).toFixed(2) + 'vh';
              }

              // The gradient: the third of the screen on the side of the position.
              if (kind === 'gradient') {
                var c = rgba(st.plateColor, op), c0 = rgba(st.plateColor, 0);
                sgrad.style.top = pos === 'top' ? '0' : pos === 'center' ? '32%' : 'auto';
                sgrad.style.bottom = pos === 'bottom' ? '0' : 'auto';
                sgrad.style.height = pos === 'center' ? '36%' : '38%';
                sgrad.style.background = pos === 'top' ? 'linear-gradient(to bottom,' + c + ' 0%,' + rgba(st.plateColor, op * 0.55) + ' 55%,' + c0 + ' 100%)'
                  : pos === 'center' ? 'linear-gradient(to bottom,' + c0 + ' 0%,' + c + ' 50%,' + c0 + ' 100%)'
                  : 'linear-gradient(to top,' + c + ' 0%,' + rgba(st.plateColor, op * 0.55) + ' 55%,' + c0 + ' 100%)';
                sgrad.style.opacity = '1';
              } else {
                sgrad.style.opacity = '0';
              }

              void swrap.offsetWidth;
              swrap.style.transition = 'opacity .2s';
              swrap.style.opacity = '1';
            }

            function paintLegacy(s) {
              // Sizes come over as points on the 1080-high canvas the
              // projector renders to, so they are turned into viewport
              // units and the stream can be any resolution.
              var size = (s.fontSize / 1080 * 100).toFixed(2) + 'vh';
              canvas.style.fontSize = size;
              canvas.style.color = s.color || '#FFFFFF';
              canvas.style.fontFamily = "'" + (s.fontFamily || 'Segoe UI') + "', 'Segoe UI', sans-serif";
              canvas.style.fontWeight = s.bold ? '600' : '400';
              canvas.style.fontStyle = s.italic ? 'italic' : 'normal';
              canvas.style.textAlign = s.alignH === 'left' ? 'left' : s.alignH === 'right' ? 'right' : 'center';
              canvas.className = s.alignV === 'top' ? 'arriba' : s.alignV === 'bottom' ? 'abajo' : 'centro';

              // The halo that keeps white text readable over a bright camera.
              var w = (s.outlineWidth || 0) / 1080 * 100;
              canvas.style.textShadow = w > 0
                ? [[-1,-1],[1,-1],[-1,1],[1,1],[0,-1.4],[0,1.4],[-1.4,0],[1.4,0]]
                    .map(function (d) {
                      return (d[0]*w).toFixed(2) + 'vh ' + (d[1]*w).toFixed(2) + 'vh 0 ' + (s.outlineColor || '#000');
                    }).join(',')
                : '0 0 1.4vh rgba(0,0,0,.85)';

              setText(main, s.showText ? (s.mainText || '') : '', s.showText ? s.mainMarks : []);
              show(secondary, s.showText && !!s.secondaryText);
              setText(secondary, s.secondaryText || '', s.secondaryMarks);
              show(caption, s.showText && !!s.caption);
              caption.textContent = s.caption || '';
              caption.style.color = s.color || '#FFFFFF';

              // The lower third owns the bottom of the screen while it is up,
              // so the caption steps out of its way instead of under it.
              caption.style.bottom = s.overlay ? '13vh' : '4vh';
            }

            // Se pregunta tres veces por segundo mientras todo va bien. Si la
            // dirección quedó con un PIN viejo, insistir a ese ritmo hace que
            // la computadora bloquee a este equipo por golpear la puerta: ante
            // un rechazo se espera cada vez más, hasta diez segundos.
            var espera = 300;

            function poll() {
              fetch('/api/output?pin=' + encodeURIComponent(pin) + '&canal=' + encodeURIComponent(canal))
                .then(function (r) {
                  if (r.status === 403 || r.status === 429) {
                    espera = Math.min(espera * 2, 10000);
                    return null;
                  }
                  espera = 300;
                  return r.ok ? r.json() : null;
                })
                .then(function (s) { if (s) paint(s); })
                .catch(function () { /* la computadora se fue: dejamos lo último en pantalla */ })
                .then(function () { setTimeout(poll, espera); });
            }

            // The style editor's preview (WebView2) calls paint() itself and has
            // no server to ask.
            if (!(window.chrome && window.chrome.webview)) poll();
          </script>
        </body>
        </html>
        """;
}
