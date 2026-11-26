/* Generación de PDFs y almacenamiento local para consulta offline. */
(function () {
    'use strict';

    const DB_NAME = 'thebox-documentos';
    const STORE = 'documentos';

    const TIPOS = [
        { id: 'frente', titulo: 'Frente de póliza', descripcion: 'Datos de la póliza, el vehículo y el asegurado.', archivo: 'frente-poliza' },
        { id: 'tarjeta', titulo: 'Tarjeta de circulación digital', descripcion: 'Identificación del vehículo asegurado.', archivo: 'tarjeta-circulacion' },
        { id: 'vigencia', titulo: 'Comprobante de vigencia', descripcion: 'Constancia del período de cobertura.', archivo: 'comprobante-vigencia' }
    ];

    function abrirDB() {
        return new Promise((resolver, rechazar) => {
            const req = indexedDB.open(DB_NAME, 1);
            req.onupgradeneeded = () => {
                const db = req.result;
                if (!db.objectStoreNames.contains(STORE)) db.createObjectStore(STORE, { keyPath: 'id' });
            };
            req.onsuccess = () => resolver(req.result);
            req.onerror = () => rechazar(req.error);
        });
    }

    function guardar(registro) {
        return abrirDB().then(db => new Promise((resolver, rechazar) => {
            const tx = db.transaction(STORE, 'readwrite');
            tx.objectStore(STORE).put(registro);
            tx.oncomplete = () => { db.close(); resolver(registro); };
            tx.onerror = () => { db.close(); rechazar(tx.error); };
        }));
    }

    function listar() {
        return abrirDB().then(db => new Promise((resolver, rechazar) => {
            const req = db.transaction(STORE, 'readonly').objectStore(STORE).getAll();
            req.onsuccess = () => {
                db.close();
                resolver(req.result.sort((a, b) => b.fecha.localeCompare(a.fecha)));
            };
            req.onerror = () => { db.close(); rechazar(req.error); };
        }));
    }

    function obtener(id) {
        return abrirDB().then(db => new Promise((resolver, rechazar) => {
            const req = db.transaction(STORE, 'readonly').objectStore(STORE).get(id);
            req.onsuccess = () => { db.close(); resolver(req.result); };
            req.onerror = () => { db.close(); rechazar(req.error); };
        }));
    }

    function eliminar(id) {
        return abrirDB().then(db => new Promise((resolver, rechazar) => {
            const tx = db.transaction(STORE, 'readwrite');
            tx.objectStore(STORE).delete(id);
            tx.oncomplete = () => { db.close(); resolver(); };
            tx.onerror = () => { db.close(); rechazar(tx.error); };
        }));
    }

    const fmtFecha = iso => { const [y, m, d] = String(iso).split('-'); return `${d}/${m}/${y}`; };
    const fmtMoneda = n => new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(Number(n));
    const hoyISO = () => {
        const n = new Date();
        return `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`;
    };
    const slug = s => String(s).normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-zA-Z0-9]+/g, '-').replace(/^-|-$/g, '').toLowerCase();

    function encabezado(doc, titulo) {
        doc.setFillColor(22, 50, 92);
        doc.rect(0, 0, 210, 30, 'F');
        doc.setTextColor(255, 255, 255);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(17);
        doc.text('THE BOX SEGUROS', 14, 13);
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(9);
        doc.text('Gestion de seguros', 14, 21);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.text(titulo, 196, 14, { align: 'right' });
        doc.setTextColor(17, 24, 39);
    }

    function fila(doc, y, etiqueta, valor) {
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(9);
        doc.setTextColor(107, 114, 128);
        doc.text(etiqueta, 14, y);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(17, 24, 39);
        doc.text(String(valor == null ? '-' : valor), 78, y);
    }

    function pie(doc, poliza) {
        doc.setDrawColor(227, 232, 240);
        doc.line(14, 272, 196, 272);
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(8);
        doc.setTextColor(107, 114, 128);
        doc.text(`Poliza ${poliza.numero} - Generado el ${fmtFecha(hoyISO())}`, 14, 279);
        doc.text('Documento generado digitalmente por el portal del asegurado', 196, 279, { align: 'right' });
    }

    function generarPDF(tipo, poliza, perfil) {
        const jsPDF = window.jspdf.jsPDF;
        const doc = new jsPDF({ unit: 'mm', format: 'a4' });
        const vehiculo = `${poliza.marca} ${poliza.modelo} ${poliza.anio}`;

        if (tipo === 'frente') {
            encabezado(doc, 'Frente de poliza');
            doc.setFont('helvetica', 'bold');
            doc.setFontSize(13);
            doc.text(`Poliza Nro. ${poliza.numero}`, 14, 45);
            doc.setDrawColor(227, 232, 240);
            doc.line(14, 50, 196, 50);

            let y = 60;
            fila(doc, y, 'Asegurado', perfil.nombre); y += 8;
            fila(doc, y, 'DNI', perfil.dni); y += 8;
            fila(doc, y, 'Vehiculo', vehiculo); y += 8;
            fila(doc, y, 'Patente', poliza.patente); y += 8;
            fila(doc, y, 'Tipo de seguro', poliza.tipoSeguro); y += 8;
            fila(doc, y, 'Estado', poliza.estado); y += 8;
            fila(doc, y, 'Vigencia desde', fmtFecha(poliza.vigenciaDesde)); y += 8;
            fila(doc, y, 'Vigencia hasta', fmtFecha(poliza.vigenciaHasta)); y += 8;
            fila(doc, y, 'Prima mensual', fmtMoneda(poliza.primaMensual));

            doc.setFillColor(243, 246, 251);
            doc.roundedRect(14, 140, 182, 22, 2, 2, 'F');
            doc.setFont('helvetica', 'normal');
            doc.setFontSize(9);
            doc.setTextColor(55, 65, 81);
            doc.text('La cobertura se rige por las condiciones generales y particulares de la poliza.', 20, 152);
            pie(doc, poliza);
        } else if (tipo === 'tarjeta') {
            encabezado(doc, 'Tarjeta de circulacion digital');
            doc.setFillColor(243, 246, 251);
            doc.roundedRect(14, 45, 182, 76, 3, 3, 'F');
            doc.setDrawColor(213, 219, 229);
            doc.roundedRect(14, 45, 182, 76, 3, 3, 'S');

            doc.setFont('helvetica', 'bold');
            doc.setFontSize(9);
            doc.setTextColor(107, 114, 128);
            doc.text('PATENTE', 105, 58, { align: 'center' });
            doc.setFontSize(28);
            doc.setTextColor(22, 50, 92);
            doc.text(poliza.patente, 105, 78, { align: 'center' });
            doc.setFontSize(11);
            doc.setTextColor(55, 65, 81);
            doc.text(vehiculo, 105, 95, { align: 'center' });
            doc.setFont('helvetica', 'normal');
            doc.setFontSize(9);
            doc.text(`Poliza ${poliza.numero} - ${poliza.tipoSeguro}`, 105, 108, { align: 'center' });

            doc.setTextColor(17, 24, 39);
            let y = 140;
            fila(doc, y, 'Asegurado', perfil.nombre); y += 8;
            fila(doc, y, 'DNI', perfil.dni); y += 8;
            fila(doc, y, 'Vigencia', `${fmtFecha(poliza.vigenciaDesde)} al ${fmtFecha(poliza.vigenciaHasta)}`); y += 8;
            fila(doc, y, 'Estado', poliza.estado);
            pie(doc, poliza);
        } else {
            encabezado(doc, 'Comprobante de vigencia');
            const vigente = poliza.estado === 'Vigente';
            doc.setFillColor(vigente ? 223 : 253, vigente ? 243 : 222, vigente ? 230 : 222);
            doc.roundedRect(14, 45, 182, 24, 2, 2, 'F');
            doc.setFont('helvetica', 'bold');
            doc.setFontSize(14);
            doc.setTextColor(vigente ? 31 : 180, vigente ? 122 : 35, vigente ? 70 : 24);
            doc.text(vigente ? 'POLIZA VIGENTE' : `ESTADO: ${poliza.estado.toUpperCase()}`, 105, 61, { align: 'center' });

            doc.setTextColor(17, 24, 39);
            doc.setFont('helvetica', 'normal');
            doc.setFontSize(10);
            const texto = vigente
                ? `Se certifica que la poliza Nro. ${poliza.numero}, correspondiente a ${perfil.nombre}, se encuentra vigente al ${fmtFecha(hoyISO())}.`
                : `La poliza Nro. ${poliza.numero}, correspondiente a ${perfil.nombre}, registra el estado ${poliza.estado.toLowerCase()} al ${fmtFecha(hoyISO())}.`;
            doc.text(doc.splitTextToSize(texto, 182), 14, 88);

            let y = 110;
            fila(doc, y, 'Vehiculo', vehiculo); y += 8;
            fila(doc, y, 'Patente', poliza.patente); y += 8;
            fila(doc, y, 'Tipo de seguro', poliza.tipoSeguro); y += 8;
            fila(doc, y, 'Periodo de cobertura', `${fmtFecha(poliza.vigenciaDesde)} al ${fmtFecha(poliza.vigenciaHasta)}`);
            pie(doc, poliza);
        }

        return doc.output('blob');
    }

    function descargar(blob, nombre) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = nombre;
        document.body.append(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function registrar(tipo, poliza, perfil, blob) {
        const definicion = TIPOS.find(t => t.id === tipo);
        return guardar({
            id: `${poliza.id}-${tipo}`,
            polizaId: poliza.id,
            polizaNumero: poliza.numero,
            tipo,
            titulo: definicion.titulo,
            fecha: new Date().toISOString(),
            blob
        });
    }

    async function descargarUno(tipo, poliza, perfil) {
        const definicion = TIPOS.find(t => t.id === tipo);
        const blob = generarPDF(tipo, poliza, perfil);
        await registrar(tipo, poliza, perfil, blob);
        descargar(blob, `${slug(poliza.numero)}-${definicion.archivo}.pdf`);
    }

    async function descargarTodos(poliza, perfil) {
        const zip = new JSZip();
        for (const definicion of TIPOS) {
            const blob = generarPDF(definicion.id, poliza, perfil);
            await registrar(definicion.id, poliza, perfil, blob);
            zip.file(`${slug(poliza.numero)}-${definicion.archivo}.pdf`, blob);
        }
        const comprimido = await zip.generateAsync({ type: 'blob' });
        descargar(comprimido, `${slug(poliza.numero)}-documentacion.zip`);
    }

    function abrir(id, destino) {
        return obtener(id).then(registro => {
            if (!registro) {
                if (destino) destino.close();
                return false;
            }
            const url = URL.createObjectURL(registro.blob);
            if (destino) destino.location.href = url;
            else window.open(url, '_blank');
            setTimeout(() => URL.revokeObjectURL(url), 60000);
            return true;
        });
    }

    window.Documentos = { TIPOS, descargarUno, descargarTodos, listar, abrir, eliminar };
})();
