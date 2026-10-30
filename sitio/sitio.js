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

    // La pantalla se inclina un poco hacia el puntero (sólo con mouse)
    if (!quieto && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
      var pantalla = proyeccion.querySelector('.pantalla');
      proyeccion.addEventListener('mousemove', function (e) {
        var r = proyeccion.getBoundingClientRect();
        var x = (e.clientX - r.left) / r.width - 0.5;
        var y = (e.clientY - r.top) / r.height - 0.5;
        pantalla.style.setProperty('--inclina-y', (x * 7).toFixed(2) + 'deg');
        pantalla.style.setProperty('--inclina-x', (-y * 5).toFixed(2) + 'deg');
      });
      proyeccion.addEventListener('mouseleave', function () {
        pantalla.style.removeProperty('--inclina-y');
        pantalla.style.removeProperty('--inclina-x');
      });
    }
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

  // Cifras: cuentan desde cero al llegar a la sección
  var cifras = document.querySelectorAll("[data-contar]");
  var contar = function (el) {
    var meta = Number(el.getAttribute("data-contar"));
    if (quieto || meta < 1) { el.textContent = meta; return; }
    var n = 0;
    el.textContent = 0;
    var paso = setInterval(function () {
      n += 1; el.textContent = n;
      if (n >= meta) clearInterval(paso);
    }, 260);
  };
  if (cifras.length) {
    if ("IntersectionObserver" in window) {
      var vigia = new IntersectionObserver(function (entradas) {
        entradas.forEach(function (en) {
          if (en.isIntersecting) { contar(en.target); vigia.unobserve(en.target); }
        });
      }, { threshold: 0.8 });
      cifras.forEach(function (el) { vigia.observe(el); });
    }
  }

  // Las capturas de las funciones se desplazan apenas más lento que la página
  if (!quieto && window.matchMedia("(min-width: 861px) and (hover: hover)").matches) {
    var capturas = Array.prototype.slice.call(document.querySelectorAll(".funcion-imagen img"));
    var esperando = false;
    var mover = function () {
      esperando = false;
      var centro = window.innerHeight / 2;
      capturas.forEach(function (img) {
        var r = img.parentNode.getBoundingClientRect();
        if (r.bottom < -200 || r.top > window.innerHeight + 200) return;
        var d = (r.top + r.height / 2 - centro) / window.innerHeight;
        img.style.transform = "translateY(" + (d * -34).toFixed(1) + "px)";
      });
    };
    window.addEventListener("scroll", function () {
      if (!esperando) { esperando = true; requestAnimationFrame(mover); }
    }, { passive: true });
    mover();
  }

  // La barra se afirma al bajar y marca en qué sección estás
  var barra = document.getElementById("barra");
  if (barra) {
    var bajar = function () { barra.classList.toggle("abajo", window.scrollY > 24); };
    window.addEventListener("scroll", bajar, { passive: true });
    bajar();
  }
  var enlaces = {};
  document.querySelectorAll(".menu a[href^=\"#\"]").forEach(function (a) { enlaces[a.getAttribute("href").slice(1)] = a; });
  if ("IntersectionObserver" in window) {
    var secciones = new IntersectionObserver(function (entradas) {
      entradas.forEach(function (en) {
        var a = enlaces[en.target.id];
        if (a) a.classList.toggle("actual", en.isIntersecting);
      });
    }, { rootMargin: "-45% 0px -50% 0px" });
    Object.keys(enlaces).forEach(function (id) {
      var s = document.getElementById(id);
      if (s) secciones.observe(s);
    });
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
