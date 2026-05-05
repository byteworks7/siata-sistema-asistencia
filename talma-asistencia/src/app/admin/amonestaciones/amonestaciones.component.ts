import { Component, OnInit, signal } from '@angular/core';
import { CommonModule }              from '@angular/common';
import { FormsModule }               from '@angular/forms';
import { HttpClient }                from '@angular/common/http';
import { environment }               from '../../../environments/environment';

interface Trabajador {
  id:        number;
  dni:       string;
  nombres:   string;
  apellidos: string;
  cargo:     string;
  area:      string;
}

interface Amonestacion {
  id:               number;
  idTrabajador:     number;
  nombreTrabajador: string;
  tipo:             string;
  motivo:           string;
  fechaEmision:     string;
  diasSuspension:   number;
  correoEnviado:    boolean;
}

interface ResumenTrabajador {
  tardanzas:      number;
  faltas:         number;
  avisosEscritos: number;
  suspensiones:   number;
}

@Component({
  selector: 'app-amonestaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './amonestaciones.component.html',
  styleUrls: ['./amonestaciones.component.scss']
})
export class AmonestacionesComponent implements OnInit {

  readonly API = environment.apiUrl;

  trabajadores   = signal<Trabajador[]>([]);
  amonestaciones = signal<Amonestacion[]>([]);
  cargando       = signal(false);
  guardando      = signal(false);
  mensaje        = signal('');
  mensajeError   = signal(false);
  mostrarForm    = signal(false);
  busqueda       = '';
  filtroTrabajador = 0;

  resumen         = signal<ResumenTrabajador | null>(null);
  cargandoResumen = signal(false);
  trabajadorSeleccionado = signal<Trabajador | null>(null);

  // Resultado de días suspendidos
  diasSuspendidos = signal<string[]>([]);

  form = {
    idTrabajador:       0,
    tipo:               'AVISO_ESCRITO',
    motivo:             '',
    fechaInicioSuspension: ''  // solo para suspensiones
  };

  readonly tipos = [
    { val: 'AVISO_ESCRITO', lbl: 'Aviso escrito',     dias: 0, color: 'naranja' },
    { val: 'SUSPENSION_1D', lbl: 'Suspensión 1 día',  dias: 1, color: 'rojo' },
    { val: 'SUSPENSION_2D', lbl: 'Suspensión 2 días', dias: 2, color: 'rojo' },
    { val: 'SUSPENSION_3D', lbl: 'Suspensión 3 días', dias: 3, color: 'rojo' },
  ];

  constructor(private http: HttpClient) {}

  ngOnInit() { this.cargarTodo(); }

  get esSuspension(): boolean { return this.form.tipo !== 'AVISO_ESCRITO'; }
  get diasTipo(): number { return this.tipos.find(t => t.val === this.form.tipo)?.dias ?? 0; }
  get totalAvisos()       { return this.amonestaciones().filter(a => a.tipo === 'AVISO_ESCRITO').length; }
  get totalSuspensiones() { return this.amonestaciones().filter(a => a.tipo !== 'AVISO_ESCRITO').length; }

  get amonestacionesFiltradas(): Amonestacion[] {
    let lista = this.amonestaciones();
    if (this.filtroTrabajador) lista = lista.filter(a => a.idTrabajador === this.filtroTrabajador);
    if (this.busqueda) {
      const b = this.busqueda.toLowerCase();
      lista = lista.filter(a => a.nombreTrabajador.toLowerCase().includes(b));
    }
    return lista;
  }

  cargarTodo() {
    this.cargando.set(true);
    this.http.get<Trabajador[]>(`${this.API}/trabajadores`).subscribe(t => this.trabajadores.set(t));
    this.cargarAmonestaciones();
  }

  cargarAmonestaciones() {
    this.cargando.set(true);
    const url = this.filtroTrabajador
      ? `${this.API}/amonestaciones?idTrabajador=${this.filtroTrabajador}`
      : `${this.API}/amonestaciones`;
    this.http.get<Amonestacion[]>(url).subscribe({
      next: a => { this.amonestaciones.set(a); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirFormNuevo() {
    this.form = {
      idTrabajador:          0,
      tipo:                  'AVISO_ESCRITO',
      motivo:                '',
      fechaInicioSuspension: new Date(Date.now() + 86400000).toISOString().slice(0, 10) // mañana por defecto
    };
    this.resumen.set(null);
    this.trabajadorSeleccionado.set(null);
    this.diasSuspendidos.set([]);
    this.mostrarForm.set(true);
  }

  cerrarForm() { this.mostrarForm.set(false); this.resumen.set(null); this.diasSuspendidos.set([]); }

  onTrabajadorChange() {
    const id = this.form.idTrabajador;
    if (!id) { this.resumen.set(null); this.trabajadorSeleccionado.set(null); return; }
    const t = this.trabajadores().find(t => t.id === id) ?? null;
    this.trabajadorSeleccionado.set(t);
    this.cargarResumen(id);
  }

  cargarResumen(idTrabajador: number) {
    this.cargandoResumen.set(true);
    const mes  = new Date().getMonth() + 1;
    const anio = new Date().getFullYear();

    this.http.get<any[]>(`${this.API}/asistencias/trabajador/${idTrabajador}?mes=${mes}&anio=${anio}`).subscribe({
      next: asistencias => {
        const tardanzas = asistencias.filter(a => a.estadoEntrada === 'TARDANZA').length;
        const faltas    = asistencias.filter(a => a.estadoEntrada === 'FALTA').length;
        this.http.get<Amonestacion[]>(`${this.API}/amonestaciones?idTrabajador=${idTrabajador}`).subscribe({
          next: amon => {
            this.resumen.set({
              tardanzas,
              faltas,
              avisosEscritos: amon.filter(a => a.tipo === 'AVISO_ESCRITO').length,
              suspensiones:   amon.filter(a => a.tipo !== 'AVISO_ESCRITO').length
            });
            this.cargandoResumen.set(false);
          },
          error: () => this.cargandoResumen.set(false)
        });
      },
      error: () => this.cargandoResumen.set(false)
    });
  }

  guardar() {
    if (!this.form.idTrabajador) { this.mostrarMensaje('Selecciona un trabajador', true); return; }
    if (!this.form.motivo.trim()) { this.mostrarMensaje('Ingresa el motivo', true); return; }
    if (this.esSuspension && !this.form.fechaInicioSuspension) {
      this.mostrarMensaje('Selecciona la fecha de inicio de suspensión', true); return;
    }

    this.guardando.set(true);

    const payload: any = {
      idTrabajador:          this.form.idTrabajador,
      tipo:                  this.form.tipo,
      motivo:                this.form.motivo,
      fechaInicioSuspension: this.esSuspension ? this.form.fechaInicioSuspension : null
    };

    this.http.post<any>(`${this.API}/amonestaciones`, payload).subscribe({
      next: (res) => {
        this.guardando.set(false);
        if (res.diasSuspendidos?.length > 0) {
          this.diasSuspendidos.set(res.diasSuspendidos);
          this.mostrarMensaje(`Amonestación registrada ✓ — Suspensión: ${res.diasSuspendidos.join(', ')}`, false);
        } else {
          this.mostrarMensaje('Amonestación registrada ✓', false);
          this.mostrarForm.set(false);
        }
        this.cargarAmonestaciones();
      },
      error: (e) => {
        this.guardando.set(false);
        this.mostrarMensaje(e.error?.mensaje ?? 'Error al guardar', true);
      }
    });
  }

  confirmarCerrar() { this.mostrarForm.set(false); this.diasSuspendidos.set([]); }

  eliminar(a: Amonestacion) {
    if (!confirm(`¿Eliminar amonestación de ${a.nombreTrabajador}?`)) return;
    this.http.delete(`${this.API}/amonestaciones/${a.id}`).subscribe({
      next: () => { this.mostrarMensaje('Eliminada ✓', false); this.cargarAmonestaciones(); },
      error: () => this.mostrarMensaje('Error al eliminar', true)
    });
  }

  getClaseTipo(tipo: string): string { return tipo === 'AVISO_ESCRITO' ? 'naranja' : 'rojo'; }
  getEtiquetaTipo(tipo: string): string { return this.tipos.find(t => t.val === tipo)?.lbl ?? tipo; }

  private mostrarMensaje(msg: string, esError: boolean) {
    this.mensaje.set(msg);
    this.mensajeError.set(esError);
    setTimeout(() => this.mensaje.set(''), 5000);
  }
}