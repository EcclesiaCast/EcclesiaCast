namespace EcclesiaCast.App.Remote;

/// <summary>
/// The page the phone loads. One self-contained file — no CDN, no build step —
/// because it is served from the operator's laptop on a network that may well
/// have no internet at all. It polls the state twice a second and posts one
/// action at a time.
/// </summary>
internal static class RemotePage
{
    internal const string Html = """
        <!doctype html>
        <html lang="es">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
        <meta name="theme-color" content="#14161c">
        <title>EcclesiaCast</title>
        <style>
          *{box-sizing:border-box;-webkit-tap-highlight-color:transparent}
          body{margin:0;background:#14161c;color:#fff;font:16px/1.4 system-ui,-apple-system,"Segoe UI",sans-serif;
               padding:env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left)}
          header{padding:12px 14px;background:#1c1f27;border-bottom:1px solid #2a2e3a;
                 display:flex;align-items:center;gap:10px;position:sticky;top:0;z-index:5}
          header b{font-size:15px}
          .dot{width:10px;height:10px;border-radius:50%;background:#4a5266;flex:none}
          .dot.on{background:#c43b3b}
          main{padding:14px;max-width:720px;margin:0 auto}
          .card{background:#1c1f27;border:1px solid #2a2e3a;border-radius:10px;padding:14px;margin-bottom:12px}
          .label{font-size:11px;letter-spacing:.08em;color:#5b6070;text-transform:uppercase;margin-bottom:6px}
          .live{font-size:21px;font-weight:600;white-space:pre-wrap;word-break:break-word;min-height:1.4em}
          .next{font-size:15px;color:#9aa1b2;white-space:pre-wrap;word-break:break-word}
          .row{display:flex;gap:10px}
          .row>*{flex:1}
          button{font:inherit;color:#fff;background:#232630;border:1px solid #2a2e3a;border-radius:10px;
                 padding:16px 10px;cursor:pointer;touch-action:manipulation}
          button:active{background:#2f63c9}
          button.big{font-size:22px;font-weight:600;padding:26px 10px}
          button.on{background:#2f63c9;border-color:#2f63c9}
          button.live{background:#c43b3b;border-color:#c43b3b}
          .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(140px,1fr));gap:8px}
          .slide{text-align:left;padding:10px;font-size:13px;line-height:1.3}
          .slide .t{font-size:10px;color:#9aa1b2;margin-bottom:4px}
          .slide.live{background:#c43b3b;border-color:#c43b3b}
          .slide.live .t{color:#ffd9d9}
          .item{text-align:left;padding:12px;font-size:14px;margin-bottom:6px;width:100%}
          .status{font-size:12px;color:#5b6070;padding:4px 2px 20px}
          .pin{max-width:320px;margin:60px auto;text-align:center}
          .pin input{font-size:32px;letter-spacing:.4em;text-align:center;width:100%;padding:14px;
                     background:#232630;border:1px solid #2a2e3a;border-radius:10px;color:#fff}
          .err{color:#e08a8a;font-size:14px;min-height:1.3em;margin-top:10px}
          .hide{display:none}
        </style>
        </head>
        <body>

        <div class="pin" id="pinView">
          <h2>EcclesiaCast</h2>
          <p style="color:#9aa1b2">Escribí el PIN que muestra la computadora.</p>
          <input id="pinInput" inputmode="numeric" pattern="[0-9]*" maxlength="4" placeholder="····">
          <div class="err" id="pinError"></div>
          <button style="margin-top:14px;width:100%" onclick="savePin()">Entrar</button>
        </div>

        <div id="app" class="hide">
          <header>
            <span class="dot" id="dot"></span>
            <b id="song">EcclesiaCast</b>
            <span style="flex:1"></span>
            <span id="stateLabel" style="font-size:12px;color:#9aa1b2"></span>
          </header>

          <main>
            <div class="card">
              <div class="label" id="slideLabel">En vivo</div>
              <div class="live" id="live"></div>
            </div>

            <div class="card">
              <div class="label">Sigue</div>
              <div class="next" id="next"></div>
            </div>

            <div class="row" style="margin-bottom:12px">
              <button class="big" onclick="cmd('prev')">◀</button>
              <button class="big" onclick="cmd('next')">▶</button>
            </div>

            <div class="row" style="margin-bottom:12px">
              <button id="bClear" onclick="cmd('clear')">Clear</button>
              <button id="bBlack" onclick="cmd('black')">Black</button>
              <button id="bLogo" onclick="cmd('logo')">Logo</button>
            </div>

            <div class="row" style="margin-bottom:12px">
              <button onclick="cmd('nobackground')">Sin fondo</button>
              <button id="bOutput" onclick="cmd('output')">Salida</button>
            </div>

            <div class="card">
              <div class="label">Diapositivas</div>
              <div class="grid" id="slides"></div>
            </div>

            <div class="card">
              <div class="label">Playlist</div>
              <div id="playlist"></div>
            </div>

            <div class="status" id="status"></div>
          </main>
        </div>

        <script>
        var pin = localStorage.getItem('ec-pin') || '';
        var busy = false;

        function savePin() {
          var value = document.getElementById('pinInput').value.trim();
          if (value.length !== 4) { document.getElementById('pinError').textContent = 'Son 4 números.'; return; }
          pin = value;
          localStorage.setItem('ec-pin', pin);
          poll(true);
        }

        function show(ok) {
          document.getElementById('pinView').className = ok ? 'pin hide' : 'pin';
          document.getElementById('app').className = ok ? '' : 'hide';
        }

        function cmd(action, index) {
          if (busy) return;
          busy = true;
          // The phone can be a room away from the laptop; vibrate so the
          // person driving knows the tap registered without looking up.
          if (navigator.vibrate) navigator.vibrate(12);
          fetch('/api/cmd', {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({pin: pin, action: action, index: index === undefined ? null : index})
          }).then(function (r) { return r.ok ? r.json() : null; })
            .then(function (s) { if (s) render(s); })
            .catch(function () {})
            .then(function () { busy = false; });
        }

        function poll(first) {
          fetch('/api/state?pin=' + encodeURIComponent(pin)).then(function (r) {
            if (r.status === 403) {
              show(false);
              if (first) document.getElementById('pinError').textContent = 'PIN incorrecto.';
              return null;
            }
            return r.ok ? r.json() : null;
          }).then(function (s) {
            if (!s) return;
            show(true);
            render(s);
          }).catch(function () {
            document.getElementById('status').textContent = 'Sin conexión con la computadora…';
          });
        }

        function render(s) {
          document.getElementById('song').textContent = s.songTitle || 'EcclesiaCast';
          document.getElementById('live').textContent = s.liveText || '';
          document.getElementById('next').textContent = s.nextText || '—';
          document.getElementById('slideLabel').textContent = s.slideLabel || 'En vivo';
          document.getElementById('status').textContent = s.status || '';
          document.getElementById('dot').className = 'dot' + (s.isProjecting ? ' on' : '');
          document.getElementById('stateLabel').textContent = s.outputState;

          document.getElementById('bClear').className = s.outputState === 'Clear' ? 'on' : '';
          document.getElementById('bBlack').className = s.outputState === 'Black' ? 'on' : '';
          document.getElementById('bLogo').className = s.outputState === 'Logo' ? 'on' : '';
          document.getElementById('bOutput').className = s.isProjecting ? 'live' : '';

          var slides = document.getElementById('slides');
          slides.innerHTML = '';
          (s.slides || []).forEach(function (slide) {
            var b = document.createElement('button');
            b.className = 'slide' + (slide.isLive ? ' live' : '');
            b.innerHTML = '<div class="t"></div><div class="p"></div>';
            b.querySelector('.t').textContent = slide.label;
            b.querySelector('.p').textContent = slide.preview;
            b.onclick = function () { cmd('slide', slide.index); };
            slides.appendChild(b);
          });

          var list = document.getElementById('playlist');
          list.innerHTML = '';
          (s.playlist || []).forEach(function (item) {
            var b = document.createElement('button');
            b.className = 'item';
            b.textContent = item.kind + '  ' + item.caption;
            b.onclick = function () { cmd('playlist', item.index); };
            list.appendChild(b);
          });
        }

        if (pin) { poll(false); } else { show(false); }
        setInterval(function () { if (pin) poll(false); }, 700);
        document.getElementById('pinInput').addEventListener('keydown', function (e) {
          if (e.key === 'Enter') savePin();
        });
        </script>
        </body>
        </html>
        """;
}
