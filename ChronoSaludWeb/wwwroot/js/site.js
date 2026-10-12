// JavaScript propio de ChronoSalud. La app funciona entera sin JavaScript:
// lo de acá solo suma comodidades, y si no carga todo sigue andando igual.

// ---------------------------------------------------------------------------
// Ver la contraseña mientras se escribe (el botón con el ojo).
// Recorre todos los campos de contraseña de la página (login, registro, Mi
// perfil, altas…) y le agrega a cada uno un botón al costado. Al tocarlo, el
// campo pasa a mostrar el texto; al tocarlo de nuevo, lo vuelve a ocultar.
// Así no hay que tocar cada vista: cualquier campo de contraseña nuevo lo
// tiene solo.
// ---------------------------------------------------------------------------
(function () {
    // Los íconos de ojo y ojo tachado (Lucide), igual que el resto de la app.
    const ojo = '<path d="M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0" /><circle cx="12" cy="12" r="3" />';
    const ojoTachado = '<path d="M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49" /><path d="M14.084 14.158a3 3 0 0 1-4.242-4.242" /><path d="M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143" /><path d="m2 2 20 20" />';

    function dibujar(svg, trazos) {
        svg.innerHTML = trazos;
    }

    document.querySelectorAll('input[type="password"]').forEach(function (campo) {
        // Un envoltorio para poder poner el botón encima del borde derecho del
        // campo. Se lleva el margen de arriba del campo, así el botón queda
        // justo a la altura del campo.
        const envoltorio = document.createElement('span');
        envoltorio.className = 'relative block';
        envoltorio.style.marginTop = getComputedStyle(campo).marginTop;
        campo.style.marginTop = '0';
        campo.parentNode.insertBefore(envoltorio, campo);
        envoltorio.appendChild(campo);

        // pr-11: el texto no queda escondido debajo del botón.
        campo.classList.add('block', 'pr-11');

        // El nombre no cambia: el lector de pantalla lee "Mostrar contraseña,
        // botón conmutador, presionado / no presionado" (aria-pressed).
        const boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'absolute inset-y-0 right-0 flex min-w-11 cursor-pointer items-center justify-center rounded-input text-muted hover:text-primary';
        boton.setAttribute('aria-label', 'Mostrar contraseña');
        boton.setAttribute('aria-pressed', 'false');
        if (campo.id) boton.setAttribute('aria-controls', campo.id);

        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('class', 'size-5');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '1.5');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        svg.setAttribute('aria-hidden', 'true');
        dibujar(svg, ojo);
        boton.appendChild(svg);
        envoltorio.appendChild(boton);

        boton.addEventListener('click', function () {
            const mostrar = campo.type === 'password';
            campo.type = mostrar ? 'text' : 'password';
            boton.setAttribute('aria-pressed', mostrar ? 'true' : 'false');
            dibujar(svg, mostrar ? ojoTachado : ojo);
        });

        // Al enviar se vuelve a ocultar, para que el navegador no la guarde
        // como un texto común en las sugerencias del formulario.
        if (campo.form) {
            campo.form.addEventListener('submit', function () {
                campo.type = 'password';
            });
        }
    });
})();

// ---------------------------------------------------------------------------
// Botón que muestra que se está enviando (paso 20).
// Solo los botones con data-cargando="Guardando…": al enviar el formulario el
// botón cambia su texto por ese, queda bloqueado para que un segundo clic no
// mande todo dos veces y, si la respuesta tarda más de 150 ms, muestra un
// latido que se dibuja como en un monitor. No se usa disabled: un botón
// deshabilitado no viaja en el formulario, y algunos llevan name y value.
// Si la validación del cliente frena el envío, el botón no cambia.
// ---------------------------------------------------------------------------
(function () {
    const latido = '<path d="M2 12h4l2.5-5 3.5 10 3-13 2.5 8H22" pathLength="1" />';

    function bloquear(boton) {
        // El ancho actual queda fijo para que el botón no salte al cambiar el texto.
        boton.style.minWidth = boton.offsetWidth + 'px';
        boton.dataset.textoOriginal = boton.innerHTML;
        boton.setAttribute('aria-disabled', 'true');
        boton.setAttribute('aria-busy', 'true');
        boton.textContent = boton.dataset.cargando;

        boton.dataset.temporizador = String(setTimeout(function () {
            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.setAttribute('class', 'latido-carga');
            svg.setAttribute('viewBox', '0 0 24 24');
            svg.setAttribute('fill', 'none');
            svg.setAttribute('stroke', 'currentColor');
            svg.setAttribute('stroke-width', '2');
            svg.setAttribute('stroke-linecap', 'round');
            svg.setAttribute('stroke-linejoin', 'round');
            svg.setAttribute('aria-hidden', 'true');
            svg.innerHTML = latido;
            boton.prepend(svg);
        }, 150));
    }

    function liberar(boton) {
        clearTimeout(Number(boton.dataset.temporizador));
        if (boton.dataset.textoOriginal !== undefined) boton.innerHTML = boton.dataset.textoOriginal;
        boton.removeAttribute('aria-disabled');
        boton.removeAttribute('aria-busy');
        boton.style.minWidth = '';
        delete boton.dataset.textoOriginal;
        delete boton.dataset.temporizador;
    }

    document.addEventListener('submit', function (evento) {
        const formulario = evento.target;

        // Ya se está enviando: el segundo clic no hace nada.
        if (formulario.dataset.enviando === 'true') {
            evento.preventDefault();
            return;
        }

        const boton = evento.submitter;
        if (!boton || !boton.matches('[data-cargando]')) return;

        // La validación de jQuery escucha el mismo evento y lo cancela si
        // falta algo; se mira después de que corrieron todos.
        setTimeout(function () {
            if (evento.defaultPrevented) return;
            formulario.dataset.enviando = 'true';
            bloquear(boton);
        }, 0);
    });

    // Al volver con el botón Atrás el navegador puede mostrar la página como
    // quedó (con el botón bloqueado): se la deja como nueva.
    window.addEventListener('pageshow', function (evento) {
        if (!evento.persisted) return;
        document.querySelectorAll('form[data-enviando]').forEach(function (formulario) {
            delete formulario.dataset.enviando;
        });
        document.querySelectorAll('[data-cargando][aria-busy]').forEach(liberar);
    });
})();

// ---------------------------------------------------------------------------
// Foco inicial solo con mouse (paso 20).
// En login y "olvidé mi contraseña" el email recibe el foco al cargar, pero
// solo en una computadora: en el celular abriría el teclado y taparía media
// pantalla. Si el servidor ya enfocó un campo con error, se respeta ese.
// ---------------------------------------------------------------------------
(function () {
    const campo = document.querySelector('[data-autofoco]');
    if (!campo || !window.matchMedia('(pointer: fine)').matches) return;
    if (document.activeElement && document.activeElement !== document.body) return;
    campo.focus();
})();

// ---------------------------------------------------------------------------
// Ctrl+Enter (⌘+Enter en Mac) envía el formulario desde un texto largo
// (paso 20). Enter solo sigue haciendo un renglón nuevo. Se envía con el
// botón principal del formulario, así también muestra que está enviando.
// ---------------------------------------------------------------------------
document.addEventListener('keydown', function (evento) {
    const campo = evento.target;
    if (evento.key !== 'Enter' || !(evento.ctrlKey || evento.metaKey)) return;
    if (!(campo instanceof HTMLTextAreaElement) || !campo.form) return;

    evento.preventDefault();
    const boton = campo.form.querySelector('button[type="submit"][data-cargando]') || undefined;
    campo.form.requestSubmit(boton);
});

// ---------------------------------------------------------------------------
// Aviso de cambios sin guardar (paso 20).
// En los formularios largos (data-avisar-cambios), si se escribió algo y se
// intenta salir sin guardar, el navegador pregunta antes de irse. Al enviar
// el formulario deja de preguntar.
// ---------------------------------------------------------------------------
(function () {
    const formularios = document.querySelectorAll('form[data-avisar-cambios]');
    if (formularios.length === 0) return;

    let hayCambios = false;
    formularios.forEach(function (formulario) {
        formulario.addEventListener('input', function () { hayCambios = true; });
        formulario.addEventListener('change', function () { hayCambios = true; });
        formulario.addEventListener('submit', function () { hayCambios = false; });
    });

    window.addEventListener('beforeunload', function (evento) {
        if (!hayCambios) return;
        evento.preventDefault();
        // Navegadores viejos piden returnValue además de preventDefault.
        evento.returnValue = '';
    });
})();

// ---------------------------------------------------------------------------
// Cerrar los avisos de después de guardar (paso 20).
// Los que tienen data-cerrable reciben un botón con una cruz. Sin JavaScript
// el aviso queda como siempre, y se va solo al cambiar de pantalla.
// ---------------------------------------------------------------------------
(function () {
    const cruz = '<path d="M18 6 6 18" /><path d="m6 6 12 12" />';

    document.querySelectorAll('[data-cerrable]').forEach(function (aviso) {
        const boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'area-tactil -my-1 -mr-2 flex size-7 shrink-0 cursor-pointer items-center justify-center rounded-badge opacity-80 hover:opacity-100';
        boton.setAttribute('aria-label', 'Cerrar aviso');

        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('class', 'size-4');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '1.5');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('aria-hidden', 'true');
        svg.innerHTML = cruz;
        boton.appendChild(svg);

        boton.addEventListener('click', function () {
            // El foco iba en el botón, que desaparece: pasa al contenido.
            const contenido = document.getElementById('contenido');
            aviso.remove();
            if (contenido) {
                contenido.setAttribute('tabindex', '-1');
                contenido.focus({ preventScroll: true });
            }
        });

        aviso.appendChild(boton);
    });
})();

// ---------------------------------------------------------------------------
// Tira de días (pedir y reprogramar un turno) (paso 20).
// En el celular la tira se desplaza de costado; al cargar, el día elegido
// queda a la vista, centrado, sin mover la página de arriba abajo.
// ---------------------------------------------------------------------------
document.querySelectorAll('[data-tira-dias]').forEach(function (tira) {
    const elegido = tira.querySelector('[aria-current="date"]');
    if (!elegido || tira.scrollWidth <= tira.clientWidth) return;
    tira.scrollLeft = elegido.offsetLeft - (tira.clientWidth - elegido.offsetWidth) / 2;
});
