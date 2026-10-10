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
