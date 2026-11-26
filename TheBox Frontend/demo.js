/*
 * Modo demo para GitHub Pages: responde los endpoints con datos simulados.
 * En localhost no se activa: el portal sigue usando la API ASP.NET real.
 */
(function () {
    'use strict';

    const forzado = new URLSearchParams(location.search).has('demo');
    if (forzado) {
        try { sessionStorage.setItem('thebox-demo', '1'); } catch (e) { /* se ignora */ }
    }

    let activadoPorSesion = false;
    try { activadoPorSesion = sessionStorage.getItem('thebox-demo') === '1'; } catch (e) { /* se ignora */ }

    const esDemo = location.hostname.endsWith('github.io') ||
        location.protocol === 'file:' || forzado || activadoPorSesion;
    if (!esDemo) return;

    const CLAVE = 'thebox-demo-state';
    const HOY = hoyISO();

    const USUARIOS = {
        maria: {
            dni: '27999888', nombre: 'María González', clave: 'Maria2026',
            email: 'maria.gonzalez@email.com', telefono: '11 5555 0101', debeCambiarPassword: false
        },
        juan: {
            dni: '38450123', nombre: 'Juan Pérez', clave: '12345678901',
            email: 'juan.perez@email.com', telefono: '11 5555 0202', debeCambiarPassword: true
        }
    };

    const POLIZAS = [
        {
            id: 1, usuario: 'juan', numero: 'POL-000123', marca: 'Toyota', modelo: 'Corolla', anio: 2022,
            patente: 'AB123CD', tipoSeguro: 'Todo Riesgo', estado: 'Vigente',
            vigenciaDesde: '2024-01-01', vigenciaHasta: '2024-12-31', primaMensual: 25000,
            dias: [-105, -75, -45, -15, 15, 45], pagadas: 3, tuvoSuspension: false
        },
        {
            id: 2, usuario: 'juan', numero: 'POL-000456', marca: 'Ford', modelo: 'Ranger', anio: 2021,
            patente: 'AA456BB', tipoSeguro: 'Terceros Completo', estado: 'Suspendida',
            vigenciaDesde: '2023-06-01', vigenciaHasta: '2024-05-31', primaMensual: 36200,
            dias: [-120, -90, -60, -30, 0, 30], pagadas: 2, tuvoSuspension: true
        },
        {
            id: 3, usuario: 'juan', numero: 'POL-000789', marca: 'Honda', modelo: 'Wave', anio: 2020,
            patente: 'A123BCD', tipoSeguro: 'Responsabilidad Civil', estado: 'Cancelada',
            vigenciaDesde: '2022-03-01', vigenciaHasta: '2023-02-28', primaMensual: 9800,
            dias: [-420, -390, -360], pagadas: 3, tuvoSuspension: false
        },
        {
            id: 4, usuario: 'maria', numero: 'POL-000321', marca: 'Volkswagen', modelo: 'Gol Trend', anio: 2019,
            patente: 'AC789DE', tipoSeguro: 'Terceros Completo', estado: 'Vigente',
            vigenciaDesde: '2026-03-01', vigenciaHasta: '2027-02-28', primaMensual: 27400,
            dias: [-62, -32, -2, 28, 58, 88], pagadas: 3, tuvoSuspension: false
        },
        {
            id: 5, usuario: 'maria', numero: 'POL-000654', marca: 'Fiat', modelo: 'Cronos', anio: 2021,
            patente: 'AD321FG', tipoSeguro: 'Todo Riesgo', estado: 'Suspendida',
            vigenciaDesde: '2025-08-15', vigenciaHasta: '2026-08-14', primaMensual: 41900,
            dias: [-90, -60, -30, 0, 30], pagadas: 1, tuvoSuspension: true
        }
    ];

    const MEDIOS = [
        { id: 'visa-4242', descripcion: 'Visa terminada en 4242' },
        { id: 'master-5100', descripcion: 'Mastercard terminada en 5100' },
        { id: 'pagofacil', descripcion: 'Pago Fácil (efectivo)' }
    ];

    function hoyISO() {
        const n = new Date();
        return `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`;
    }

    function sumarDias(iso, dias) {
        const [a, m, d] = iso.split('-').map(Number);
        const fecha = new Date(Date.UTC(a, m - 1, d));
        fecha.setUTCDate(fecha.getUTCDate() + dias);
        return fecha.toISOString().slice(0, 10);
    }

    function vehiculo(p) { return `${p.marca} ${p.modelo} ${p.anio}`; }

    function estadoInicial() {
        const siniestros = [
            crearSiniestro(1, 'juan', 3, -505, 'Accidente', 'Cerrado', 'Av. Mitre 1500, Avellaneda',
                'Colisión con otro vehículo al girar en una esquina. Daños en el lateral derecho.'),
            crearSiniestro(2, 'juan', 2, -371, 'Robo', 'Cerrado', 'Calle Belgrano 800, Lanús',
                'Robo del vehículo estacionado en la vía pública durante la noche.'),
            crearSiniestro(3, 'juan', 1, -260, 'Accidente', 'En análisis', 'Ruta 4 km 12, Lomas de Zamora',
                'Choque en cadena por frenada brusca. Daños en el paragolpes delantero y el capó.'),
            crearSiniestro(4, 'maria', 4, -20, 'Daños por granizo', 'En análisis', 'Estacionamiento del trabajo, Quilmes',
                'Granizo durante una tormenta. Abolladuras en el techo y el capó.')
        ];

        return {
            usuario: 'maria',
            overrides: {},
            siniestros,
            nextPagoId: 9001,
            nextSiniestroId: 5,
            nextSolicitudId: 1
        };
    }

    function crearSiniestro(id, usuario, polizaId, diasAtras, tipo, estado, lugar, descripcion) {
        const p = POLIZAS.find(x => x.id === polizaId);
        const fecha = sumarDias(HOY, diasAtras);
        const fechaDenuncia = `${sumarDias(fecha, 1)}T10:00:00`;
        return {
            id, usuario, polizaId, numero: `SIN-${String(id).padStart(6, '0')}`,
            fecha, tipo, estado, poliza: p.numero, vehiculo: vehiculo(p),
            lugar, descripcion, fechaDenuncia,
            documentos: ['denuncia.pdf', 'fotos-danos.jpg'],
            seguimiento: [
                { fecha: fechaDenuncia, estado: 'Recibido', comentario: 'Denuncia registrada correctamente.' },
                { fecha: `${sumarDias(fecha, 3)}T14:30:00`, estado: 'En análisis', comentario: 'Un perito revisará el vehículo.' }
            ].slice(0, estado === 'Cerrado' ? 3 : 2).concat(
                estado === 'Cerrado'
                    ? [{ fecha: `${sumarDias(fecha, 12)}T11:00:00`, estado: 'Cerrado', comentario: 'Siniestro resuelto y liquidado.' }]
                    : []
            )
        };
    }

    function leerEstado() {
        try {
            const guardado = JSON.parse(sessionStorage.getItem(CLAVE));
            if (guardado && guardado.overrides && guardado.siniestros) return guardado;
        } catch (e) { /* se ignora */ }
        return estadoInicial();
    }

    function guardarEstado(estado) {
        try { sessionStorage.setItem(CLAVE, JSON.stringify(estado)); } catch (e) { /* se ignora */ }
    }

    let estado = leerEstado();

    try {
        if (!sessionStorage.getItem('token')) {
            sessionStorage.setItem('token', 'demo-token');
            sessionStorage.setItem('nombre', USUARIOS[estado.usuario].nombre);
        }
    } catch (e) { /* se ignora */ }

    function usuarioActual() { return USUARIOS[estado.usuario] || USUARIOS.maria; }

    function cuotasDePoliza(p) {
        return p.dias.map((dias, i) => {
            const id = p.id * 100 + i + 1;
            const original = sumarDias(HOY, dias);
            const ov = estado.overrides[id] || {};
            const vencimiento = ov.vencimiento || original;
            const pagada = ov.pagada === undefined ? i < p.pagadas : ov.pagada;
            const pagoId = ov.pagoId === undefined ? (pagada ? id + 1000 : null) : ov.pagoId;
            const estadoVisible = pagada ? 'Pagada' : (vencimiento < HOY ? 'Vencida' : 'Pendiente');
            return {
                id, numero: i + 1, vencimiento, vencimientoOriginal: original, monto: p.primaMensual,
                estado: estadoVisible, pagoId,
                pagable: !pagada && p.estado !== 'Cancelada',
                reprogramable: !pagada && vencimiento >= HOY
            };
        });
    }

    function estadoPoliza(p) {
        const hayVencidas = cuotasDePoliza(p).some(c => c.estado === 'Vencida');
        return p.estado === 'Suspendida' && !hayVencidas ? 'Vigente' : p.estado;
    }

    function enMora(p) { return cuotasDePoliza(p).some(c => c.estado === 'Vencida'); }

    function buscarCuota(id) {
        for (const p of POLIZAS) {
            const cuota = cuotasDePoliza(p).find(c => c.id === Number(id));
            if (cuota) return { poliza: p, cuota };
        }
        return null;
    }

    function json(body, status) {
        return new Response(JSON.stringify(body === undefined ? {} : body), {
            status: status || 200,
            headers: { 'Content-Type': 'application/json' }
        });
    }

    function error(mensaje, status) { return json({ errores: [mensaje] }, status); }

    function cuerpo(init) {
        if (!init || !init.body || typeof init.body !== 'string') return {};
        try { return JSON.parse(init.body); } catch (e) { return {}; }
    }

    function resumen(p) {
        return {
            id: p.id, numero: p.numero, vehiculo: vehiculo(p), tipoSeguro: p.tipoSeguro,
            estado: estadoPoliza(p), vigenciaDesde: p.vigenciaDesde, vigenciaHasta: p.vigenciaHasta
        };
    }

    function detalle(p) {
        return {
            id: p.id, numero: p.numero, marca: p.marca, modelo: p.modelo, anio: p.anio,
            patente: p.patente, tipoSeguro: p.tipoSeguro, estado: estadoPoliza(p),
            vigenciaDesde: p.vigenciaDesde, vigenciaHasta: p.vigenciaHasta, primaMensual: p.primaMensual
        };
    }

    function perfil() {
        const u = usuarioActual();
        return { nombre: u.nombre, dni: u.dni, email: u.email, telefono: u.telefono };
    }

    function politicasDelUsuario() { return POLIZAS.filter(p => p.usuario === estado.usuario); }

    function comprobante(pagoId) {
        const encontrado = buscarCuota(pagoId - 1000);
        const porPago = Object.entries(estado.overrides)
            .find(([, ov]) => ov.pagoId === pagoId);
        const par = porPago ? buscarCuota(Number(porPago[0])) : encontrado;
        if (!par || !par.cuota.pagoId) return error('El comprobante no existe.', 404);

        const { poliza, cuota } = par;
        const ov = estado.overrides[cuota.id] || {};
        const u = usuarioActual();
        return json({
            numero: `CP-${String(pagoId).padStart(6, '0')}`,
            fecha: ov.fechaPago || `${sumarDias(cuota.vencimiento, -1)}T15:00:00.000Z`,
            asegurado: u.nombre, dni: u.dni, poliza: poliza.numero, vehiculo: vehiculo(poliza),
            cuota: cuota.numero, vencimiento: cuota.vencimiento, monto: cuota.monto,
            medioPago: ov.medioPago || MEDIOS[0].descripcion,
            referencia: ov.referencia || `SIM-DEMO${cuota.id}`
        });
    }

    async function responder(ruta, init) {
        const metodo = (init.method || 'GET').toUpperCase();

        if (ruta === '/auth/login' && metodo === 'POST') {
            const datos = cuerpo(init);
            const dni = String(datos.dni || '').replace(/\D/g, '');
            const clave = Object.keys(USUARIOS).find(k => USUARIOS[k].dni === dni);
            const u = USUARIOS[clave];
            if (!u || datos.password !== u.clave) {
                return error('DNI o contraseña incorrectos, o la cuenta no está habilitada.', 401);
            }
            estado.usuario = clave;
            guardarEstado(estado);
            try { sessionStorage.setItem('nombre', u.nombre); } catch (e) { /* se ignora */ }
            return json({ token: `demo-token-${clave}`, nombre: u.nombre, debeCambiarPassword: u.debeCambiarPassword });
        }

        if (ruta === '/auth/cambiar-password' && metodo === 'POST') {
            return json({ token: 'demo-token-ok' });
        }

        if (ruta === '/portal/me') return json(perfil());

        if (ruta === '/polizas') return json(politicasDelUsuario().map(resumen));

        let m = ruta.match(/^\/polizas\/(\d+)$/);
        if (m && metodo === 'GET') {
            const p = POLIZAS.find(x => x.id === Number(m[1]) && x.usuario === estado.usuario);
            return p ? json(detalle(p)) : error('La póliza no existe.', 404);
        }

        m = ruta.match(/^\/polizas\/(\d+)\/cuotas$/);
        if (m && metodo === 'GET') {
            const p = POLIZAS.find(x => x.id === Number(m[1]) && x.usuario === estado.usuario);
            if (!p) return error('La póliza no existe.', 404);
            return json({
                polizaId: p.id, numero: p.numero, estadoPoliza: estadoPoliza(p),
                enMora: enMora(p), cuotas: cuotasDePoliza(p)
            });
        }

        if (ruta === '/pagos/medios') return json(MEDIOS);

        if (ruta === '/pagos/proximo') {
            const pendientes = politicasDelUsuario()
                .filter(p => p.estado !== 'Cancelada')
                .flatMap(p => cuotasDePoliza(p).filter(c => !c.pagada).map(c => ({ p, c })))
                .sort((a, b) => a.c.vencimiento.localeCompare(b.c.vencimiento));
            if (!pendientes.length) return new Response(null, { status: 204 });
            const { p, c } = pendientes[0];
            return json({
                vencimiento: c.vencimiento, monto: c.monto, poliza: p.numero, vencida: c.vencimiento < HOY
            });
        }

        m = ruta.match(/^\/pagos\/(\d+)\/comprobante$/);
        if (m && metodo === 'GET') return comprobante(Number(m[1]));

        m = ruta.match(/^\/cuotas\/(\d+)\/pagos$/);
        if (m && metodo === 'POST') {
            const datos = cuerpo(init);
            const medio = MEDIOS.find(x => x.id === datos.medioPagoId);
            if (!medio) return error('Elegí un medio de pago válido.', 400);
            const encontrado = buscarCuota(m[1]);
            if (!encontrado) return error('La cuota no existe.', 404);
            const { poliza, cuota } = encontrado;
            if (cuota.pagada) return error('La cuota ya está paga.', 409);
            if (poliza.estado === 'Cancelada') return error('La póliza está cancelada y no admite pagos.', 409);

            const estadoPago = datos.medioPagoId === 'master-5100' ? 'Rechazado'
                : datos.medioPagoId === 'pagofacil' ? 'Pendiente' : 'Aprobado';
            const mensaje = estadoPago === 'Rechazado' ? 'La tarjeta fue rechazada por fondos insuficientes.'
                : estadoPago === 'Pendiente' ? 'Tu pago está pendiente de acreditación.'
                    : 'Tu pago fue acreditado correctamente.';
            const referencia = `SIM-${Math.random().toString(16).slice(2, 14).toUpperCase()}`;
            const aprobado = estadoPago === 'Aprobado';
            let pagoId = null;
            let comprobanteNumero = null;

            if (aprobado) {
                pagoId = estado.nextPagoId++;
                comprobanteNumero = `CP-${String(pagoId).padStart(6, '0')}`;
                estado.overrides[cuota.id] = {
                    ...(estado.overrides[cuota.id] || {}),
                    pagada: true, pagoId, fechaPago: new Date().toISOString(),
                    medioPago: medio.descripcion, referencia
                };
                guardarEstado(estado);
            }

            return json({
                estado: estadoPago, mensaje, referencia, pagoId, comprobante: comprobanteNumero,
                estadoCuota: aprobado ? 'Pagada' : cuota.estado,
                estadoPoliza: estadoPoliza(poliza), enMora: enMora(poliza)
            });
        }

        m = ruta.match(/^\/cuotas\/(\d+)\/vencimiento$/);
        if (m && metodo === 'POST') {
            const datos = cuerpo(init);
            const encontrado = buscarCuota(m[1]);
            if (!encontrado) return error('La cuota no existe.', 404);
            const { poliza, cuota } = encontrado;

            if (poliza.tuvoSuspension || estadoPoliza(poliza) === 'Suspendida') {
                return error('No cumplís con las condiciones habilitadas para cambiar la fecha de vencimiento: la póliza registra una suspensión por impagos.', 409);
            }
            if (estadoPoliza(poliza) !== 'Vigente') {
                return error('No cumplís con las condiciones habilitadas para cambiar la fecha de vencimiento: la póliza no está activa.', 409);
            }
            if (cuota.pagada) return error('La cuota ya está paga.', 409);
            if (cuota.vencimiento < HOY) {
                return error('La cuota ya venció, por lo que no se puede cambiar su fecha. Podés abonarla desde Mis pagos.', 409);
            }
            const nueva = datos.nuevaFecha;
            const maxima = sumarDias(cuota.vencimientoOriginal, 15);
            if (!nueva) return error('Elegí la nueva fecha de vencimiento.', 400);
            if (nueva <= cuota.vencimientoOriginal || nueva > maxima) {
                return error(`La nueva fecha debe ser posterior al vencimiento original (${cuota.vencimientoOriginal}) y hasta el ${maxima} (15 días corridos).`, 400);
            }
            if (nueva < HOY) return error('La nueva fecha no puede ser anterior a hoy.', 400);
            if (nueva === cuota.vencimiento) return error('Esa ya es la fecha de vencimiento de la cuota.', 400);

            const anterior = cuota.vencimiento;
            estado.overrides[cuota.id] = { ...(estado.overrides[cuota.id] || {}), vencimiento: nueva };
            guardarEstado(estado);
            return json({
                cuotaId: cuota.id, vencimientoAnterior: anterior, vencimientoNuevo: nueva,
                mensaje: 'La fecha de vencimiento se actualizó correctamente.'
            });
        }

        if (ruta === '/siniestros' && metodo === 'GET') {
            return json(estado.siniestros
                .filter(s => s.usuario === estado.usuario)
                .sort((a, b) => b.fecha.localeCompare(a.fecha) || b.id - a.id));
        }

        if (ruta === '/siniestros' && metodo === 'POST') {
            const fd = init.body;
            if (!fd || typeof fd.get !== 'function') return error('Formato de solicitud inválido.', 400);
            const poliza = POLIZAS.find(p => p.id === Number(fd.get('polizaId')));
            const fecha = String(fd.get('fecha') || HOY);
            if (!poliza || !fecha || !fd.get('tipo') || !fd.get('lugar') || !fd.get('descripcion')) {
                return error('Completá todos los datos obligatorios.', 400);
            }
            const id = estado.nextSiniestroId++;
            const documentos = (fd.getAll('documentos') || []).map(f => f.name).filter(Boolean);
            const registro = {
                id, usuario: estado.usuario, polizaId: poliza.id,
                numero: `SIN-${String(id).padStart(6, '0')}`, fecha, tipo: fd.get('tipo'),
                estado: 'Recibido', poliza: poliza.numero, vehiculo: vehiculo(poliza),
                lugar: fd.get('lugar'), descripcion: fd.get('descripcion'),
                fechaDenuncia: new Date().toISOString(), documentos,
                seguimiento: [{ fecha: new Date().toISOString(), estado: 'Recibido', comentario: 'Denuncia registrada correctamente.' }]
            };
            estado.siniestros.push(registro);
            guardarEstado(estado);
            return json({ id, numero: registro.numero, estado: registro.estado });
        }

        m = ruta.match(/^\/siniestros\/(\d+)$/);
        if (m && metodo === 'GET') {
            const s = estado.siniestros.find(x => x.id === Number(m[1]) && x.usuario === estado.usuario);
            return s ? json(s) : error('El siniestro no existe.', 404);
        }

        if (ruta === '/presupuestos' && metodo === 'POST') {
            const id = estado.nextSolicitudId++;
            guardarEstado(estado);
            return json({ id, estado: 'Pendiente' });
        }

        return error('Endpoint de demostración no disponible.', 404);
    }

    const fetchReal = window.fetch.bind(window);
    window.fetch = function (input, init) {
        let url;
        try {
            url = new URL(typeof input === 'string' ? input : input.url, location.href);
        } catch (e) {
            return fetchReal(input, init);
        }
        if (!url.pathname.startsWith('/api/')) return fetchReal(input, init);
        return responder(url.pathname.replace(/^\/api/, ''), init || {});
    };
})();
