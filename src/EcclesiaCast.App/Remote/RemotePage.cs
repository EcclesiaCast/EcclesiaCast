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
          .slide img{width:100%;display:block;border-radius:4px;background:#0b0d12;
                     aspect-ratio:16/9;object-fit:contain}
          .shot{width:100%;max-height:38vh;object-fit:contain;display:none;border-radius:6px;background:#0b0d12}
          .item{text-align:left;padding:12px;font-size:14px;margin-bottom:6px;width:100%}
          .status{font-size:12px;color:#5b6070;padding:4px 2px 20px}
          .pin{max-width:320px;margin:60px auto;text-align:center}
          .pin input{font-size:32px;letter-spacing:.4em;text-align:center;width:100%;padding:14px;
                     background:#232630;border:1px solid #2a2e3a;border-radius:10px;color:#fff}
          .err{color:#e08a8a;font-size:14px;min-height:1.3em;margin-top:10px}
          .hide{display:none}
          .search input{width:100%;font:inherit;padding:12px 14px;background:#232630;border:1px solid #2a2e3a;
                        border-radius:10px;color:#fff}
          .hits{margin-top:8px;max-height:46vh;overflow-y:auto}
          .hit{display:block;text-align:left;padding:10px 12px;margin-bottom:6px;width:100%}
          .hit .h{font-size:14px;font-weight:600}
          .hit .d{font-size:12px;color:#9aa1b2;margin-top:2px}
          .none{font-size:13px;color:#5b6070;padding:6px 2px}
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
            <div class="card search">
              <input id="q" type="search" placeholder="Buscar canción o versículo…" autocomplete="off"
                     enterkeyhint="search" oninput="searchSoon()">
              <div class="hits" id="hits"></div>
            </div>

            <div class="card">
              <div class="label" id="slideLabel">En vivo</div>
              <div class="live" id="live"></div>
              <img class="shot" id="liveImg" alt="">
            </div>

            <div class="card">
              <div class="label">Sigue</div>
              <div class="next" id="next"></div>
              <img class="shot" id="nextImg" alt="">
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

            <div class="card hide" id="presCard">
              <div class="label">Presentaciones</div>
              <div id="presList"></div>
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

        // ── Buscar una canción o un versículo (como Ctrl+K en la computadora) ──
        // Se espera un momento después de la última letra, y una respuesta tardía
        // de una búsqueda vieja no pisa a la más nueva.
        var searchTimer = null;
        var searchSeq = 0;

        function searchSoon() {
          clearTimeout(searchTimer);
          var text = document.getElementById('q').value.trim();
          if (text.length < 2) { searchSeq++; showHits([], ''); return; }
          searchTimer = setTimeout(function () { runSearch(text); }, 350);
        }

        function runSearch(text) {
          var mine = ++searchSeq;
          fetch('/api/search?pin=' + encodeURIComponent(pin) + '&q=' + encodeURIComponent(text))
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (list) { if (list && mine === searchSeq) showHits(list, text); })
            .catch(function () {});
        }

        function showHits(list, text) {
          var box = document.getElementById('hits');
          box.innerHTML = '';
          if (!list.length && text) {
            var none = document.createElement('div');
            none.className = 'none';
            none.textContent = 'No encontré nada.';
            box.appendChild(none);
            return;
          }
          list.forEach(function (hit) {
            var b = document.createElement('button');
            b.className = 'hit';
            var h = document.createElement('div');
            h.className = 'h';
            h.textContent = (hit.kind === 'song' ? '♪  ' : '†  ') + hit.title;
            var d = document.createElement('div');
            d.className = 'd';
            d.textContent = hit.detail;
            b.appendChild(h);
            b.appendChild(d);
            b.onclick = function () { openHit(hit.index); };
            box.appendChild(b);
          });
        }

        // Carga la canción o el capítulo en la computadora: sus diapositivas
        // aparecen abajo y ahí se elige cuál proyectar.
        function openHit(index) {
          cmd('hit', index, function () {
            document.getElementById('q').value = '';
            searchSeq++;
            showHits([], '');
            document.getElementById('slides').scrollIntoView({behavior: 'smooth', block: 'start'});
          });
        }

        function cmd(action, index, done) {
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
            .then(function (s) { if (s) { render(s); if (done) done(); } })
            .catch(function () {})
            .then(function () { busy = false; });
        }

        function poll(first) {
          fetch('/api/state?pin=' + encodeURIComponent(pin)).then(function (r) {
            if (r.status === 429) {
              // Too many wrong PINs from this phone: the computer is making
              // it wait. Keep counting the wait down so nobody is left
              // wondering whether the app died.
              show(false);
              return r.json().then(function (e) {
                var left = Math.ceil(((e && e.retryAfter) || 0) / 60);
                document.getElementById('pinError').textContent =
                  'Demasiados intentos. Probá de nuevo en ' + (left > 1 ? left + ' minutos.' : 'un minuto.');
                return null;
              }).catch(function () { return null; });
            }
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

        function thumbUrl(id) {
          return '/api/thumb?pin=' + encodeURIComponent(pin) + '&id=' + id;
        }

        // The poster is only asked for when the picture changes: setting the
        // same address again would make the phone ask the computer every poll.
        function showShot(elementId, mediaId) {
          var img = document.getElementById(elementId);
          if (!mediaId) { img.style.display = 'none'; img.removeAttribute('data-id'); return; }
          if (img.getAttribute('data-id') !== String(mediaId)) {
            img.setAttribute('data-id', String(mediaId));
            img.src = thumbUrl(mediaId);
          }
          img.style.display = 'block';
        }

        var slidesKey = '';
        var presentationsKey = '';

        // The cards are rebuilt only when what they show changed; between
        // polls only the mark of the live one moves. Rebuilding every 0.7 s
        // would make the pages flicker and ask for their posters again.
        function renderSlides(list) {
          var slides = document.getElementById('slides');
          var key = pin + '\n' + list.map(function (x) { return x.index + '|' + x.label + '|' + x.preview + '|' + (x.mediaId || ''); }).join('\n');
          if (key !== slidesKey) {
            slidesKey = key;
            slides.innerHTML = '';
            list.forEach(function (slide) {
              var b = document.createElement('button');
              b.className = 'slide';
              var t = document.createElement('div');
              t.className = 't';
              t.textContent = slide.label;
              b.appendChild(t);
              if (slide.mediaId) {
                var img = document.createElement('img');
                img.alt = slide.label;
                img.src = thumbUrl(slide.mediaId);
                b.appendChild(img);
              } else {
                var p = document.createElement('div');
                p.className = 'p';
                p.textContent = slide.preview;
                b.appendChild(p);
              }
              b.onclick = function () { cmd('slide', slide.index); };
              slides.appendChild(b);
            });
          }
          for (var i = 0; i < list.length && i < slides.children.length; i++) {
            slides.children[i].className = 'slide' + (list[i].isLive ? ' live' : '');
          }
        }

        function renderPresentations(list) {
          document.getElementById('presCard').className = list.length ? 'card' : 'card hide';
          var key = list.map(function (x) { return x.index + '|' + x.name + '|' + x.pages; }).join('\n');
          if (key === presentationsKey) return;
          presentationsKey = key;
          var box = document.getElementById('presList');
          box.innerHTML = '';
          list.forEach(function (p) {
            var b = document.createElement('button');
            b.className = 'item';
            b.textContent = '📑  ' + p.name + ' (' + p.pages + ')';
            b.onclick = function () { cmd('presentation', p.index); };
            box.appendChild(b);
          });
        }

        function render(s) {
          document.getElementById('song').textContent = s.songTitle || 'EcclesiaCast';
          document.getElementById('live').textContent = s.liveText || '';
          document.getElementById('next').textContent = s.nextText || '—';
          document.getElementById('slideLabel').textContent = s.slideLabel || 'En vivo';
          document.getElementById('status').textContent = s.status || '';
          document.getElementById('dot').className = 'dot' + (s.isProjecting ? ' on' : '');
          document.getElementById('stateLabel').textContent = s.outputState === 'Content' ? 'En pantalla' : s.outputState;

          document.getElementById('bClear').className = s.outputState === 'Clear' ? 'on' : '';
          document.getElementById('bBlack').className = s.outputState === 'Black' ? 'on' : '';
          document.getElementById('bLogo').className = s.outputState === 'Logo' ? 'on' : '';
          document.getElementById('bOutput').className = s.isProjecting ? 'live' : '';

          // A page on the air (or the next one) shows as its poster, not as text.
          showShot('liveImg', s.liveMediaId);
          showShot('nextImg', s.nextMediaId);
          document.getElementById('live').style.display = s.liveMediaId ? 'none' : '';
          document.getElementById('next').style.display = s.nextMediaId ? 'none' : '';

          renderPresentations(s.presentations || []);
          renderSlides(s.slides || []);

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
