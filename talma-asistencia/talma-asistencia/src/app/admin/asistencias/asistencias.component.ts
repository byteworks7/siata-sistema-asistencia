import { Component, OnInit, signal } from '@angular/core';
import { CommonModule }              from '@angular/common';
import { FormsModule }               from '@angular/forms';
import { HttpClient }           from '@angular/common/http';
import { forkJoin }             from 'rxjs';
import { environment }               from '../../../environments/environment';

interface Trabajador {
  id:        number;
  dni:       string;
  nombres:   string;
  apellidos: string;
}

interface Asistencia {
  id:                number | null;
  idTrabajador:      number;
  fecha:             string;
  horaEntrada:       string | null;
  horaSalida:        string | null;
  estadoEntrada:     string;
  estadoSalida:      string;
  minutosTardanza:   number;
  corregidoPorAdmin: boolean;
  observacion:       string | null;
  tieneRegistro:     boolean;
  esDescanso:        boolean;
}

@Component({
  selector: 'app-asistencias',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asistencias.component.html',
  styleUrls: ['./asistencias.component.scss']
})
export class AsistenciasComponent implements OnInit {

  readonly API = environment.apiUrl;

  trabajadores  = signal<Trabajador[]>([]);
  asistencias   = signal<Asistencia[]>([]);
  cargando      = signal(false);
  guardando     = signal(false);
  mensaje       = signal('');
  mensajeError  = signal(false);
  mostrarForm   = signal(false);
  editando      = signal<Asistencia | null>(null);

  filtroTrabajador = 0;
  filtroMes        = new Date().getMonth() + 1;
  filtroAnio       = new Date().getFullYear();

  readonly meses = ['Enero','Febrero','Marzo','Abril','Mayo','Junio',
                    'Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];

  form = {
    horaEntrada:   '',
    horaSalida:    '',
    estadoEntrada: 'VACACIONES',
    estadoSalida:  'PENDIENTE',
    observacion:   ''
  };

  readonly estadosEntrada = ['PUNTUAL','A_TIEMPO','TARDANZA','FALTA','VACACIONES','SIN_REGISTRO'];
  readonly estadosSalida  = ['REGISTRADA','SALIDA_ANTICIPADA','SALIDA_NO_REGISTRADA','PENDIENTE'];

  constructor(private http: HttpClient) {}

  ngOnInit() { this.cargarTrabajadores(); }

  cargarTrabajadores() {
    this.http.get<Trabajador[]>(`${this.API}/trabajadores`).subscribe(t => this.trabajadores.set(t));
  }

  buscar() {
    if (!this.filtroTrabajador) { this.mostrarMensaje('Selecciona un trabajador', true); return; }
    this.cargando.set(true);

    // Cargar asistencias y calendario en paralelo
    forkJoin({
      asistencias: this.http.get<any[]>(
        `${this.API}/asistencias/trabajador/${this.filtroTrabajador}?mes=${this.filtroMes}&anio=${this.filtroAnio}`
      ),
      calendario: this.http.get<any>(
        `${this.API}/calendario/${this.filtroTrabajador}?mes=${this.filtroMes}&anio=${this.filtroAnio}`
      )
    }).subscribe({
      next: ({ asistencias, calendario }) => {
        const diasCalendario: any[] = calendario.dias ?? [];
        const totalDias = new Date(this.filtroAnio, this.filtroMes, 0).getDate();
        const resultado: Asistencia[] = [];

        for (let n = 1; n <= totalDias; n++) {
          const fechaStr = `${this.filtroAnio}-${String(this.filtroMes).padStart(2,'0')}-${String(n).padStart(2,'0')}`;
          const reg      = asistencias.find((r: any) => r.fecha === fechaStr);
          const calDia   = diasCalendario.find((d: any) => d.fecha === fechaStr);
          const esDescanso = calDia?.estado === 'DESCANSO';

          resultado.push({
            id:                reg?.id ?? null,
            idTrabajador:      this.filtroTrabajador,
            fecha:             fechaStr,
            horaEntrada:       reg?.horaEntrada  ?? null,
            horaSalida:        reg?.horaSalida   ?? null,
            estadoEntrada:     reg?.estadoEntrada ?? (esDescanso ? 'DESCANSO' : 'SIN_REGISTRO'),
            estadoSalida:      reg?.estadoSalida  ?? '—',
            minutosTardanza:   reg?.minutosTardanza ?? 0,
            corregidoPorAdmin: reg?.corregidoPorAdmin ?? false,
            observacion:       reg?.observacion ?? null,
            tieneRegistro:     !!reg,
            esDescanso
          });
        }

        this.asistencias.set(resultado);
        this.cargando.set(false);
      },
      error: () => { this.cargando.set(false); this.mostrarMensaje('Error al cargar', true); }
    });
  }

  abrirCorreccion(a: Asistencia) {
    this.editando.set(a);
    this.form = {
      horaEntrada:   a.horaEntrada ?? '',
      horaSalida:    a.horaSalida  ?? '',
      estadoEntrada: (a.estadoEntrada === 'SIN_REGISTRO' || a.estadoEntrada === 'DESCANSO')
                       ? 'VACACIONES' : a.estadoEntrada,
      estadoSalida:  (a.estadoSalida === '—') ? 'PENDIENTE' : a.estadoSalida,
      observacion:   a.observacion ?? ''
    };
    this.mostrarForm.set(true);
  }

  cerrarForm() { this.mostrarForm.set(false); }

  guardarCorreccion() {
    const a = this.editando();
    if (!a) return;
    this.guardando.set(true);

    const payload = {
      horaEntrada:   this.form.horaEntrada   || null,
      horaSalida:    this.form.horaSalida    || null,
      estadoEntrada: this.form.estadoEntrada || null,
      estadoSalida:  this.form.estadoSalida  || null,
      observacion:   this.form.observacion   || null
    };

    if (!a.tieneRegistro || a.id === null) {
      const crearPayload = {
        idTrabajador:  a.idTrabajador,
        fecha:         a.fecha,
        estadoEntrada: this.form.estadoEntrada,
        estadoSalida:  this.form.estadoSalida,
        observacion:   this.form.observacion || null
      };
      this.http.post(`${this.API}/asistencias/crear-manual`, crearPayload).subscribe({
        next: () => { this.guardando.set(false); this.mostrarForm.set(false); this.mostrarMensaje('Registro creado ✓', false); this.buscar(); },
        error: (e) => { this.guardando.set(false); this.mostrarMensaje(e.error?.mensaje ?? 'Error', true); }
      });
    } else {
      this.http.put(`${this.API}/asistencias/${a.id}/corregir`, payload).subscribe({
        next: () => { this.guardando.set(false); this.mostrarForm.set(false); this.mostrarMensaje('Corregido ✓', false); this.buscar(); },
        error: (e) => { this.guardando.set(false); this.mostrarMensaje(e.error?.mensaje ?? 'Error', true); }
      });
    }
  }

  getClaseEstado(estado: string): string {
    switch (estado) {
      case 'PUNTUAL':
      case 'A_TIEMPO':   return 'verde';
      case 'TARDANZA':   return 'naranja';
      case 'FALTA':      return 'rojo';
      case 'VACACIONES': return 'morado';
      case 'DESCANSO':   return 'gris';
      default:           return 'gris';
    }
  }

  getEtiqueta(estado: string): string {
    const map: Record<string, string> = {
      'PUNTUAL':              'Puntual',
      'A_TIEMPO':             'A tiempo',
      'TARDANZA':             'Tardanza',
      'FALTA':                'Falta',
      'VACACIONES':           'Vacaciones',
      'DESCANSO':             'Descanso',
      'SIN_REGISTRO':         'Sin registro',
      'REGISTRADA':           'Registrada',
      'SALIDA_ANTICIPADA':    'S. anticipada',
      'SALIDA_NO_REGISTRADA': 'Sin salida',
      'PENDIENTE':            'Pendiente',
    };
    return map[estado] ?? estado;
  }

  private mostrarMensaje(msg: string, esError: boolean) {
    this.mensaje.set(msg);
    this.mensajeError.set(esError);
    setTimeout(() => this.mensaje.set(''), 3500);
  }
}