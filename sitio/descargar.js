// EcclesiaCast: en el celular el instalador de Windows no sirve, así que los
// botones de descargar abren un cartel para mandarse el enlace y bajarlo en
// la compu. Cada opción se cuenta aparte en GoatCounter, para separar las
// descargas reales («descargar») de los que se lo mandan para después.
(function () {
  'use strict';

  var ua = navigator.userAgent;
  var esCelular = /Android|iPhone|iPad|iPod/i.test(ua) ||
    (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  if (!esCelular || typeof HTMLDialogElement !== 'function') return;

  var botones = document.querySelectorAll('a[data-release="url"]');
  if (!botones.length) return;

  // ?ref=celular: cuando lo abra en la compu, el contador muestra que llegó así.
  var enlace = 'https://ecclesiacast.org/?ref=celular';
  var mensaje = 'EcclesiaCast, el programa gratis para proyectar en la iglesia. Bajalo en la compu (Windows): ' + enlace;

  function contar(ruta, titulo) {
    if (window.goatcounter && window.goatcounter.count)
      window.goatcounter.count({ path: ruta, title: titulo, event: true });
  }

  var cartel = document.createElement('dialog');
  cartel.className = 'cartel-celular';
  cartel.setAttribute('aria-labelledby', 'cartel-celular-titulo');
  cartel.innerHTML =
    '<form method="dialog" class="cartel-cerrar"><button aria-label="Cerrar">' +
      '<svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/></svg>' +
    '</button></form>' +
    '<h2 id="cartel-celular-titulo">Bajalo en la compu</h2>' +
    '<p>EcclesiaCast es un programa para Windows: en el celular no se instala. Mandate el enlace y abrilo en la compu de la iglesia.</p>' +
    '<a class="boton boton-grande cartel-whatsapp" target="_blank" rel="noopener" href="https://wa.me/?text=' + encodeURIComponent(mensaje) + '">' +
      '<svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path d="M12 3.5a8.5 8.5 0 0 0-7.3 12.8L3.5 20.5l4.3-1.1A8.5 8.5 0 1 0 12 3.5Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" fill="none"/></svg>' +
      'Mandar por WhatsApp</a>' +
    '<p class="cartel-nota">Para mandártelo a vos, buscá tu propio nombre en la lista.</p>' +
    '<div class="cartel-otras">' +
      (navigator.share
        ? '<button type="button" class="boton boton-hueco cartel-compartir">Otras opciones</button>'
        : '<a class="boton boton-hueco cartel-correo" href="mailto:?subject=' + encodeURIComponent('EcclesiaCast para la compu') + '&body=' + encodeURIComponent(mensaje) + '">Por correo</a>') +
      '<button type="button" class="boton boton-hueco cartel-copiar">Copiar enlace</button>' +
    '</div>' +
    '<input class="cartel-enlace" type="text" readonly value="' + enlace + '" aria-label="Enlace para abrir en la compu">';
  document.body.appendChild(cartel);

  cartel.querySelector('.cartel-whatsapp').addEventListener('click', function () {
    contar('mandar-whatsapp', 'Se lo mandó por WhatsApp');
  });
  var correo = cartel.querySelector('.cartel-correo');
  if (correo) correo.addEventListener('click', function () {
    contar('mandar-correo', 'Se lo mandó por correo');
  });
  var compartir = cartel.querySelector('.cartel-compartir');
  if (compartir) compartir.addEventListener('click', function () {
    navigator.share({ title: 'EcclesiaCast', text: mensaje.replace(' ' + enlace, ''), url: enlace })
      .then(function () { contar('mandar-compartir', 'Se lo mandó con Compartir'); })
      .catch(function () { /* cerró el menú sin elegir */ });
  });

  var copiar = cartel.querySelector('.cartel-copiar');
  var campo = cartel.querySelector('.cartel-enlace');
  copiar.addEventListener('click', function () {
    var listo = function () {
      copiar.textContent = 'Copiado';
      setTimeout(function () { copiar.textContent = 'Copiar enlace'; }, 2500);
      contar('mandar-copiar', 'Copió el enlace');
    };
    var aMano = function () {
      campo.focus();
      campo.select();
      try { if (document.execCommand('copy')) listo(); } catch (e) { /* queda seleccionado */ }
    };
    if (navigator.clipboard && navigator.clipboard.writeText)
      navigator.clipboard.writeText(enlace).then(listo, aMano);
    else aMano();
  });

  // Tocar fuera del cartel lo cierra.
  cartel.addEventListener('click', function (e) {
    if (e.target === cartel) cartel.close();
  });

  Array.prototype.forEach.call(botones, function (b) {
    b.setAttribute('data-goatcounter-click', 'mandar-a-la-compu');
    b.setAttribute('data-goatcounter-title', 'Abrió «Mandármelo a la compu» en el celular');
    if (b.classList.contains('boton-grande')) {
      // La flecha de bajar pasa a ser el avión de enviar.
      var dibujo = b.querySelector('svg');
      if (dibujo) dibujo.innerHTML = '<path d="M20.5 3.5 10 14M20.5 3.5 14 20.5l-4-6.5-6.5-4 17-6.5Z" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round" fill="none"/>';
      var nodos = b.childNodes;
      for (var i = nodos.length - 1; i >= 0; i--) {
        if (nodos[i].nodeType === 3 && nodos[i].textContent.trim()) {
          nodos[i].textContent = ' Mandármelo a la compu ';
          break;
        }
      }
    }
    b.addEventListener('click', function (e) {
      e.preventDefault();
      cartel.showModal();
    });
  });

  var datos = document.querySelector('.descarga-datos');
  if (datos) datos.textContent = 'Es para Windows 10 y 11: te mandás el enlace y lo bajás en la compu.';
})();
