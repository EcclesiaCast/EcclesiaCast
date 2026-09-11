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

            #main, #secondary { margin: 0; white-space: pre-wrap; line-height: 1.15; }
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
          </style>
        </head>
        <body>
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

            var canvas = document.getElementById('canvas');
            var main = document.getElementById('main');
            var secondary = document.getElementById('secondary');
            var caption = document.getElementById('caption');
            var overlay = document.getElementById('overlay');
            var countdown = document.getElementById('countdown');
            var countdownHeading = document.getElementById('countdownHeading');
            var countdownClock = document.getElementById('countdownClock');

            function show(el, on) { el.classList.toggle('oculto', !on); }

            function paint(s) {
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

              main.textContent = s.showText ? (s.mainText || '') : '';
              show(secondary, s.showText && !!s.secondaryText);
              secondary.textContent = s.secondaryText || '';
              show(caption, s.showText && !!s.caption);
              caption.textContent = s.caption || '';
              caption.style.color = s.color || '#FFFFFF';

              show(countdown, !!s.countdown);
              countdownHeading.textContent = s.countdownHeading || '';
              show(countdownHeading, !!s.countdownHeading);
              countdownClock.textContent = s.countdown || '';

              show(overlay, !!s.overlay);
              overlay.textContent = s.overlay || '';

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
              fetch('/api/output?pin=' + encodeURIComponent(pin))
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

            poll();
          </script>
        </body>
        </html>
        """;
}
