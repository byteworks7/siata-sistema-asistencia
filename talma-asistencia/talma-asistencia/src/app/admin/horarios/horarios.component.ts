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

interface PlantillaTurno {
  id:               number;
  nombre:           string;
  horaEntrada:      string;
  horaSalida:       string;
  descripcion:      string;
  diasTrabajoCiclo: number | null;
  diasDescansoCiclo:number | null;
  diasSemanaFijos:  string | null;
}

interface Horario {
  id:               number;
  idTrabajador:     number;
  nombreTrabajador: string;
  idPlantilla:      number | null;
  nombrePlantilla:  string;
  horaEntrada:      string;
  horaSalida:       string;
  diasDescanso:     string;
  fechaInicio:      string;
  fechaFin:         string | null;
}

@Component({
  selector: 'app-horarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './horarios.component.html',
  styleUrls: ['./horarios.component.scss']
})
export class HorariosComponent implements OnInit {

  readonly API = environment.apiUrl;

  trabajadores  = signal<Trabajador[]>([]);
  plantillas    = signal<PlantillaTurno[]>([]);
  horarios      = signal<Horario[]>([]);
  cargando      = signal(false);
  guardando     = signal(false);
  mensaje       = signal('');
  mensajeError  = signal(false);
  mostrarForm   = signal(false);
  editando      = signal<Horario | null>(null);

  // Tipo de horario: 'rotativo' o 'fijo'
  tipoHorario = 'rotativo';

  form = {
    idTrabajador:      0,
    idPlantilla:       0,
    horaEntrada:       '06:00',
    horaSalida:        '14:00',
    diasTrabajoCiclo:  5,
    diasDescansoCiclo: 1,
    diasTrabajo:       '',
    fechaInicio:       new Date().toISOString().slice(0, 10),
    fechaFin:          ''
  };

  busqueda = '';

  constructor(private http: HttpClient) {}

  ngOnInit() { this.cargarTodo(); }

  cargarTodo() {
    this.cargando.set(true);
    this.http.get<Trabajador[]>(`${this.API}/trabajadores`).subscribe(t => this.trabajadores.set(t));
    this.http.get<PlantillaTurno[]>(`${this.API}/horarios/plantillas`).subscribe(p => this.plantillas.set(p));
    this.http.get<Horario[]>(`${this.API}/horarios`).subscribe({
      next: h => { this.horarios.set(h); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  get horariosFiltrados(): Horario[] {
    if (!this.busqueda) return this.horarios();
    const b = this.busqueda.toLowerCase();
    return this.horarios().filter(h => h.nombreTrabajador?.toLowerCase().includes(b));
  }

  abrirFormNuevo() {
    this.editando.set(null);
    this.tipoHorario = 'rotativo';
    this.form = {
      idTrabajador:      0,
      idPlantilla:       0,
      horaEntrada:       '06:00',
      horaSalida:        '14:00',
      diasTrabajoCiclo:  5,
      diasDescansoCiclo: 1,
      diasTrabajo:       '',
      fechaInicio:       new Date().toISOString().slice(0, 10),
      fechaFin:          ''
    };
    this.mostrarForm.set(true);
  }

  abrirFormEditar(h: Horario) {
    this.editando.set(h);
    // Determinar tipo por diasDescanso
    const esRotativo = h.diasDescanso?.includes('rotativo') || h.diasDescanso === '—';
    this.tipoHorario = esRotativo ? 'rotativo' : 'fijo';

    // Extraer ciclo del texto "5x1 (rotativo)"
    let trabaja = 5, descansa = 1;
    if (h.diasDescanso && h.diasDescanso.includes('x')) {
      const parts = h.diasDescanso.split('x');
      trabaja  = parseInt(parts[0]) || 5;
      descansa = parseInt(parts[1]) || 1;
    }

    this.form = {
      idTrabajador:      h.idTrabajador,
      idPlantilla:       h.idPlantilla ?? 0,
      horaEntrada:       h.horaEntrada,
      horaSalida:        h.horaSalida,
      diasTrabajoCiclo:  trabaja,
      diasDescansoCiclo: descansa,
      diasTrabajo:       esRotativo ? '' : h.diasDescanso,
      fechaInicio:       h.fechaInicio?.slice(0, 10) ?? '',
      fechaFin:          h.fechaFin?.slice(0, 10) ?? ''
    };
    this.mostrarForm.set(true);
  }

  cerrarForm() { this.mostrarForm.set(false); }

  onPlantillaChange() {
    const p = this.plantillas().find(p => p.id === +this.form.idPlantilla);
    if (p) {
      if (p.diasTrabajoCiclo) {
        this.tipoHorario           = 'rotativo';
        this.form.diasTrabajoCiclo  = p.diasTrabajoCiclo;
        this.form.diasDescansoCiclo = p.diasDescansoCiclo ?? 1;
      } else if (p.diasSemanaFijos) {
        this.tipoHorario      = 'fijo';
        this.form.diasTrabajo = p.diasSemanaFijos;
      }
    }
  }

  guardar() {
    if (!this.form.idTrabajador) { this.mostrarMensaje('Selecciona un trabajador', true); return; }
    if (!this.form.horaEntrada || !this.form.horaSalida) { this.mostrarMensaje('Completa los horarios', true); return; }

    this.guardando.set(true);

    const payload: any = {
      idTrabajador:       this.form.idTrabajador,
      idPlantilla:        this.form.idPlantilla || null,
      horaEntrada:        this.form.horaEntrada,
      horaSalida:         this.form.horaSalida,
      toleranciaMinutos:  5,
      fechaInicio:        this.form.fechaInicio,
      fechaFin:           this.form.fechaFin || null,
      cerrarHorarioAnterior: false,
    };

    if (this.tipoHorario === 'rotativo') {
      payload.diasTrabajoCiclo  = this.form.diasTrabajoCiclo;
      payload.diasDescansoCiclo = this.form.diasDescansoCiclo;
      payload.diasTrabajo       = null;
    } else {
      payload.diasTrabajoCiclo  = null;
      payload.diasDescansoCiclo = null;
      payload.diasTrabajo       = this.form.diasTrabajo || null;
    }

    const req = this.editando()
      ? this.http.put(`${this.API}/horarios/${this.editando()!.id}`, payload)
      : this.http.post(`${this.API}/horarios`, payload);

    req.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mostrarForm.set(false);
        this.mostrarMensaje('Horario guardado correctamente ✓', false);
        this.cargarTodo();
      },
      error: (e) => {
        this.guardando.set(false);
        this.mostrarMensaje(e.error?.mensaje ?? 'Error al guardar', true);
      }
    });
  }

  eliminar(h: Horario) {
    if (!confirm(`¿Eliminar horario de ${h.nombreTrabajador}?`)) return;
    this.http.delete(`${this.API}/horarios/${h.id}`).subscribe({
      next: () => { this.mostrarMensaje('Horario eliminado', false); this.cargarTodo(); },
      error: () => this.mostrarMensaje('Error al eliminar', true)
    });
  }

  private mostrarMensaje(msg: string, esError: boolean) {
    this.mensaje.set(msg);
    this.mensajeError.set(esError);
    setTimeout(() => this.mensaje.set(''), 3500);
  }
}