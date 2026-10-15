// EcclesiaCast: el menú del celular, las diapositivas de la portada, las
// secciones que aparecen al bajar y el botón para copiar la huella.
(function () {
  'use strict';

  var quieto = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // Menú del celular
  var boton = document.querySelector('.menu-boton');
  var menu = document.getElementById('menu');
  if (boton && menu) {
    boton.addEventListener('click', function () {
      var abierto = menu.classList.toggle('abierto');
      boton.setAttribute('aria-expanded', abierto ? 'true' : 'false');
    });
    menu.addEventListener('click', function (e) {
      if (e.target.tagName === 'A') {
        menu.classList.remove('abierto');
        boton.setAttribute('aria-expanded', 'false');
      }
    });
  }

  // La pantalla de la portada: pasa sola, con los botones o con las flechas
  // del teclado, igual que en el programa.
  var proyeccion = document.querySelector('.proyeccion');
  if (proyeccion) {
    var diapos = proyeccion.querySelectorAll('.diapo');
    var titulo = proyeccion.querySelector('.diapo-titulo');
    var actual = 0;
    var reloj = null;
    var enVista = true;

    var mostrar = function (n) {
      diapos[actual].classList.remove('activa');
      actual = (n + diapos.length) % diapos.length;
      diapos[actual].classList.add('activa');
      titulo.textContent = diapos[actual].getAttribute('data-titulo');
      if (!quieto) {
        proyeccion.classList.add('brillo');
        setTimeout(function () { proyeccion.classList.remove('brillo'); }, 160);
      }
    };
    var parar = function () { clearInterval(reloj); reloj = null; };
    var seguir = function () {
      parar();
      if (!quieto && enVista && !document.hidden) {
        reloj = setInterval(function () { mostrar(actual + 1); }, 5500);
      }
    };

    proyeccion.querySelectorAll('[data-mover]').forEach(function (b) {
      b.addEventListener('click', function () {
        mostrar(actual + Number(b.getAttribute('data-mover')));
        seguir();
      });
    });
    proyeccion.addEventListener('mouseenter', parar);
    proyeccion.addEventListener('mouseleave', seguir);
    proyeccion.addEventListener('focusin', parar);
    proyeccion.addEventListener('focusout', seguir);
    document.addEventListener('visibilitychange', seguir);

    document.addEventListener('keydown', function (e) {
      if (!enVista || e.altKey || e.ctrlKey || e.metaKey) return;
      var t = e.target;
      if (t && (t.isContentEditable || /^(INPUT|TEXTAREA|SELECT|VIDEO)$/.test(t.tagName))) return;
      if (e.key === 'ArrowRight') { mostrar(actual + 1); seguir(); }
      else if (e.key === 'ArrowLeft') { mostrar(actual - 1); seguir(); }
    });

    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (entradas) {
        enVista = entradas[0].isIntersecting;
        seguir();
      }, { threshold: 0.35 }).observe(proyeccion);
    }
    seguir();
  }

  // Las secciones aparecen al llegar a ellas
  var revelar = document.querySelectorAll('.revelar');
  if (quieto || !('IntersectionObserver' in window)) {
    revelar.forEach(function (el) { el.classList.add('visible'); });
  } else {
    var observador = new IntersectionObserver(function (entradas) {
      entradas.forEach(function (en) {
        if (en.isIntersecting) {
          en.target.classList.add('visible');
          observador.unobserve(en.target);
        }
      });
    }, { rootMargin: '0px 0px -8% 0px', threshold: 0.12 });
    revelar.forEach(function (el) { observador.observe(el); });
  }

  // Copiar la huella
  document.querySelectorAll('[data-copiar]').forEach(function (b) {
    b.addEventListener('click', function () {
      var origen = document.querySelector(b.getAttribute('data-copiar'));
      if (!origen || !navigator.clipboard) return;
      navigator.clipboard.writeText(origen.textContent.trim()).then(function () {
        b.textContent = 'Copiada';
        setTimeout(function () { b.textContent = 'Copiar'; }, 2000);
      });
    });
  });
})();
