namespace ChronoSaludWeb.Models;

/// <summary>
/// Trazos de los íconos, estilo Lucide: trazo 1.5, sin relleno. Viven acá y no
/// dentro de una vista porque los usan varios parciales (accesos rápidos,
/// métricas, botones de "ver más"); así el controlador sigue manejando solo una
/// clave y nunca marcado.
/// </summary>
public static class IconosLucide
{
    public static string Trazos(string icono) => icono switch
    {
        "campana"        => """<path d="M10.268 21a2 2 0 0 0 3.464 0" /><path d="M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326" />""",
        "estudio"        => """<path d="M21 7 6.82 21.18a2.83 2.83 0 0 1-3.99-.01a2.83 2.83 0 0 1 0-4L17 3" /><path d="m16 2 6 6" /><path d="M12 16H4" />""",
        "calendario-mas" => """<path d="M8 2v4" /><path d="M16 2v4" /><path d="M21 13V6a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h8" /><path d="M3 10h18" /><path d="M16 19h6" /><path d="M19 16v6" />""",
        "calendario"     => """<path d="M8 2v4" /><path d="M16 2v4" /><rect width="18" height="18" x="3" y="4" rx="2" /><path d="M3 10h18" /><path d="M8 14h.01" /><path d="M12 14h.01" /><path d="M16 14h.01" />""",
        "pastilla"       => """<path d="m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z" /><path d="m8.5 8.5 7 7" />""",
        "escudo"         => """<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" /><path d="m9 12 2 2 4-4" />""",
        "historia"       => """<path d="M15 2H9a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1" /><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" /><path d="M9 14h6" /><path d="M9 18h4" />""",
        "personas"       => """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />""",
        "persona-mas"    => """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M19 8v6" /><path d="M22 11h-6" />""",
        "lupa"           => """<circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" />""",
        "puntos"         => """<circle cx="12" cy="12" r="1" /><circle cx="19" cy="12" r="1" /><circle cx="5" cy="12" r="1" />""",
        "casa"           => """<path d="M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8" /><path d="M3 10a2 2 0 0 1 .709-1.528l7-5.999a2 2 0 0 1 2.582 0l7 5.999A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />""",
        "estetoscopio"   => """<path d="M4.8 2.3A.3.3 0 1 0 5 2H4a2 2 0 0 0-2 2v5a6 6 0 0 0 6 6 6 6 0 0 0 6-6V4a2 2 0 0 0-2-2h-1a.2.2 0 1 0 .3.3" /><path d="M8 15v1a6 6 0 0 0 6 6 6 6 0 0 0 6-6v-4" /><circle cx="20" cy="10" r="2" />""",
        "menu"           => """<path d="M4 12h16" /><path d="M4 18h16" /><path d="M4 6h16" />""",
        "ajustes"        => """<line x1="21" x2="14" y1="4" y2="4" /><line x1="10" x2="3" y1="4" y2="4" /><line x1="21" x2="12" y1="12" y2="12" /><line x1="8" x2="3" y1="12" y2="12" /><line x1="21" x2="16" y1="20" y2="20" /><line x1="12" x2="3" y1="20" y2="20" /><line x1="14" x2="14" y1="2" y2="6" /><line x1="8" x2="8" y1="10" y2="14" /><line x1="16" x2="16" y1="18" y2="22" />""",
        "salir"          => """<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><polyline points="16 17 21 12 16 7" /><line x1="21" x2="9" y1="12" y2="12" />""",
        "chevron-abajo"  => """<path d="m6 9 6 6 6-6" />""",
        "alerta"         => """<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3" /><path d="M12 9v4" /><path d="M12 17h.01" />""",
        "info"           => """<circle cx="12" cy="12" r="10" /><path d="M12 16v-4" /><path d="M12 8h.01" />""",
        "persona"        => """<circle cx="12" cy="8" r="5" /><path d="M20 21a8 8 0 0 0-16 0" />""",
        "reloj"          => """<circle cx="12" cy="12" r="10" /><polyline points="12 6 12 12 16 14" />""",
        "actividad"      => """<polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />""",
        "telefono"       => """<path d="M13.832 16.568a1 1 0 0 0 1.213-.303l.355-.465A2 2 0 0 1 17 15h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2A18 18 0 0 1 2 4a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-.8 1.6l-.468.351a1 1 0 0 0-.292 1.233 14 14 0 0 0 6.392 6.384" />""",
        "mensaje"        => """<path d="M7.9 20A9 9 0 1 0 4 16.1L2 22Z" />""",
        "ayuda"          => """<circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" /><path d="M12 17h.01" />""",
        "cerrar"         => """<path d="M18 6 6 18" /><path d="m6 6 12 12" />""",
        "imprimir"       => """<path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" /><path d="M6 9V3a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v6" /><rect x="6" y="14" width="12" height="8" rx="1" />""",

        // Navegación y acciones.
        "volver"         => """<path d="m12 19-7-7 7-7" /><path d="M19 12H5" />""",
        "flecha-der"     => """<path d="M5 12h14" /><path d="m12 5 7 7-7 7" />""",
        "flecha-arriba"  => """<path d="m5 12 7-7 7 7" /><path d="M12 19V5" />""",
        "flecha-abajo"   => """<path d="M12 5v14" /><path d="m19 12-7 7-7-7" />""",
        "chevron-izq"    => """<path d="m15 18-6-6 6-6" />""",
        "chevron-der"    => """<path d="m9 18 6-6-6-6" />""",
        "mas"            => """<path d="M5 12h14" /><path d="M12 5v14" />""",
        "menos"          => """<path d="M5 12h14" />""",
        "lapiz"          => """<path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z" /><path d="m15 5 4 4" />""",
        "enlace-externo" => """<path d="M15 3h6v6" /><path d="M10 14 21 3" /><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />""",
        "archivo-mas"    => """<path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z" /><path d="M14 2v4a2 2 0 0 0 2 2h4" /><path d="M9 15h6" /><path d="M12 18v-6" />""",
        "candado"        => """<rect width="18" height="11" x="3" y="11" rx="2" /><path d="M7 11V7a5 5 0 0 1 10 0v4" />""",

        // Estados y avisos.
        "check"          => """<path d="M20 6 9 17l-5-5" />""",
        "check-circulo"  => """<circle cx="12" cy="12" r="10" /><path d="m9 12 2 2 4-4" />""",
        "completado"     => """<path d="M21.801 10A10 10 0 1 1 17 3.335" /><path d="m9 11 3 3L22 4" />""",
        "x-circulo"      => """<circle cx="12" cy="12" r="10" /><path d="m15 9-6 6" /><path d="m9 9 6 6" />""",
        "alerta-circulo" => """<circle cx="12" cy="12" r="10" /><path d="M12 8v4" /><path d="M12 16h.01" />""",
        "usuario-x"      => """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="m17 8 5 5" /><path d="m22 8-5 5" />""",

        // Fechas.
        "calendario-x"     => """<path d="M8 2v4" /><path d="M16 2v4" /><rect width="18" height="18" x="3" y="4" rx="2" /><path d="M3 10h18" /><path d="m14 14-4 4" /><path d="m10 14 4 4" />""",
        "calendario-flecha" => """<path d="M8 2v4" /><path d="M16 2v4" /><rect width="18" height="18" x="3" y="4" rx="2" /><path d="M3 10h18" /><path d="m14 14 2 2-2 2" /><path d="M8 16h8" />""",

        // Datos de una persona.
        "email"          => """<rect width="20" height="16" x="2" y="4" rx="2" /><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />""",
        "documento"      => """<path d="M16 10h2" /><path d="M16 14h2" /><path d="M6.17 15a3 3 0 0 1 5.66 0" /><circle cx="9" cy="11" r="2" /><rect x="2" y="5" width="20" height="14" rx="2" />""",
        "ubicacion"      => """<path d="M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0" /><circle cx="12" cy="10" r="3" />""",
        "gota"           => """<path d="M12 22a7 7 0 0 0 7-7c0-2-1-3.9-3-5.5s-3.5-4-4-6.5c-.5 2.5-2 4.9-4 6.5C6 11.1 5 13 5 15a7 7 0 0 0 7 7z" />""",
        "nota"           => """<path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z" /><path d="M14 2v4a2 2 0 0 0 2 2h4" /><path d="M16 13H8" /><path d="M16 17H8" /><path d="M10 9H8" />""",
        "globo"          => """<circle cx="12" cy="12" r="10" /><path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20" /><path d="M2 12h20" />""",
        _              => """<circle cx="12" cy="12" r="10" />""",
    };
}
